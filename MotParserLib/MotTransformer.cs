// MIT license
//
// Copyright (c) 2018 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using NLog;
using System.Runtime.InteropServices;
using System.Configuration;
using MotCommonLib;
using MotListenerLib;


//using TransformerService.Controllers;

namespace MotParserLib
{
    public enum PlatformOs
    {
        Unknown,
        Windows,
        Unix,
        macOS,
        Linux,
        Android,
        iOS
    };

    public class MotTransformerBase : IDisposable
    {
        protected Logger eventLogger;
        protected Hl7SocketListener socketListener;
        protected FilesystemListener filesystemListener;
        protected string gatewayAddress;
        protected int gatewayPort;
        protected int listenerPort;
        protected string winMonitorDirectory;
        protected string nixMonitorDirectory;
        protected bool watchFileSystem;
        protected bool watchSocket;

        protected List<string> _responses;
        private PlatformOs _platform;

        public MotTransformerBase()
        {
            LoadConfiguration();
            _platform = GetOs();
        }

        protected void LoadConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            listenerPort = Convert.ToInt32(appSettings["ListenerPort"] ?? "24025");
            gatewayPort = Convert.ToInt32(appSettings["GatewayPort"] ?? "24042");
            gatewayAddress = appSettings["GatewayAddress"] ?? "192.168.1.160";
            winMonitorDirectory = appSettings["WinMonitorDirectory"] ?? @"c:\motnext\io";
            nixMonitorDirectory = appSettings["NixMonitorDirectory"] ?? @"~/motnext/io";
            watchFileSystem = (appSettings["WatchFileSystem"] ?? "false") == "true";
            watchSocket = (appSettings["WatchSocket"] ?? "false") == "true";
        }

        protected void SaveConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            appSettings["ListenerPort"] = listenerPort.ToString();
            appSettings["GatewayPort"] = gatewayPort.ToString();
            appSettings["GatewayAddress"] = gatewayAddress;
            appSettings["WinMonitorDirectory"] = winMonitorDirectory;
            appSettings["NixMonitorDirectory"] = nixMonitorDirectory;
            appSettings["WatchFileSystem"] = watchFileSystem.ToString();
            appSettings["WatchSocket"] = watchSocket.ToString();
        }

        public List<string> GetConfigList()
        {
            var appSettings = ConfigurationManager.AppSettings;
            var response = new List<string>();

            response.Add($"ListenerPort: {appSettings["ListenerPort"] ?? "24025"}");
            response.Add($"GatewayPort: {appSettings["GatewayPort"] ?? "24042"}");
            response.Add($"GAteway Address: {appSettings["GatewayAddress"] ?? "127.0.0.1"}");
            response.Add($"WinMonitorDirectory: {appSettings["WinMonitorDirectory"] ?? @"c:\motnext\io"}");
            response.Add($"NixMonitorDirectory: {appSettings["NixMonitorDirectory"] ?? @"~/motnext/io"}");
            response.Add($"WatchFileSystem: {appSettings["WatchFileSystem"]}");
            response.Add($"WatchSocket: {appSettings["WatchSocket"] ?? "false"}");

            return response;
        }

        protected PlatformOs GetOs()
        {
            // just worry about Nix and Win for now
            if (RuntimeInformation.OSDescription.Contains("Unix"))
            {
                _platform = PlatformOs.Unix;
            }
            else if (RuntimeInformation.OSDescription.Contains("Windows"))
            {
                _platform = PlatformOs.Windows;
            }
            else
            {
                _platform = PlatformOs.Unknown;
            }

            return _platform;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                filesystemListener?.Dispose();
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// Direct IDisposable destructor that destroys and nullifies everything 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class MotTransformerInterface : MotTransformerBase
    {
        InputDataFormat _inputDataFormat;

        public string Parse(string data, InputDataFormat inputDataFormat = InputDataFormat.AutoDetect)
        {
            this._inputDataFormat = inputDataFormat;
            return Parse(data);
        }

        public string Parse(string data)
        {
            if(data == null)
            {
                return "Null data";
            }

            var responseMessage = data;

            using (var gatewaySocket = new MotSocket(gatewayAddress, gatewayPort))
            {
                using (var p = new MotParser(gatewaySocket, data, _inputDataFormat))
                {
                    eventLogger.Info(p.ResponseMessage);
                    responseMessage = p.ResponseMessage;
                    _responses.Add(responseMessage);
                }
            }

            return responseMessage;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MotTransformerInterface()
        {
            try
            {
                LoadConfiguration();
                _responses = new List<string>();
                eventLogger = LogManager.GetLogger("Mot.Transformer.Service");
            }
            catch (Exception ex)
            {
                eventLogger?.Fatal("Failed to start construct HL7Execute: {0}", ex.Message);
                throw new Exception("Constructor Failure");
            }
        }
        /// <summary>
        /// <c>Startup</c>
        /// The method caled by Topshelf to start the service running
        /// </summary>
        public void Start()
        {
            if (watchSocket)
            {
                socketListener = new Hl7SocketListener(listenerPort, Parse);
                socketListener.RunAsService = true;
                socketListener.Go();
            }

            if (watchFileSystem)
            {
                filesystemListener = new FilesystemListener((GetOs() == PlatformOs.Windows) ? winMonitorDirectory : nixMonitorDirectory, Parse);
                filesystemListener.RunAsService = true;
                filesystemListener.Go();
            }

            eventLogger.Info("Service started");
        }
        /// <summary>
        /// <c>StopListener</c>
        /// The method called by Topshelf to halt the service
        /// </summary>
        public void Stop()
        {
            if (watchSocket)
            {
                socketListener.ShutDown();
            }

            if (watchFileSystem)
            {
                filesystemListener.ShutDown();
            }

            eventLogger.Info("sevice stopped");
        }
        /// <summary>
        /// <c>ShutDown</c>
        /// Same as stop for now
        /// </summary>
        public void ShutDown()
        {
            this.Stop();
        }
        /// <summary>
        /// <c>Restart</c>
        /// Method called by Topshellf to restart the service
        /// </summary>
        public void Restart()
        {
            if (watchSocket)
            {
                socketListener.ShutDown();
                socketListener.Go();
            }

            if (watchFileSystem)
            {
                filesystemListener.ShutDown();
                filesystemListener.Go();
            }

            eventLogger.Info("Service restarted");
        }
    }
}

