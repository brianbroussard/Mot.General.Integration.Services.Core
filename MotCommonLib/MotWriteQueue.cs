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
    public class MotWriteQueue
    {

        private static Mutex _queueMutex;
        private List<KeyValuePair<string, string>> Records { get; set; } = null;
        public bool SendEof { get; set; } = false;
        public bool LogRecords { get; set; } = false;
        private readonly Logger EventLogger;

        /// <inheritdoc />
        public MotWriteQueue()
        {
            Records = new List<KeyValuePair<string, string>>();
            EventLogger = LogManager.GetLogger("WriteQueue.Record");

            if (_queueMutex == null)
            {
                _queueMutex = new Mutex();
            }
        }
        ~MotWriteQueue()
        {
            Records?.Clear();
        }
        private int compare(KeyValuePair<string, string> a, KeyValuePair<string, string> b)
        {
            return string.Compare(a.Key, b.Key);
        }
        public void Add(string __type, string __record)
        {
            if (string.IsNullOrEmpty(__type) || string.IsNullOrEmpty(__record))
            {
                throw new ArgumentNullException("motQueue.Add NULL argument");
            }

            Records.Add(new KeyValuePair<string, string>(__type, __record));
            Records.Sort(compare);
        }
        public void Write(MotSocket Socket)
        {
            string __big_buf = string.Empty;

            if (Socket == null)
            {
                throw new ArgumentNullException("motQueue Null Socket Argument");
            }

            //__queue_mutex.WaitOne();

            //Console.WriteLine("Queue writing on thread {0}({1})", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId);

            try
            {
                // Push it to the port
                foreach (KeyValuePair<string, string> __record in Records)
                {
                    Socket.Write(__record.Value);

                    if (LogRecords)
                    {
                        EventLogger.Debug(__record);
                    }
                }

                // Flush
                Records.Clear();
                //__queue_mutex.ReleaseMutex();
            }
            catch (Exception ex)
            {
                //__queue_mutex.ReleaseMutex();
                throw new Exception("Failed to write queue to Socket: " + ex.Message);
            }
        }
        public void Write(NetworkStream Stream)
        {
            string __big_buf = string.Empty;

            if (Stream == null)
            {
                throw new ArgumentNullException("motQueue Null Socket Argument");
            }

            //__queue_mutex.WaitOne();

            //Console.WriteLine("Queue writing on thread {0}({1})", Thread.CurrentThread.Name, Thread.CurrentThread.ManagedThreadId);

            try
            {
                var buf = new Byte[1024];
                var bytesRead = 0;

                // Push it to the port
                foreach (KeyValuePair<string, string> Record in Records)
                {
                    Stream.Write(Encoding.UTF8.GetBytes(Record.Value), 0, Record.Value.Length);
                    bytesRead = Stream.Read(buf, 0, buf.Length);

                    if (LogRecords)
                    {
                        EventLogger.Debug(Record);
                    }
                }

                // Flush
                Records.Clear();
            }
            catch (Exception ex)
            {
                throw new Exception("Failed to write queue to Stream: " + ex.Message);
            }
        }
        public void WriteEOF(MotSocket __socket)
        {
            if (__socket == null)
            {
                throw new ArgumentNullException("motQueue Null Socket Argument");
            }

            if (SendEof)
            {
                try
                {
                    __socket.WriteReturn("<EOF/>");
                }
                catch (Exception ex)
                {
                    throw new Exception("Failed to write queue <EOF/>: " + ex.Message);
                }
            }
        }
        public void Clear()
        {
            Records.Clear();
        }
    }
}