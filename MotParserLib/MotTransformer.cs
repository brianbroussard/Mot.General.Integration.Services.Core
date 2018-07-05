// MIT license
//
// Copyright (c) 2016 by Peter H. Jenney and Medicine-On-Time, LLC.
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
using System.IO;
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
        protected Logger EventLogger;
        protected Hl7SocketListener SocketListener;
        protected FilesystemListener FilesystemListener;
        protected string GatewayAddress;
        protected int GatewayPort;
        protected int ListenerPort;
        protected string WinMonitorDirectory;
        protected string NixMonitorDirectory;
        protected bool WatchFileSystem;
        protected bool WatchSocket;

        public List<string> Responses;
        public PlatformOs Platform;

        public MotTransformerBase()
        {
            LoadConfiguration();
            Platform = GetOs();
        }

        protected void LoadConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            ListenerPort = Convert.ToInt32(appSettings["ListenerPort"] ?? "24025");
            GatewayPort = Convert.ToInt32(appSettings["GatewayPort"] ?? "24042");
            GatewayAddress = appSettings["GatewayAddress"] ?? "192.168.1.160";
            WinMonitorDirectory = appSettings["WinMonitorDirectory"] ?? @"c:\motnext\io";
            NixMonitorDirectory = appSettings["NixMonitorDirectory"] ?? @"~/motnext/io";
            WatchFileSystem = (appSettings["WatchFileSystem"] ?? "false") == "true";
            WatchSocket = (appSettings["WatchSocket"] ?? "false") == "true";
        }

        protected void SaveConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            appSettings["ListenerPort"] = ListenerPort.ToString();
            appSettings["GatewayPort"] = GatewayPort.ToString();
            appSettings["GatewayAddress"] = GatewayAddress;
            appSettings["WinMonitorDirectory"] = WinMonitorDirectory;
            appSettings["NixMonitorDirectory"] = NixMonitorDirectory;
            appSettings["WatchFileSystem"] = WatchFileSystem.ToString();
            appSettings["WatchSocket"] = WatchSocket.ToString();
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
                Platform = PlatformOs.Unix;
            }
            else if (RuntimeInformation.OSDescription.Contains("Windows"))
            {
                Platform = PlatformOs.Windows;
            }
            else
            {
                Platform = PlatformOs.Unknown;
            }

            return Platform;
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FilesystemListener?.Dispose();
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
        InputDataFormat inputDataFormat;

        public string Parse(string data, InputDataFormat inputDataFormat = InputDataFormat.AutoDetect)
        {
            this.inputDataFormat = inputDataFormat;
            return Parse(data);
        }

        public string Parse(string data)
        {
            if(data == null)
            {
                return "Null data";
            }

            var resp = data;

            using (var GatewaySocket = new MotSocket(GatewayAddress, GatewayPort))
            {
                using (var p = new MotParser(GatewaySocket, data, inputDataFormat))
                {
                    EventLogger.Info(p.ResponseMessage);
                    resp = p.ResponseMessage;
                    Responses.Add(resp);
                }
            }

            return resp;
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public MotTransformerInterface()
        {
            try
            {
                LoadConfiguration();
                Responses = new List<string>();
                EventLogger = LogManager.GetLogger("Mot.Transformer.Service");
            }
            catch (Exception ex)
            {
                EventLogger?.Fatal("Failed to start construct HL7Execute: {0}", ex.Message);
                throw new Exception("Constructor Failure");
            }
        }
        /// <summary>
        /// <c>Startup</c>
        /// The method caled by Topshelf to start the service running
        /// </summary>
        public void Start()
        {
            if (WatchSocket)
            {
                SocketListener = new Hl7SocketListener(ListenerPort, Parse);
                SocketListener.RunAsService = true;
                SocketListener.Go();
            }

            if (WatchFileSystem)
            {
                FilesystemListener = new FilesystemListener((GetOs() == PlatformOs.Windows) ? WinMonitorDirectory : NixMonitorDirectory, Parse);
                FilesystemListener.RunAsService = true;
                FilesystemListener.Go();
            }

            EventLogger.Info("Service started");
        }
        /// <summary>
        /// <c>StopListener</c>
        /// The method called by Topshelf to halt the service
        /// </summary>
        public void Stop()
        {
            if (WatchSocket)
            {
                SocketListener.ShutDown();
            }

            if (WatchFileSystem)
            {
                FilesystemListener.ShutDown();
            }

            EventLogger.Info("sevice stopped");
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
            if (WatchSocket)
            {
                SocketListener.ShutDown();
                SocketListener.Go();
            }

            if (WatchFileSystem)
            {
                FilesystemListener.ShutDown();
                FilesystemListener.Go();
            }

            EventLogger.Info("Service restarted");
        }
    }
}

