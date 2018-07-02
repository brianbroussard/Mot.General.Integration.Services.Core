// 
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
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using NLog;


//
// var portValue = System.Configuration.ConfigurationManager.AppSettings["Port"];
//
namespace MotCommonLib
{
#pragma warning disable 1591
    public delegate string StringStringDelegate(string data);
    public delegate void VoidStringDelegate(string data);
    public delegate byte[] ByteByteDelegate(byte[] data);
    public delegate void VoidByteDelegate(byte[] data);
    public delegate bool BoolStringDelegate(string data);
    public delegate bool BoolByteDelegate(byte[] data);
#pragma warning restore 1591

    /// <summary>
    /// <c>motSocket</c>
    /// A simple socket abstraction used for listeners and direct socket manipulation
    /// </summary>
    public class MotSocket : IDisposable
    {
        private bool serviceRunning;
        private string stringIoBuffer;
        private byte[] byteIoBuffer;
        private bool doBinaryIo;

        /// <summary>
        /// <c>Disposed</c>
        /// Flag to trap disposed object
        /// </summary>
        public bool Disposed { get; set; }

        /// <summary>
        /// <c>UseSSL</c>
        /// Property to flag the use of TLS/SSL for the instance
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        /// <c>Port</c>
        /// Property to set the target tcp/ip port for the instance 
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// <c>Address</c>
        /// Property to resolve the target IP from a FQDN
        /// </summary>
        public string Address
        {
            get => internalAddress;
            set
            {
                var hostList = Dns.GetHostAddresses(value);

                foreach (var h in hostList)
                {
                    if (h.AddressFamily == AddressFamily.InterNetwork)
                    {
                        internalAddress = h.ToString();
                    }
                }

                openForListening = false;
            }
        }

        /// <summary>
        /// <c>TCP_TIMEOUT</c>
        /// Property to set the default I/O timeout values for the instance
        /// </summary>
        public int TcpTimeout { get; set; } = 5000;


        private string internalAddress = string.Empty;
        private TcpClient localTcpClient;
        private TcpListener messageTrigger;
#pragma warning disable 1591
        public EndPoint remoteEndPoint;
#pragma warning restore 1591
        private EndPoint localEndPoint;
        private Logger eventLogger;

        /// <summary>
        /// <c>StringArgCallback</c>
        /// Callback delegate returning void taking a string argument used to call parsers
        /// </summary>
        public StringStringDelegate StringArgCallback { get; set; }

        /// <summary>
        /// <c>ByteArgCallback</c>
        /// Callback delegate returning void taking a byte[] argument used to call parsers
        /// </summary>
        public ByteByteDelegate ByteArgCallback { get; set; }

        /// <summary>
        /// <c>StringArgProtocolProcessor</c>
        /// Delegate returning bool and taking a string argument used to process return values
        /// </summary>
        public BoolStringDelegate StringArgProtocolProcessor { get; set; } = null;

        /// <summary>
        /// <c>ByteArgProtocolProcessor</c>
        /// Delegate returning a bool taking a byte[] argument used to process return values
        /// </summary>
        public BoolByteDelegate ByteArgProtocolProcessor { get; set; } = DefaultProtocolProcessor;

        bool openForListening;
#pragma warning disable 1591
        public NetworkStream netStream;
        public SslStream netSslStream;
#pragma warning restore 1591
        /// <summary>
        /// <c>SocketMutex</c>
        /// Instance Mutex to manage overlapping I/O
        /// </summary>
        public static Mutex SocketMutex;

        /// <summary>
        /// <c>tcpClientConnected</c>
        /// Instance listener reset manager
        /// </summary>
        public ManualResetEvent TcpClientConnected = new ManualResetEvent(false);

        private X509Certificate x509Certificate;
        private X509Certificate2Collection x509Collection;
        private X509Certificate2Collection x509ValidCollection;
        private X509Store x509Store;

