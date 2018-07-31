using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using NLog;

namespace MotCommonLib
{
    /// <summary>
    /// <c>WriteQueue</c>
    /// A simple mechanism for collecting and ordering record objects by dependancy.  
    /// For example, A prescriber cannot write a Scrip without the Drug being available 
    /// and a Patient to write it for, so the send order would be Drug, Scrip, Patient, Prescriber
    /// </summary>
    public class MotWriteQueue :IDisposable
    {
        private static Mutex _queueMutex;
        private List<KeyValuePair<string, string>> Records { get; set; }
        public bool SendEof { get; set; } = false;
        public bool LogRecords { get; set; } = false;
        private readonly Logger _eventLogger;

        /// <inheritdoc />
        public MotWriteQueue()
        {
            Records = new List<KeyValuePair<string, string>>();
            _eventLogger = LogManager.GetLogger("WriteQueue");

            if (_queueMutex == null)
            {
                _queueMutex = new Mutex();
            }
        }

        ~MotWriteQueue()
        {
            Dispose();
        }

        private int CompareTypes(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return string.CompareOrdinal(a.Key, b.Key);
        }

        public void Add(string type, string record)
        {
            if (string.IsNullOrEmpty(type) || string.IsNullOrEmpty(record))
            {
                throw new ArgumentNullException($"motQueue.Add NULL argument");
            }

            Records.Add(new KeyValuePair<string, string>(type, record));
            Records.Sort(CompareTypes);
        }
        public void Write(MotSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException($"motQueue Null Socket Argument");
            }

            try
            {
                // Push it to the port
                foreach (var record in Records)
                {
                    socket.Write(record.Value);

                    if (LogRecords)
                    {
                        _eventLogger.Debug(record);
                    }
                }

                // Flush
                Records.Clear();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to write queue to Socket: " + ex.Message);
            }
        }
        public void Write(NetworkStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException($"motQueue Null Socket Argument");
            }

            try
            {
                var buf = new byte[1024];
                var bytesRead = 0;

                // Push it to the port
                foreach (var record in Records)
                {
                    stream.Write(Encoding.UTF8.GetBytes(record.Value), 0, record.Value.Length);
                    bytesRead = stream.Read(buf, 0, buf.Length);

                    if (LogRecords)
                    {
                        _eventLogger.Debug(record);
                    }
                }

                // Flush
                Records.Clear();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to write queue to Stream: {ex.Message}");
            }
        }
        public void WriteEof(MotSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException($"motQueue Null Socket Argument");
            }

            if (SendEof)
            {
                try
                {
                    socket.WriteReturn("<EOF/>");
                }
                catch (Exception ex)
                {
                    throw new Exception($"Failed to write queue <EOF/>: {ex.Message}");
                }
            }
        }

        public void Clear()
        {
            Records.Clear();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                Records.Clear();
                Records = null;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}