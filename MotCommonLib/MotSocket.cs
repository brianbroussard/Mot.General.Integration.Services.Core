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
namespace Mot.Common.Interface.Lib
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
    ///     <c>motSocket</c>
    ///     A simple socket abstraction used for listeners and direct socket manipulation
    /// </summary>
    public class MotSocket : IDisposable
    {
        /// <summary>
        ///     <c>SocketMutex</c>
        ///     Instance Mutex to manage overlapping I/O
        /// </summary>
        public static Mutex SocketMutex;

        private byte[] _byteIoBuffer;
        private bool _doBinaryIo;
        private Logger _eventLogger;


        private string _internalAddress = string.Empty;
        // ReSharper disable once NotAccessedField.Local
        private EndPoint _localEndPoint;
        private TcpClient _localTcpClient;
        private TcpListener _messageTrigger;

        private bool _openForListening;
#pragma warning disable 1591
        public EndPoint RemoteEndPoint;
#pragma warning restore 1591
        private bool _serviceRunning;
        private string _stringIoBuffer;

        /// <summary>
        ///     <c>tcpClientConnected</c>
        ///     Instance listener reset manager
        /// </summary>
        public ManualResetEvent TcpClientConnected = new ManualResetEvent(false);

        private X509Certificate _x509Certificate;
        // ReSharper disable once NotAccessedField.Local
        private X509Certificate2Collection _x509Collection;
        private X509Store _x509Store;
        private X509Certificate2Collection _x509ValidCollection;

        public bool _preferASCII { get; set; }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Core constructor
        /// </summary>
        public MotSocket()
        {
            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            if (SocketMutex == null) SocketMutex = new Mutex();

            MotSetCertificate();
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Constructor setting the target port as an int and setting a callback function for processing Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public MotSocket(int port, StringStringDelegate stringArgCallback = null)
        {
            StartUp(port, stringArgCallback);
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Constructor setting the target port as an int and setting a callback function for processing Binary Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public MotSocket(int port, ByteByteDelegate byteArgCallback = null)
        {
            StartUp(port, byteArgCallback);
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Constructor setting the target port as a string and setting a callback function for processing binary Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public MotSocket(string port, ByteByteDelegate byteArgCallback = null)
        {
            StartUp(Convert.ToInt32(port), byteArgCallback);
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Constructor setting the target port as a string and setting a callback function for processing Read() data
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public MotSocket(string port, StringStringDelegate stringArgCallback = null)
        {
            StartUp(Convert.ToInt32(port), stringArgCallback);
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Constructor to set the socket up as a server listing on Port
        /// </summary>
        /// <param name="port">Port to listen on as an int</param>
        public MotSocket(int port)
        {
            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");
            _openForListening = true;

            MotSetCertificate();

            if (SocketMutex == null) SocketMutex = new Mutex();

            try
            {
                Port = port;

                OpenAsServer();
                _eventLogger.Info("Listening on port {0}", port);
                _serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to connect to remote socket " + e.Message);
            }
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Socket used for writing and processing return valus uusing a BoolByteDelegate for return value processing
        /// </summary>
        /// <param name="targetAddress"></param>
        /// <param name="targetPort"></param>
        /// <param name="byteProtocolProcessor"></param>
        public MotSocket(string targetAddress, int targetPort, BoolByteDelegate byteProtocolProcessor = null)
        {
            Port = targetPort;
            Address = targetAddress;
            ByteArgProtocolProcessor = byteProtocolProcessor ?? DefaultProtocolProcessor;
            _openForListening = false;
            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null) SocketMutex = new Mutex();

            OpenAsClient();
        }

        /// <summary>
        ///     <c>motSocket</c>
        ///     Socket used for writing with the option of enabling SSL and using a BoolByteDelegate for return value processing
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
            _openForListening = false;

            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null) SocketMutex = new Mutex();

            OpenAsClient();
        }

        /// <summary>
        ///     <c>Disposed</c>
        ///     Flag to trap disposed object
        /// </summary>
        public bool Disposed { get; set; }

        /// <summary>
        ///     <c>UseSSL</c>
        ///     Property to flag the use of TLS/SSL for the instance
        /// </summary>
        public bool UseSsl { get; set; }

        /// <summary>
        ///     <c>Port</c>
        ///     Property to set the target tcp/ip port for the instance
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        ///     <c>Address</c>
        ///     Property to resolve the target IP from a FQDN
        /// </summary>
        public string Address
        {
            get => _internalAddress;
            set
            {
                var hostList = Dns.GetHostAddresses(value);

                foreach (var h in hostList)
                {
                    if (h.AddressFamily == AddressFamily.InterNetwork)
                    {
                        _internalAddress = h.ToString();
                    }
                }

                _openForListening = false;
            }
        }

        /// <summary>
        ///     <c>TCP_TIMEOUT</c>
        ///     Property to set the default I/O timeout values for the instance
        /// </summary>
        public int TcpTimeout { get; set; } = 10000;

        /// <summary>
        ///     <c>StringArgCallback</c>
        ///     Callback delegate returning string taking a string argument used to call parsers
        /// </summary>
        public StringStringDelegate StringArgCallback { get; set; }

        /// <summary>
        ///     <c>ByteArgCallback</c>
        ///     Callback delegate returning void taking a byte[] argument used to call parsers
        /// </summary>
        public ByteByteDelegate ByteArgCallback { get; set; }

        /// <summary>
        ///     <c>StringArgProtocolProcessor</c>
        ///     Delegate returning bool and taking a string argument used to process return values
        /// </summary>
        public BoolStringDelegate StringArgProtocolProcessor { get; set; } = null;

        /// <summary>
        ///     <c>ByteArgProtocolProcessor</c>
        ///     Delegate returning a bool taking a byte[] argument used to process return values
        /// </summary>
        public BoolByteDelegate ByteArgProtocolProcessor { get; set; } = DefaultProtocolProcessor;

        /// <inheritdoc />
        /// <summary>
        ///     <c>Dispose</c>
        ///     Direct IDisposable destructor that destroys and nullifies everything
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void MotSetCertificate()
        {
            try
            {
                _x509Store = new X509Store("MY", StoreLocation.LocalMachine);
                _x509Store.Open(OpenFlags.ReadOnly | OpenFlags.OpenExistingOnly);

                _x509Collection = _x509Store.Certificates;
                _x509ValidCollection = _x509Store.Certificates.Find(X509FindType.FindByTimeValid, DateTime.Now, false);

                foreach (var x in _x509ValidCollection)
                    if (x.FriendlyName.ToUpper() == Dns.GetHostName().ToUpper())
                    {
                        _x509Certificate = x;
                        return;
                    }

                _x509Certificate = null;
            }
            catch (Exception ex)
            {
                _eventLogger.Warn($"Failed to set SSL certificate: {ex.Message}");
            }
        }

        /// <summary>
        ///     <c>StartUp</c>
        ///     Constructor helper that can be called on an empty instance
        /// </summary>
        /// <param name="port"></param>
        /// <param name="stringArgCallback"></param>
        public void StartUp(int port, StringStringDelegate stringArgCallback = null)
        {
            _openForListening = true;
            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null) SocketMutex = new Mutex();

            try
            {
                StringArgCallback = stringArgCallback;
                Port = port;

                OpenAsServer();

                _eventLogger.Info("Listening on port {0}", port);
                _serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to connect to remote socket " + e.Message);
            }
        }

        /// <summary>
        ///     <c>StartUp</c>
        ///     Constructor helper that can be called on an empty instance
        /// </summary>
        /// <param name="port"></param>
        /// <param name="byteArgCallback"></param>
        public void StartUp(int port, ByteByteDelegate byteArgCallback = null)
        {
            _openForListening = true;
            _eventLogger = LogManager.GetLogger("motCommonLib.Socket");

            MotSetCertificate();

            if (SocketMutex == null) SocketMutex = new Mutex();

            try
            {
                ByteArgCallback = byteArgCallback;
                Port = port;
                _doBinaryIo = true;

                OpenAsServer();

                _eventLogger.Info($"Listening on port: {port}");
                _serviceRunning = true;
            }
            catch (Exception e)
            {
                throw new Exception($"Failed to connect to remote socket {e.Message}");
            }
        }

        /// <summary>
        ///     <c>ValidateServerCertificate</c>
        ///     Ensures the certificate being used by the listener is valid
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="serverCert"></param>
        /// <param name="certChain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public bool ValidateServerCertificate(object sender, X509Certificate serverCert, X509Chain certChain, SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None) return true;

            _eventLogger.Error("Server certificate validation errors: {0}", sslPolicyErrors);

            return false; // burn the connection
        }

        /// <summary>
        ///     <c>~motSocket</c>
        ///     Simple destructor that calls the Dispose function
        /// </summary>
        ~MotSocket()
        {
            Dispose(false);
        }

        /// <summary>
        ///     <c>AsyncHandler</c>
        ///     Callback to handle inbound traffic and process it using the registered processing callback
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
                var localListener = (TcpListener)asyncResult.AsyncState;

                using (var tcpClient = localListener.EndAcceptTcpClient(asyncResult))
                {
                    using (var localStream = tcpClient.GetStream())
                    {
                        if (localStream.CanRead)
                        {
                            localStream.ReadTimeout = TcpTimeout;
                            localStream.WriteTimeout = TcpTimeout;

                            RemoteEndPoint = tcpClient.Client.RemoteEndPoint;
                            _localEndPoint = tcpClient.Client.LocalEndPoint;

                            var totalBytes = 0;

                            var bytesRead = new byte[1];

                            try
                            {
                                _byteIoBuffer = new byte[512];
                                _stringIoBuffer = "";

                                // Handle a touch
                                if (!localStream.DataAvailable)
                                {
                                    var ret = $"Medicine-On-Time Gateway Interface {DateTime.UtcNow.ToString()}";

                                    if (_preferASCII)
                                    {
                                        localStream.Write(Encoding.ASCII.GetBytes(ret), 0, ret.Length);
                                    }
                                    else
                                    {
                                        localStream.Write(Encoding.UTF8.GetBytes(ret), 0, ret.Length);
                                    }

                                    TcpClientConnected.Set();
                                    return;
                                }

                                while (localStream.DataAvailable)
                                {
                                    var bytesIn = localStream.Read(_byteIoBuffer, 0, _byteIoBuffer.Length);

                                    if (!_doBinaryIo)
                                    {
                                        _stringIoBuffer += Encoding.UTF8.GetString(_byteIoBuffer, 0, bytesIn);
                                    }
                                    else
                                    {
                                        Array.Resize(ref bytesRead, totalBytes + 512);
                                        _byteIoBuffer.CopyTo(bytesRead, totalBytes);
                                    }

                                    totalBytes += bytesIn;
                                }
                            }
                            catch (IOException ix)
                            {
                                _eventLogger.Warn($"AsyncRead: {ix.Message}");
                            }
                            catch (Exception ex)
                            {
                                var errorMsg = $"AsyncRead failed: {ex.Message}. {totalBytes} bytes read";
                                _eventLogger.Error(errorMsg);
                                throw new Exception(errorMsg);
                            }

                            if (totalBytes > 0)
                            {
                                // Assign the local stream to the global and process
                                NetStream = localStream;

                                if (!_doBinaryIo)
                                {
                                    var resp = StringArgCallback?.Invoke(_stringIoBuffer);
                                    if (!string.IsNullOrEmpty(resp))
                                    {
                                        if (_preferASCII)
                                        {
                                            localStream.Write(Encoding.ASCII.GetBytes(resp), 0, resp.Length);
                                        }
                                        else
                                        {
                                            localStream.Write(Encoding.UTF8.GetBytes(resp), 0, resp.Length);
                                        }
                                    }
                                }
                                else
                                {
                                    // Resize the byte array to match the data size
                                    Array.Resize(ref bytesRead, totalBytes);
                                    var resp = ByteArgCallback?.Invoke(bytesRead);

                                    if (resp != null && resp.Length > 0)
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
            catch (Exception ex)
            {
                _eventLogger.Error($"Async I/O handler: {ex.Message}");
                TcpClientConnected.Set();
            }
        }

        /// <summary>
        ///     <c>ListenAsync</c>
        ///     Async listener, hands off to AsyncHandler on tap
        /// </summary>
        public void ListenAsync()
        {
            while (_serviceRunning)
            {
                TcpClientConnected.Reset();
                _messageTrigger.BeginAcceptTcpClient(AsyncHandler, _messageTrigger);
                TcpClientConnected.WaitOne();
            }
        }

        /// <summary>
        ///     <c>SecureAsyncHandler</c>
        ///     Callback to handle TLS/SSL inbound traffic and process it using the registered processing callback
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
                        sslStream.AuthenticateAsServer(_x509Certificate, false, SslProtocols.Default, false);

                        var totalBytes = 0;

                        try
                        {
                            Thread.Sleep(1);

                            _byteIoBuffer = new byte[1024];
                            _stringIoBuffer = "";

                            int bytesIn;

                            if (sslStream.Length == 0)
                            {
                                // Handle a touch
                                var ret = $"Medicine-On-Time SSL Gateway Interface {DateTime.UtcNow.ToString()}";

                                if (_preferASCII)
                                {
                                    sslStream.Write(Encoding.ASCII.GetBytes(ret), 0, ret.Length);
                                }
                                else
                                {
                                    sslStream.Write(Encoding.UTF8.GetBytes(ret), 0, ret.Length);
                                }

                                TcpClientConnected.Set();
                                return;
                            }

                            do
                            {
                                bytesIn = sslStream.Read(_byteIoBuffer, 0, _byteIoBuffer.Length);
                                ByteArgCallback?.Invoke(_byteIoBuffer);
                                _stringIoBuffer += Encoding.UTF8.GetString(_byteIoBuffer, 0, bytesIn);
                                totalBytes += bytesIn;
                            } while (bytesIn > 0);
                        }
                        catch (IOException iox)
                        {
                            var errorMsg = $"SecureAsyncHandler read() failed: {iox.Message}";
                            _eventLogger.Error(errorMsg);
                            throw new Exception(errorMsg);
                        }
                        catch (Exception ex)
                        {
                            var errorMsg = $"SecureAsyncHandler read() failed: {ex.Message}";
                            _eventLogger.Error(errorMsg);
                            throw new Exception(errorMsg);
                        }

                        // Assuming we can stop blocking the port, probably wrong thinking
                        TcpClientConnected.Set();

                        if (totalBytes > 0)
                        {
                            //__ssl_stream = __lstream;
                            StringArgCallback?.Invoke(_stringIoBuffer);
                            var resp = StringArgCallback?.Invoke(_stringIoBuffer);

                            if (!string.IsNullOrEmpty(resp))
                            {
                                if (_preferASCII)
                                {
                                    sslStream.Write(Encoding.ASCII.GetBytes(resp), 0, resp.Length);
                                }
                                else
                                {
                                    sslStream.Write(Encoding.UTF8.GetBytes(resp), 0, resp.Length);
                                }
                            }
                        }

                        //SslStream.Close();
                        //TcpClient.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error("Secure Read: {0}", ex.Message);
            }
            finally
            {
                TcpClientConnected.Set();
            }
        }

        /// <summary>
        ///     <c>SecureListenAsync</c>
        ///     Async listener on a TLS/SSL port, hands off to SecureAsyncHandler on tap
        /// </summary>
        public void SecureListenAsync()
        {
            if (_x509Certificate == null)
            {
                throw new ArgumentNullException($"Missing X509 Certificate");
            }

            while (_serviceRunning)
            {
                TcpClientConnected.Reset();
                _messageTrigger.BeginAcceptTcpClient(SecureAsyncHandler, _messageTrigger);
                TcpClientConnected.WaitOne();
            }
        }

        /// <summary>
        ///     <c>SecureListen</c>
        ///     Synchronus listener on a TLS/SSL port, processes everything locally
        /// </summary>
        public void SecureListen()
        {
            if (_x509Certificate == null) throw new ArgumentNullException($"Missing X509 Certificate");

            _serviceRunning = true;

            while (_serviceRunning)
                try
                {
                    using (_localTcpClient = _messageTrigger.AcceptTcpClient())
                    {
                        NetSslStream = new SslStream(_localTcpClient.GetStream(), false) { ReadTimeout = TcpTimeout, WriteTimeout = TcpTimeout };

                        RemoteEndPoint = _localTcpClient.Client.RemoteEndPoint;
                        _localEndPoint = _localTcpClient.Client.LocalEndPoint;

                        NetSslStream.AuthenticateAsServer(_x509Certificate, false, SslProtocols.Tls, true);

                        _eventLogger.Info($"Accepted and Authenticated TLS connection from remote endpoint: {RemoteEndPoint}");

                        if (Read() > 0)
                        {
                            var ret = StringArgCallback?.Invoke(_stringIoBuffer);

                            if (_preferASCII)
                            {
                                NetSslStream.Write(Encoding.ASCII.GetBytes(ret), 0, ret.Length);
                            }
                            else
                            {
                                NetSslStream.Write(Encoding.UTF8.GetBytes(ret), 0, ret.Length);
                            }
                        }

                        NetSslStream.Close();
                        _localTcpClient.Close();
                    }
                }
                catch (IOException ex)
                {
                    _eventLogger.Error($"Create secure stream I/O error: {ex.StackTrace}");
                }
                catch (AuthenticationException ex)
                {
                    _eventLogger.Error($"Create secure stream Authentication error: {ex.StackTrace}");
                }
                catch (InvalidOperationException ex)
                {
                    // probably shutting the thread down
                    _serviceRunning = false;
                    Console.WriteLine(ex.Message);
                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
        }

        /// <summary>
        ///     <c>Listen</c>
        ///     Synchronus listener on a clear text port, processes everything locally
        /// </summary>
        public void Listen()
        {
            _serviceRunning = true;

            while (_serviceRunning)
            {
                try
                {
                    using (var tcpClient = _messageTrigger.AcceptTcpClient())
                    {
                        using (NetStream = tcpClient.GetStream())
                        {
                            NetStream.ReadTimeout = TcpTimeout;
                            NetStream.WriteTimeout = TcpTimeout;

                            RemoteEndPoint = tcpClient.Client.RemoteEndPoint;
                            _localEndPoint = tcpClient.Client.LocalEndPoint;

                            _eventLogger.Debug($"Accepted connection from remote endpoint {RemoteEndPoint}");

                            if (Read() > 0)
                            {
                                var ret = StringArgCallback?.Invoke(_stringIoBuffer);

                                if (_preferASCII)
                                {
                                    NetStream.Write(Encoding.ASCII.GetBytes(ret), 0, ret.Length);
                                }
                                else
                                {
                                    NetStream.Write(Encoding.UTF8.GetBytes(ret), 0, ret.Length);
                                }
                            }
                        }
                    }
                }
                catch (IOException iox)
                {
                    _eventLogger.Error($"listen() failed: {iox.Message}");
                    throw;
                }
                catch (InvalidOperationException ex)
                {
                    // probably shutting the thread down
                    _serviceRunning = false;
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
        ///     <c>Read</c>
        ///     Managed stream reader for clear and secure cnnections with muutex access management
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

                _byteIoBuffer = new byte[1024];
                _stringIoBuffer = "";

                int bytesIn;

                if (UseSsl)
                {
                    do
                    {
                        bytesIn = NetSslStream.Read(_byteIoBuffer, 0, _byteIoBuffer.Length);
                        _stringIoBuffer += Encoding.UTF8.GetString(_byteIoBuffer, 0, bytesIn);
                        totalBytes += bytesIn;

                    } while (bytesIn > 0);
                }
                else
                {
                    while (NetStream.DataAvailable)
                    {
                        bytesIn = NetStream.Read(_byteIoBuffer, 0, _byteIoBuffer.Length);
                        _stringIoBuffer += Encoding.UTF8.GetString(_byteIoBuffer, 0, bytesIn);
                        totalBytes += bytesIn;
                    }
                }

                return totalBytes;
            }
            catch (IOException iox)// timeout
            {
                _eventLogger.Error("read() failed: {0}", iox.Message);
                throw;

            }
            catch (Exception ex) // Error
            {
                _eventLogger.Error("read() failed: {0}", ex.Message);
                throw;
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     <c>Read</c>
        ///     Managed stream reader for clear and secure cnnections with muutex access management
        ///     that takes a byte buffer reference to return read data and specifies starting index
        ///     and number of bytes to read.
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
                    readCount = NetSslStream.Read(byteBuffer, index, count);
                    ByteArgCallback?.Invoke(_byteIoBuffer);
                }
                else
                {
                    readCount = NetStream.Read(byteBuffer, index, count);
                    ByteArgCallback?.Invoke(_byteIoBuffer);
                }

                return readCount;
            }
            catch
            {
                return 0;
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }


        /// <summary>
        ///     <c>DefaultProtocolProcessor</c>
        ///     Placholder delegate for processing return values from the Remote endpoint
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private static bool DefaultProtocolProcessor(byte[] buffer)
        {
            if (buffer[0] == '\x6')
            {
                return true;
            }

            if (Encoding.UTF8.GetString(buffer).ToLower().Substring(0, 2) == "ok")
            {
                return true;
            }

            return false;
        }

        /// <summary>
        ///     <c>WriteReturn</c>
        ///     Write method that sends a byte array and does not wait for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        public void WriteReturn(byte[] streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                if (UseSsl)
                {
                    NetSslStream.Write(streamData, 0, streamData.Length);
                }
                else
                {
                    NetStream.Write(streamData, 0, streamData.Length);
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error writing byte return values: {ex.Message}");
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     <c>WriteReturn</c>
        ///     Write method that sends a string and does not wait for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        public void WriteReturn(string streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                if (UseSsl)
                {
                    if (_preferASCII)
                    {
                        NetSslStream.Write(Encoding.ASCII.GetBytes(streamData), 0, streamData.Length);
                    }
                    else
                    {
                        NetSslStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    }

                }
                else
                {
                    if (_preferASCII)
                    {
                        NetStream.Write(Encoding.ASCII.GetBytes(streamData), 0, streamData.Length);
                    }
                    else
                    {
                        NetStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    }
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Error writing string return values: {ex.Message}");
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     <c>Write</c>
        ///     Write method that sends a byte array and waits for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succedded</returns>
        public bool Write(byte[] streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                var buffer = new byte[256];

                if (UseSsl)
                {
                    NetSslStream.Write(streamData, 0, streamData.Length);
                    NetSslStream.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    NetStream.Write(streamData, 0, streamData.Length);
                    NetStream.Read(buffer, 0, buffer.Length);
                }

                return ByteArgProtocolProcessor != null && (bool)ByteArgProtocolProcessor?.Invoke(buffer);
            }
            catch (IOException iox) // timeout
            {
                _eventLogger.Error($"Error write() failed: {iox.Message}");
                return false;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Error write() failed: {ex.Message}";
                _eventLogger.Error(errorMsg);
                throw new Exception(errorMsg);
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     <c>Write</c>
        ///     Write method that sends a string and waits for responses from the Remote endpoint
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succeded</returns>
        public bool Write(string streamData)
        {
            SocketMutex.WaitOne();

            try
            {
                var buffer = new byte[256];
                var len = 0;
                var retval = false;

                if (UseSsl)
                {
                    if (_preferASCII)
                    {
                        NetSslStream.Write(Encoding.ASCII.GetBytes(streamData), 0, streamData.Length);
                    }
                    else
                    {
                        NetSslStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    }

                    len = NetSslStream.Read(buffer, 0, buffer.Length);
                }
                else
                {
                    //var bytes = Encoding.UTF8.GetBytes(streamData);
                    //var str = Encoding.UTF8.GetString(bytes);

                    if (_preferASCII)
                    {
                        NetStream.Write(Encoding.ASCII.GetBytes(streamData), 0, streamData.Length);
                    }
                    else
                    {
                        NetStream.Write(Encoding.UTF8.GetBytes(streamData), 0, streamData.Length);
                    }

                    len = NetStream.Read(buffer, 0, buffer.Length);
                }

                if (len > 0)
                {
                    retval = ByteArgProtocolProcessor != null && (bool)ByteArgProtocolProcessor?.Invoke(buffer);
                }

                return retval;
            }
            catch (SocketException sx)
            {
                if (sx.ErrorCode == 10053)
                {
                    _eventLogger.Warn($"Write() SocketException. Type = {sx.GetType().Name}, {sx.Message}");
                }

                _eventLogger.Error($"Write() Exception. Type = {sx.GetType().Name}, {sx.Message}");
                return false;
            }
            catch (IOException iox)
            {
                _eventLogger.Error($"Write() Exception. Type = {iox.GetType().Name}, {iox.Message}");
                return false;
            }
            catch (Exception ex)
            {
                var errorMsg = $"Write() failed: {ex.Message}";
                _eventLogger.Error(errorMsg);
                throw new Exception(errorMsg);
            }
            finally
            {
                SocketMutex.ReleaseMutex();
            }
        }

        /// <summary>
        ///     <c>Send</c>
        ///     Overload of Write sending a byte array
        /// </summary>
        /// <param name="streamData"></param>
        /// <returns>A bool indicating if the write succeded</returns>
        public bool Send(byte[] streamData)
        {
            return Write(streamData);
        }

        /// <summary>
        ///     <c>Flush</c>
        ///     A simple overload of the underlying streams to flush any data
        /// </summary>
        public void Flush()
        {
            try
            {
                if (UseSsl)
                {
                    NetSslStream.Flush();
                }
                else
                {
                    NetStream.Flush();
                }
            }
            catch (Exception ex)
            {
                throw new IOException($"Stream flush failure {ex.Message}");
            }
        }

        /// <summary>
        ///     <c>Close</c>
        ///     Closes the socket, haltuing all I/O but doese not destruct and can be reopened
        /// </summary>
        public void Close()
        {
            if (_serviceRunning)
            {
                try
                {
                    _serviceRunning = false;

                    if (_openForListening)
                    {
                        _messageTrigger.Stop();
                    }

                    _localTcpClient?.Close();
                    NetStream?.Close();
                }
                catch (Exception ex)
                {
                    throw new Exception($"Socket close failure {ex.Message}");
                }
            }
        }

        /// <summary>
        ///     <c>OpenAsServer</c>
        ///     Sets up everything needed to be a listener on the local endpoint
        /// </summary>
        public void OpenAsServer()
        {
            try
            {
                _messageTrigger = new TcpListener(IPAddress.Any, Port);
                _messageTrigger.Start();

                _openForListening = true;
                _serviceRunning = true;
            }
            catch (Exception ex)
            {
                _eventLogger.Error("Failed to start server on: {0} : {1}", Port, ex.Message);
                throw;
            }
        }

        /// <summary>
        ///     <c>OpenAsClient</c>
        ///     Sets up everything needed to act as a client to a remote endpoint
        /// </summary>
        public void OpenAsClient()
        {
            _localTcpClient = new TcpClient(Address, Port);

            if (UseSsl)
            {
                // Resolve the machine name for the certificate
                var hostName = Dns.GetHostEntry(Address).HostName;
                if (string.IsNullOrEmpty(hostName)) hostName = Address;

                NetSslStream = new SslStream(_localTcpClient.GetStream(), false, ValidateServerCertificate, null);

                try
                {
                    NetSslStream.AuthenticateAsClient(hostName);
                }
                catch (AuthenticationException ax)
                {
                    _eventLogger.Error($"[Authentication] Failed to connect securely to {Address}:{Port}. Error: {ax.Message}");
                    _localTcpClient.Close();
                    throw;
                }
                catch (IOException iox)
                {
                    _eventLogger.Error($"[SystemIO] Failed to connect securely to {Address}:{Port}. Error: {iox.Message}");
                    _localTcpClient.Close();
                    throw;
                }

                NetSslStream.ReadTimeout = TcpTimeout;
                NetSslStream.WriteTimeout = TcpTimeout;
            }
            else
            {
                NetStream = _localTcpClient.GetStream();
                NetStream.ReadTimeout = TcpTimeout;
                NetStream.WriteTimeout = TcpTimeout;
            }

            RemoteEndPoint = _localTcpClient.Client.RemoteEndPoint;
            _localEndPoint = _localTcpClient.Client.LocalEndPoint;

            _openForListening = false;
        }

        /// <summary>
        ///     <c>Dispose</c>
        ///     Conditional IDisposable destructor
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    if (_x509Store != null)
                    {
                        _x509Store.Dispose();
                        _x509Store = null;
                    }

                    if (TcpClientConnected != null)
                    {
                        TcpClientConnected.Dispose();
                        TcpClientConnected = null;
                    }

                    if (_localTcpClient != null)
                    {
                        _localTcpClient.GetStream().Close();
                        _localTcpClient.Close();
                        _localTcpClient = null;
                    }

                    if (_openForListening)
                    {
                        if (_messageTrigger != null)
                        {
                            _messageTrigger.Stop();
                            _messageTrigger.Server.Dispose();
                        }
                    }
                    else
                    {
                        if (NetStream != null)
                        {
                            NetStream.Dispose();
                            NetStream = null;
                        }

                        if (UseSsl)
                            if (NetSslStream != null)
                            {
                                NetSslStream.Dispose();
                                NetSslStream = null;
                            }
                    }

                    Disposed = true;
                }
                catch (Exception ex)
                {
                    _eventLogger.Error($"Error disposing socket: {ex.Message}");
                }
            }
        }
#pragma warning disable 1591
        public NetworkStream NetStream;
        public SslStream NetSslStream;
#pragma warning restore 1591
    }
}