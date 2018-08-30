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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Mot.Common.Interface.Lib;
using NLog;

namespace Mot.Listener.Interface.Lib
{
    public class Hl7SocketListener : IDisposable
    {
        // ReSharper disable once NotAccessedField.Local
        private readonly string _httpUrl;
        // ReSharper disable once NotAccessedField.Local
        private readonly VoidStringDelegate _callback;
        // ReSharper disable once NotAccessedField.Local
        private readonly bool _useSsl;

        public bool UseSsl { get; set; }
        public bool RunAsService { get; set; }
        public Logger EventLogger { get; set; }
        public bool DebugMode { get; set; }
        public bool AllowZeroTQ { get; set; }

        public MotSocket GatewaySocket { get; set; }
     
        private readonly MotSocket _listenerSocket;
        private Thread _workerThread;
        // ReSharper disable once NotAccessedField.Local
        private StringStringDelegate _stringCallback;

        public string SenderAddress { get; set; }
        public string SenderPort { get; set; }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                GatewaySocket?.Dispose();
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public Hl7SocketListener(string httpUrl, int port, VoidStringDelegate callback, bool useSsl)
        {
            _httpUrl = httpUrl;
            _callback = callback;
            _useSsl = useSsl;

            if (UseSsl)
            {
                new List<string>
            {
                $"http://+:{port}/"
            }.Add($"https://+:{port}/");
            }

            //Hl7FhirListner.StartListener(prefixes, ParseHl7Message);
        }

        public Hl7SocketListener(int port, StringStringDelegate callback, bool useSsl = false)
        {
            _useSsl = useSsl;

            try
            { 
                if(port == 0)
                {
                    throw new ArgumentOutOfRangeException($"Port is 0");
                }

                _stringCallback = callback ?? throw new ArgumentNullException($"callback null");

                _listenerSocket = new MotSocket(port, callback)
                {
                    UseSsl = UseSsl
                };
            }
            catch (Exception e)
            {
                var errString = $"An error occurred while attempting to start the HL7 Listener: {e.Message}";
                EventLogger.Error(errString);
                throw;
            }
        }

        public void Go()
        {
            try
            {
                if (UseSsl)
                {
                    _workerThread = new Thread(() => _listenerSocket.SecureListenAsync())
                    {
                        Name = "Encrypted Listener"
                    };
                    _workerThread.Start();
                }
                else
                {
                    _workerThread = new Thread(() => _listenerSocket.ListenAsync())
                    {
                        Name = "Listener"
                    };
                    _workerThread.Start();
                }
            }
            catch(Exception ex)
            {
                EventLogger.Error($"An error occurred while attempting to start the socket listener: {ex.Message}");
                throw;
            }
        }

        public void ShutDown()
        {
            try
            {
                _listenerSocket?.Close();
            }
            catch (Exception ex)
            {
                EventLogger.Error($"An error occurred while attempting to stop the HL7 listener: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// <c>WriteMessageToEndpoint</c>
        /// Send a message back to the originatin g system
        /// </summary>
        /// <param name="message"></param>
        /// <param name="overridePort"></param>
        /// <param name="wait"></param>
        public void WriteMessageToEndpoint(string message, int overridePort, bool wait = false)
        {
            try
            {
                if (overridePort > 0)
                {
                    var targetIp = IPAddress.Parse(((IPEndPoint)_listenerSocket.RemoteEndPoint).Address.ToString());

                    using (var localTcpClient = new TcpClient(targetIp.ToString(), overridePort))
                    {
                        using (var stream = localTcpClient.GetStream())
                        {
                            stream.Write(System.Text.Encoding.UTF8.GetBytes(message), 0, message.Length);
                        }
                    }

                    return;
                }

                if (!wait)
                {
                    _listenerSocket.WriteReturn(message);
                }
                else
                {
                    _listenerSocket.Write(message);
                }

                _listenerSocket.Flush();
            }
            catch (Exception ex)
            {
                EventLogger?.Error("Port I/O error sending ACK to {0}.  {1}", _listenerSocket.RemoteEndPoint, ex.Message);
            }
        }

        /// <summary>
        /// <c>WriteMessageToFile</c>
        /// Persist the message to a file
        /// </summary>
        /// <param name="message"></param>
        /// <param name="fileName"></param>
        public void WriteMessageToFile(string message, string fileName)
        {
            try
            {
                EventLogger.Info("Received message. Saving to file {0}", fileName);
                using (var file = new StreamWriter(fileName))
                {
                    file.Write(message);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error("Failed to write file {0}, {1}", fileName, ex.Message);
            }
        }
    }
}
