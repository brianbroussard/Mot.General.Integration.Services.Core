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
using System.Configuration;
using Mot.Common.Interface.Lib;
using Mot.Listener.Interface.Lib;
using NLog;


//using TransformerService.Controllers;

namespace Mot.Parser.InterfaceLib
{
    public class MotTransformerBase : IDisposable
    {
        public PlatformOs Platform { get; }
        protected Logger EventLogger;
        protected List<string> Responses;

        public MotTransformerBase()
        {
            LoadConfiguration();
            Platform = GetPlatformOs.Go();
        }

        protected Hl7SocketListener SocketListener { get; set; }
        protected FilesystemListener FilesystemListener { get; set; }
        protected string GatewayAddress { get; set; }
        protected int GatewayPort { get; set; }
        protected int ListenerPort { get; set; }
        protected string WinMonitorDirectory { get; set; }
        protected string NixMonitorDirectory { get; set; }
        protected bool WatchFileSystem { get; set; }
        protected bool WatchSocket { get; set; }
        protected bool DebugMode { get; set; }
        protected bool AllowZeroTQ { get; set; }

        // If a patient doesn't have a location, it gets put in the default store
        // most of the time that's niot what we want
        protected string DefaultStoreLoc { get; set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected void LoadConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            ListenerPort = Convert.ToInt32(appSettings["ListenerPort"] ?? "24025");
            GatewayPort = Convert.ToInt32(appSettings["GatewayPort"] ?? "24042");
            GatewayAddress = appSettings["GatewayAddress"] ?? "127.0.0.1";
            WinMonitorDirectory = appSettings["WinMonitorDirectory"] ?? @"c:\motnext\io";
            NixMonitorDirectory = appSettings["NixMonitorDirectory"] ?? @"~/motnext/io";
            WatchFileSystem = (appSettings["WatchFileSystem"] ?? "false") == "true";
            WatchSocket = (appSettings["WatchSocket"] ?? "false") == "true";
            DebugMode = (appSettings["Debug"] ?? "false") == "true";
            AllowZeroTQ = (appSettings["AllowZeroTQ"] ?? "false") == "true";
            DefaultStoreLoc = appSettings["DefaultStoreLoc"] ?? "000000";
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
            appSettings["AllowZeroTQ"] = AllowZeroTQ.ToString();
            appSettings["DefaultStoreLoc"] = DefaultStoreLoc;
        }

        public List<string> GetConfigList()
        {
            var appSettings = ConfigurationManager.AppSettings;
            var response = new List<string> { $"ListenerPort: {appSettings["ListenerPort"] ?? "24025"}", $"GatewayPort: {appSettings["GatewayPort"] ?? "24042"}", $"Gateway Address: {appSettings["GatewayAddress"] ?? "127.0.0.1"}", $"WinMonitorDirectory: {appSettings["WinMonitorDirectory"] ?? @"c:\motnext\io"}", $"NixMonitorDirectory: {appSettings["NixMonitorDirectory"] ?? @"~/motnext/io"}", $"WatchFileSystem: {appSettings["WatchFileSystem"]}", $"WatchSocket: {appSettings["WatchSocket"] ?? "false"}", $"Debug: {appSettings["Debug"] ?? "false"}", $"AllowZeroTQ: {appSettings["AllowZeroTQ"] ?? "false"}", $"DefaultStoreLoc: {appSettings["DefaultStoreLoc"]}" };

            return response;
        }


        /// <summary>
        ///     Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                FilesystemListener?.Dispose();
            }
        }
    }

    public class MotTransformerInterface : MotTransformerBase
    {
        private InputDataFormat _inputDataFormat = InputDataFormat.AutoDetect;

        /// <summary>
        ///     Constructor
        /// </summary>
        public MotTransformerInterface()
        {
            try
            {
                EventLogger = LogManager.GetLogger("Mot.Transformer.Service");
            }
            catch (Exception ex)
            {
                EventLogger?.Fatal($"Failed to start construct MotTransformerInterface: {ex.Message}");
                throw new Exception("Constructor Failure");
            }
        }

        public string Parse(string data, InputDataFormat inputDataFormat)
        {
            _inputDataFormat = inputDataFormat;
            return Parse(data);
        }

        public string Parse(string data)
        {
            if (data == null)
            {
                return "Null data";
            }

            string responseMessage;

            using (var gatewaySocket = new MotSocket(GatewayAddress, GatewayPort))
            {
                using (var p = new MotParser(gatewaySocket, data, _inputDataFormat, DebugMode, AllowZeroTQ, DefaultStoreLoc))
                {
                    EventLogger.Info(p.ResponseMessage);
                    responseMessage = p.ResponseMessage;
                    Responses.Add(responseMessage);
                }
            }

            return responseMessage;
        }

        /// <summary>
        ///     <c>Startup</c>
        ///     The method caled by Topshelf to start the service running
        /// </summary>
        public void Start()
        {
            try
            {
                LoadConfiguration();
                Responses = new List<string>();

                if (WatchSocket)
                {
                    SocketListener = new Hl7SocketListener(ListenerPort, Parse)
                    {
                        RunAsService = true, AllowZeroTQ = AllowZeroTQ, DebugMode = DebugMode
                    };
                    SocketListener.Go();
                }

                if (WatchFileSystem)
                {
                    FilesystemListener = new FilesystemListener(GetPlatformOs.Go() == PlatformOs.Windows ? WinMonitorDirectory : NixMonitorDirectory, Parse)
                    {
                        RunAsService = true,
                        Listening = true,
                        DebugMode = DebugMode
                    };
                    FilesystemListener.Go();
                }

                EventLogger.Info("Service started");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                EventLogger.Error($"Failed to start service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     <c>StopListener</c>
        ///     The method called by Topshelf to halt the service
        /// </summary>
        public void Stop()
        {
            if (WatchSocket)
            {
                SocketListener.ShutDown();
                SocketListener.Dispose();
            }

            if (WatchFileSystem)
            {
                FilesystemListener.ShutDown();
                FilesystemListener.Dispose();
            }

            EventLogger.Info("sevice stopped");
        }

        /// <summary>
        ///     <c>ShutDown</c>
        ///     Same as stop for now
        /// </summary>
        public void ShutDown()
        {
            Stop();
        }

        /// <summary>
        ///     <c>Restart</c>
        ///     Method called by Topshellf to restart the service
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