        private void MotSetCertificate()
        {
            try
            {
                x509Store = new X509Store("MY", StoreLocation.LocalMachine);
                x509Store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                x509Collection = x509Store.Certificates;
                x509ValidCollection =
                    x509Store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now,
                        false);

                foreach (X509Certificate2 x in x509ValidCollection)
                {
                    if (x.FriendlyName.ToUpper() == Dns.GetHostName().ToUpper())
                    {
                        x509Certificate = x;
                        return;
                    }
                }

                x509Certificate = null;
            }
            catch (Exception ex)
            {
                eventLogger.Warn($"Failed to set SSL certificate: {ex.Message}");
            }
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Core constructor
        /// </summary>
        public MotSocket()
        {
            eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            MotSetCertificate();
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Constructor setting the target port as an int and setting a callback function for processing Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public MotSocket(int port, StringStringDelegate stringArgCallback = null)
        {
            StartUp(port, stringArgCallback);
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Constructor setting the target port as an int and setting a callback function for processing Binary Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public MotSocket(int port, ByteByteDelegate byteArgCallback = null)
        {
            StartUp(port, byteArgCallback);
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Constructor setting the target port as a string and setting a callback function for processing binary Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public MotSocket(string port, ByteByteDelegate byteArgCallback = null)
        {
            StartUp(Convert.ToInt32(port), byteArgCallback);
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Constructor setting the target port as a string and setting a callback function for processing Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public MotSocket(string port, StringStringDelegate stringArgCallback = null)
        {
            StartUp(Convert.ToInt32(port), stringArgCallback);
        }
        /// <summary>
        /// <c>StartUp</c>
        /// Constructor helper that can be called on an empty instance
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public void StartUp(int port, StringStringDelegate stringArgCallback = null)
        {
            openForListening = true;
            eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            try
            {
                StringArgCallback = stringArgCallback;
                Port = port;

                OpenAsServer();

                eventLogger.Info("Listening on port {0}", port);
                serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to connect to remote socket " + e.Message);
            }
        }
        /// <summary>
        /// <c>StartUp</c>
        /// Constructor helper that can be called on an empty instance
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public void StartUp(int port, ByteByteDelegate byteArgCallback = null)
        {
            openForListening = true;
            eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            try
            {
                ByteArgCallback = byteArgCallback;
                Port = port;
                doBinaryIo = true;

                OpenAsServer();

                eventLogger.Info($"Listening on port: {port}");
                serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to connect to remote socket {e.Message}");
            }
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Constructor to set the socket up as a server listing on Port
        /// </summary>
        /// <param name="port">Port to listen on as an int</param>
        public MotSocket(int port)
        {
            eventLogger = LogManager.GetLogger("motCommonLib.Socket");
            openForListening = true;

            MotSetCertificate();

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            try
            {
                Port = port;

                OpenAsServer();
                eventLogger.Info("Listening on port {0}", port);
                serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to connect to remote socket " + e.Message);
            }
        }
        /// <summary>
        /// <c>ValidateServerCertificate</c>
        /// Ensures the certificate being used by the listener is valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="serverCert"></param>
        /// <param name="certChain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public bool ValidateServerCertificate(object sender, X509Certificate serverCert, X509Chain certChain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                return true;
            }

            eventLogger.Error("Server certificate validation errors: {0}", sslPolicyErrors);

            return false;   // burn the connection
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Socket used for writing and processing return valus uusing a BoolByteDelegate for return value processing
        /// </summary>
        /// <param name="targetAddress"></param>
        /// <param name="targetPort"></param>
        /// <param name="byteProtocolProcessor"></param>
        public MotSocket(string targetAddress, int targetPort, BoolByteDelegate byteProtocolProcessor = null)
        {
            Port = targetPort;
            Address = targetAddress;
            ByteArgProtocolProcessor = byteProtocolProcessor ?? DefaultProtocolProcessor;
            openForListening = false;
			eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            OpenAsClient();
        }
        /// <summary>
        /// <c>motSocket</c>
        /// Socket used for writing with the option of enabling SSL and using a BoolByteDelegate for return value processing
        /// </summary>
        /// <param name="address"></param>
        /// <param name="port"></param>
        /// <param name="secureConnection"></param>
        /// <param name="byteProtocolProcessor"></param>
        public MotSocket(string address, int port, bool secureConnection, BoolByteDelegate byteProtocolProcessor = null)
        {
            Port = port;
            Address = address;
            ByteArgProtocolProcessor = byteProtocolProcessor ?? DefaultProtocolProcessor;
            UseSsl = secureConnection;
            openForListening = false;

            eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null)
            {
                SocketMutex = new Mutex();
            }

            OpenAsClient();
        }
        /// <summary>
        /// <c>~motSocket</c>
        /// Simple destructor that calls the Dispose function
        /// </summary>
        ~MotSocket()
        {
            Dispose(false);
        }

        /// <summary>
        /// <c>AsyncHandler</c>
        /// Callback to handle inbound traffic and process it using the registered processing callback
        /// </summary>
        /// <param name="asyncResult"></param>
        private void AsyncHandler(IAsyncResult asyncResult)
        {
            try
            {

				// While testing on a Linux VM, if we didn't slow things down it jumpped right out of 
				// the method whithout processing anything.  Adding the sleep slowed it down enough
				Thread.Sleep(50);

                Thread.CurrentThread.Name = "IAsyncHandler (" + Thread.CurrentThread.ManagedThreadId + ")";
                TcpListener localListener = (TcpListener)asyncResult.AsyncState;

                using (var tcpClient = localListener.EndAcceptTcpClient(asyncResult))
                {
                    using (var localStream = tcpClient.GetStream())
                    {
                        if (localStream.CanRead)
                        {
                            localStream.ReadTimeout = 500;
                            localStream.WriteTimeout = 500;

                            remoteEndPoint = tcpClient.Client.RemoteEndPoint;
                            localEndPoint = tcpClient.Client.LocalEndPoint;

                            int bytesIn = 1;
                            int totalBytes = 0;

                            byte[] bytesRead = new byte[1];

                            try
                            {
                                byteIoBuffer = new byte[512];
                                stringIoBuffer = "";

                                while (localStream.DataAvailable)
                                {                                 
                                    bytesIn = localStream.Read(byteIoBuffer, 0, byteIoBuffer.Length);
                                    
                                    if (!doBinaryIo)
                                    {
                                        stringIoBuffer += Encoding.UTF8.GetString(byteIoBuffer, 0, bytesIn);
                                    }
                                    else
                                    {
                                        Array.Resize(ref bytesRead, totalBytes + 512);
                                        byteIoBuffer.CopyTo(bytesRead, totalBytes);
                                    }

                                    totalBytes += bytesIn;
                                }
                            }
                            catch (IOException ix)
                            {
                                eventLogger.Warn($"AsyncRead: {ix.Message}");
                            }
                            catch (Exception ex)
                            {
                                var errorMsg = $"AsyncRead failed: {ex.Message}. {totalBytes} bytes read";
                                eventLogger.Error(errorMsg);
                                throw new Exception(errorMsg);
                            }

                            if (totalBytes > 0)
                            {
                                // Assign the local stream to the global and process
                                netStream = localStream;
                                
                                if (!doBinaryIo)
                                {
                                   var resp = StringArgCallback?.Invoke(stringIoBuffer);
                                    if(!string.IsNullOrEmpty(resp))
                                    {
                                        localStream.Write(Encoding.ASCII.GetBytes(resp), 0, resp.Length);
                                    }
                                }
                                else
                                {
                                    // Resize the byte array to match the data size
                                    Array.Resize(ref bytesRead, totalBytes);
                                    var resp = ByteArgCallback?.Invoke(bytesRead);
                                    if(resp.Length > 0)
                                    {
                                        localStream.Write(resp, 0, resp.Length);
                                    }
                                }
                            }

                            TcpClientConnected.Set();
                        }
                        else
                        {
                            TcpClientConnected.Set();
                            throw new Exception("Input stream is unreadable");
                        }
                    }
                }
            }
            catch(Exception ex)
			{
				eventLogger.Error($"Async I/O handler{ex.Message}");
				TcpClientConnected.Set();
			}           
        }

        /// <summary>
        /// <c>ListenAsync</c>
        /// Async listener, hands off to AsyncHandler on tap
        /// </summary>
        public void ListenAsync()
        {
            while (serviceRunning)
            {
                TcpClientConnected.Reset();
                messageTrigger.BeginAcceptTcpClient(AsyncHandler, messageTrigger);
                TcpClientConnected.WaitOne();
            }
        }

        /// <summary>
        /// <c>SecureAsyncHandler</c>
        ///  Callback to handle TLS/SSL inbound traffic and process it using the registered processing callback
        /// </summary>
        /// <param name="asyncResult"></param>
        public void SecureAsyncHandler(IAsyncResult asyncResult)
        {
            try
            {
                var secureListener = (TcpListener)asyncResult.AsyncState;

                using (var tcpClient = secureListener.EndAcceptTcpClient(asyncResult))
                {
                    using (var sslStream = new SslStream(tcpClient.GetStream(), false))
                    {
                        sslStream.AuthenticateAsServer(x509Certificate, false, SslProtocols.Default, false);

                        var totalBytes = 0;

                        try
                        {
                            Thread.Sleep(1);

                            byteIoBuffer = new byte[1024];
                            stringIoBuffer = "";

                            int bytesIn;
                            do
                            {
                                bytesIn = sslStream.Read(byteIoBuffer, 0, byteIoBuffer.Length);
                                ByteArgCallback?.Invoke(byteIoBuffer);
                                stringIoBuffer += Encoding.UTF8.GetString(byteIoBuffer, 0, bytesIn);
                                totalBytes += bytesIn;
                            } while (bytesIn > 0);
                        }
                        catch (IOException iox)
                        {
                            var errorMsg = $"SecureAsyncHandler read() failed: {iox.Message}";
                            eventLogger.Error(errorMsg);
                            throw new Exception(errorMsg);
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"SecureAsyncHandler read() failed: {ex.Message}";
                            eventLogger.Error(errorMsg);
                            throw new Exception(errorMsg);
                        }

                        // Assuming we can stop blocking the port, probably wrong thinking
                        TcpClientConnected.Set();

                        if (totalBytes > 0)
                        {
                            //__ssl_stream = __lstream;
                            StringArgCallback?.Invoke(stringIoBuffer);
                            var resp = StringArgCallback?.Invoke(stringIoBuffer);

                            if (!string.IsNullOrEmpty(resp))
                            {
                                sslStream.Write(Encoding.ASCII.GetBytes(resp), 0, resp.Length);
                            }
                        }

                        //SslStream.Close();
                        //TcpClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                eventLogger.Error("Secure Read: {0}", ex.Message);
            }
            finally
            {
                TcpClientConnected.Set();
            }
        }

        /// <summary>
        /// <c>SecureListenAsync</c>
        /// Async listener on a TLS/SSL port, hands off to SecureAsyncHandler on tap
        /// </summary>
        public void SecureListenAsync()
        {
            if (x509Certificate == null)
            {
                throw new ArgumentNullException($"Missing X509 Certificate");
            }

            while (serviceRunning)
            {
                TcpClientConnected.Reset();
                messageTrigger.BeginAcceptTcpClient(SecureAsyncHandler, messageTrigger);
                TcpClientConnected.WaitOne();
            }
        }

        /// <summary>
        /// <c>SecureListen</c>
        /// Synchronus listener on a TLS/SSL port, processes everything locally
        /// </summary>
        public void SecureListen()
        {
            if (x509Certificate == null)
            {
                throw new ArgumentNullException($"Missing X509 Certificate");
            }

            serviceRunning = true;

            while (serviceRunning)
            {
                try
                {
                    using (localTcpClient = messageTrigger.AcceptTcpClient())
                    {
                        netSslStream = new SslStream(localTcpClient.GetStream(), false)
                        {
                            ReadTimeout = TcpTimeout,
                            WriteTimeout = TcpTimeout
                        };

                        remoteEndPoint = localTcpClient.Client.RemoteEndPoint;
                        localEndPoint = localTcpClient.Client.LocalEndPoint;

                        netSslStream.AuthenticateAsServer(x509Certificate, false, SslProtocols.Tls, true);

                        eventLogger.Info($"Accepted and Authenticated TLS connection from remote endpoint: {remoteEndPoint}");


                        if (Read() > 0)
                        {
                            StringArgCallback?.Invoke(stringIoBuffer);
                        }

                        netSslStream.Close();
                        localTcpClient.Close();
                    }
                }
                catch (IOException ex)
                {
                    eventLogger.Error($"Create secure stream I/O error: { ex.StackTrace}");
                }
                catch (AuthenticationException ex)
                {
                    eventLogger.Error($"Create secure stream Authentication error: {ex.StackTrace}");
                }
                catch (InvalidOperationException ex)
                {
                    // probably shutting the thread down
                    serviceRunning = false;
                    Console.WriteLine(ex.Message);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// <c>Listen</c>
        /// Synchronus listener on a clear text port, processes everything locally
        /// </summary>
        public void Listen()
        {
            serviceRunning = true;

            while (serviceRunning)
            {
                try
                {
                    using (var tcpClient = messageTrigger.AcceptTcpClient())
                    {
                        using (netStream = tcpClient.GetStream())
                        {
                            netStream.ReadTimeout = TcpTimeout;
                            netStream.WriteTimeout = TcpTimeout;

                            remoteEndPoint = tcpClient.Client.RemoteEndPoint;
                            localEndPoint = tcpClient.Client.LocalEndPoint;

                            eventLogger.Debug($"Accepted connection from remote endpoint {remoteEndPoint}");

                            if (Read() > 0)
                            {
                                StringArgCallback?.Invoke(stringIoBuffer);
                                eventLogger.Debug($"Data from {remoteEndPoint}, {stringIoBuffer}");
                            }
                        }
                    }
                }
                catch (IOException iox)
                {
                    eventLogger.Error($"listen() failed: {iox.Message}");
                    throw;
                }
                catch (InvalidOperationException ex)
                {
                    // probably shutting the thread down
                    serviceRunning = false;
                    Console.WriteLine(ex.Message);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);

                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }

        /// <summary>
        /// <c>Read</c>
        /// Managed stream reader for clear and secure cnnections with muutex access management
        /// </summary>
        /// <returns>The number of bytes read</returns>
        public int Read()
        {
            var totalBytes = 0;

            if (SocketMutex == null)
            {
                return 0;
            }

            SocketMutex.WaitOne();

            try
            {
                Thread.Sleep(1);

                byteIoBuffer = new byte[1024];
                stringIoBuffer = "";

                int bytesIn;
                if (UseSsl)
                {
                    do
                    {
                        bytesIn = netSslStream.Read(byteIoBuffer, 0, byteIoBuffer.Length);

                        ByteArgCallback?.Invoke(byteIoBuffer);

                        stringIoBuffer += Encoding.UTF8.GetString(byteIoBuffer, 0, bytesIn);
                        totalBytes += bytesIn;
                    } while (bytesIn > 0);
                }
                else
                {
                    while (netStream.DataAvailable)
                    {
                        bytesIn = netStream.Read(byteIoBuffer, 0, byteIoBuffer.Length);

                        ByteArgCallback?.Invoke(byteIoBuffer);

                        stringIoBuffer += Encoding.UTF8.GetString(byteIoBuffer, 0, bytesIn);
                        totalBytes += bytesIn;
                    }
                }

                SocketMutex.ReleaseMutex();

                return totalBytes;
            }
            catch (IOException iox)
            {
                SocketMutex.ReleaseMutex();
                eventLogger.Error("read() failed: {0}", iox.Message);
                throw;
                // timeout
            }
            catch (Exception ex)
            {
                SocketMutex.ReleaseMutex();
                eventLogger.Error("read() failed: {0}", ex.Message);
                throw; // new Exception("read() failed: " + ex.Message);
            }
        }

        /// <summary>
        /// <c>Read</c>
        /// Managed stream reader for clear and secure cnnections with muutex access management
        /// that takes a byte buffer reference to return read data and specifies starting index
        /// and number of bytes to read.
        /// </summary>
        /// <returns>The number of bytes read</returns>
        public int Read(ref byte[] byteBuffer, int index, int count)
        {
            if (SocketMutex == null)
            {
                return 0;
            }

            SocketMutex.WaitOne();

            try
            {
                int readCount;

                if (UseSsl)
                {
                    readCount = netSslStream.Read(byteBuffer, index, count);
                    ByteArgCallback?.Invoke(byteIoBuffer);
                }
                else
                {
                    readCount = netStream.Read(byteBuffer, index, count);
                    ByteArgCallback?.Invoke(byteIoBuffer);
                }

                SocketMutex.ReleaseMutex();

                return readCount;
            }
            catch
            {
                SocketMutex.ReleaseMutex();
                return 0;
            }
        }


        /// <summary>
        /// <c>DefaultProtocolProcessor</c>
        /// Placholder delegate for processing return values from the Remote endpoint
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool DefaultProtocolProcessor(byte[] buffer)
        {
            return buffer.Length > 0;
        }

        /// <summary>
        /// <c>WriteReturn</c>
        /// Write method that sends a byte array and does not wait for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        public void WriteReturn(byte[] streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                if (UseSsl)
                {
                    netSslStream.Write(streamData, 0, streamData.Length);
                }
                else
                {
                    netStream.Write(streamData, 0, streamData.Length);
                }
            }
            catch (Exception ex)
            {
                eventLogger.Error($"Error writing byte return values: {ex.Message}");
            }

            SocketMutex.ReleaseMutex();
        }

        /// <summary>
        /// <c>WriteReturn</c>
        /// Write method that sends a string and does not wait for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        public void WriteReturn(string streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                if (UseSsl)
                {
                    netSslStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                }
                else
                {
                    netStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                }
            }
            catch (Exception ex)
            {
                eventLogger.Error($"Error writing string return values: {ex.Message}");
            }

            SocketMutex.ReleaseMutex();
        }

        /// <summary>
        /// <c>Write</c>
        /// Write method that sends a byte array and waits for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succedded</returns>
        public bool Write(byte[] streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                byte[] buffer = new byte[256];

                if (UseSsl)
                {
                    netSslStream.Write(streamData, 0, streamData.Length);
                    netSslStream.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    netStream.Write(streamData, 0, streamData.Length);
                    netStream.Read(buffer, 0, buffer.Length);
                }

                SocketMutex.ReleaseMutex();
                return ByteArgProtocolProcessor != null && (bool)ByteArgProtocolProcessor?.Invoke(buffer);
            }
            catch (IOException iox) // timeout
            {
                SocketMutex.ReleaseMutex();
                eventLogger.Error($"Error write() failed: {iox.Message}");
                return false;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error write() failed: {ex.Message}";

                SocketMutex.ReleaseMutex();
                eventLogger.Error(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        /// <summary>
        /// <c>Write</c>
        /// Write method that sends a string and waits for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succeded</returns>
        public bool Write(string streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                byte[] buffer = new byte[256];

                if (UseSsl)
                {
                    netSslStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    netSslStream.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    netStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    netStream.Read(buffer, 0, buffer.Length);
                }

                var retVal = ByteArgProtocolProcessor != null && (bool)ByteArgProtocolProcessor?.Invoke(buffer);

                SocketMutex.ReleaseMutex();

                return retVal;
            }
            catch (SocketException sx)
            {
                SocketMutex.ReleaseMutex();

                if (sx.ErrorCode == 10053)
                {
                    eventLogger.Warn($"Write() SocketException. Type = {sx.GetType().Name}, {sx.Message}");
                }

                eventLogger.Error($"Write() Exception. Type = {sx.GetType().Name}, {sx.Message}");
                return false;
            }
            catch (IOException iox)
            {
                SocketMutex.ReleaseMutex();

                eventLogger.Error($"Write() Exception. Type = {iox.GetType().Name}, {iox.Message}");

                return false;
            }
            catch (Exception ex)
            {
                SocketMutex.ReleaseMutex();

                var errorMsg = $"Write() failed: {ex.Message}";
                eventLogger.Error(errorMsg);
                throw new Exception(errorMsg);
            }
        }

        /// <summary>
        /// <c>Send</c>
        /// Overload of Write sending a byte array
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succeded</returns>
        public bool Send(byte[] streamData)
        {
            return Write(streamData);
        }

        /// <summary>
        /// <c>Flush</c>
        /// A simple overload of the underlying streams to flush any data
        /// </summary>
        public void Flush()
        {
            try
            {
                if (UseSsl)
                {
                    netSslStream.Flush();
                }
                else
                {
                    netStream.Flush();
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Stream flush failure {ex.Message}");
            }
        }

        /// <summary>
        /// <c>Close</c>
        /// Closes the socket, haltuing all I/O but doese not destruct and can be reopened
        /// </summary>
        public void Close()
        {
            if (serviceRunning)
            {
                try
                {
                    serviceRunning = false;

                    if (openForListening)
                    {
                        messageTrigger.Stop();
                    }

                    localTcpClient?.Close();
                    netStream?.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Socket close failure {ex.Message}");
                }
            }
        }

        /// <summary>
        /// <c>OpenAsServer</c>
        /// Sets up everything needed to be a listener on the local endpoint
        /// </summary>
        public void OpenAsServer()
        {
            try
            {
                messageTrigger = new TcpListener(IPAddress.Any, Port);
                messageTrigger.Start();

                openForListening = true;
                serviceRunning = true;
            }
            catch (Exception ex)
            {
                eventLogger.Error("Failed to start server on: {0} : {1}", Port, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// <c>OpenAsClient</c>
        /// Sets up everything needed to act as a client to a remote endpoint
        /// </summary>
        public void OpenAsClient()
        {
            localTcpClient = new TcpClient(Address, Port);

            if (UseSsl)
            {
                // Resolve the machine name for the certificate
                var hostName = Dns.GetHostEntry(Address).HostName;
                if (string.IsNullOrEmpty(hostName))
                {
                    hostName = Address;
                }

                netSslStream = new SslStream(localTcpClient.GetStream(),
                    false,
                    ValidateServerCertificate,
                    null
                );

                try
                {
                    netSslStream.AuthenticateAsClient(hostName);
                }
                catch (AuthenticationException ax)
                {
                    eventLogger.Error($"[Authentication] Failed to connect securely to {Address}:{Port}. Error: {ax.Message}");
                    localTcpClient.Close();
                    throw;
                }
                catch (IOException iox)
                {
                    eventLogger.Error($"[SystemIO] Failed to connect securely to {Address}:{Port}. Error: {iox.Message}");
                    localTcpClient.Close();
                    throw;
                }

                netSslStream.ReadTimeout = TcpTimeout;
                netSslStream.WriteTimeout = TcpTimeout;
            }
            else
            {
                netStream = localTcpClient.GetStream();
                netStream.ReadTimeout = TcpTimeout;
                netStream.WriteTimeout = TcpTimeout;
            }

            remoteEndPoint = localTcpClient.Client.RemoteEndPoint;
            localEndPoint = localTcpClient.Client.LocalEndPoint;

            openForListening = false;
        }

        /// <summary>
        /// <c>Dispose</c>
        /// Conditional IDisposable destructor
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (x509Store != null)
                    {
                        x509Store.Dispose();
                        x509Store = null;
                    }
                    if (TcpClientConnected != null)
                    {
                        TcpClientConnected.Dispose();
                        TcpClientConnected = null;
                    }
                    if (localTcpClient != null)
                    {
                        localTcpClient.GetStream().Close();
                        localTcpClient.Close();
                        localTcpClient = null;
                    }
                    if (openForListening)
                    {
                        if (messageTrigger != null)
                        {
                            messageTrigger.Stop();
                            messageTrigger.Server.Dispose();
                        }
                    }
                    else
                    {
                        if (netStream != null)
                        {
                            netStream.Dispose();
                            netStream = null;
                        }

                        if (UseSsl)
                        {
                            if (netSslStream != null)
                            {
                                netSslStream.Dispose();
                                netSslStream = null;
                            }
                        }
                    }

                    Disposed = true;
                }
                catch (Exception ex)
                {
                    eventLogger.Error($"Error disposing socket: {ex.Message}");
                }
            }
        }
        /// <inheritdoc />
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
}
