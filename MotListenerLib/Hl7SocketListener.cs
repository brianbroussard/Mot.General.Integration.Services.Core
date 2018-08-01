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
using MotCommonLib;
using NLog;

namespace MotListenerLib
{
    public class Hl7SocketListener
    {
        public bool UseSsl { get; set; }
        public bool RunAsService { get; set; }
        public Logger EventLogger { get; set; }

        public MotSocket GatewaySocket { get; set; }
     
        private MotSocket ListenerSocket;
        public MotSocket WorkerSocket;
        private Thread WorkerThread;
        private StringStringDelegate stringCallback;

        public string SenderAddress { get; set; }
        public string SenderPort { get; set; }

        public Hl7SocketListener(string httpUrl, int port, VoidStringDelegate callback, bool useSSL)
        {
            List<string> prefixes = new List<string>
            {
                $"http://+:{port}/"
            };

            if (UseSsl)
            {
                prefixes.Add($"https://+:{port}/");
            }

            //Hl7FhirListner.StartListener(prefixes, ParseHl7Message);
        }

        public Hl7SocketListener(int port, StringStringDelegate callback, bool useSSL = false)
        {
            try
            {
                this.stringCallback = callback ?? throw new ArgumentNullException($"callback null");

                ListenerSocket = new MotSocket(port, callback)
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
                    WorkerThread = new Thread(() => ListenerSocket.SecureListenAsync())
                    {
                        Name = "Encrypted Listener"
                    };
                    WorkerThread.Start();
                }
                else
                {
                    WorkerThread = new Thread(() => ListenerSocket.ListenAsync())
                    {
                        Name = "Listener"
                    };
                    WorkerThread.Start();
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
                ListenerSocket?.Close();
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
                    var targetIp = IPAddress.Parse(((IPEndPoint)ListenerSocket.remoteEndPoint).Address.ToString());

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
                    ListenerSocket.WriteReturn(message);
                }
                else
                {
                    ListenerSocket.Write(message);
                }

                ListenerSocket.Flush();
            }
            catch (Exception ex)
            {
                EventLogger?.Error("Port I/O error sending ACK to {0}.  {1}", ListenerSocket.remoteEndPoint, ex.Message);
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
