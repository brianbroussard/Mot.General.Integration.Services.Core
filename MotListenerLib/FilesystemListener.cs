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
using System.IO;
using System.Threading;
using NLog;
using MotCommonLib;
using System.Threading.Tasks;

namespace MotListenerLib
{
    public class FilesystemListener : IDisposable
    {
        public bool SendEof { get; set; }
        public bool DebugMode { get; set; }
        public bool AutoTruncate { get; set; }
        public bool Listening { get; set; }
        public bool RunAsTask { get; set; }
        public bool RunAsService {get;set;}
        public string DirName { get; set; }
        public string TargetIp { get; set; }
        public int TargetPort { get; set; }
        public StringStringDelegate StringProcessor { get; set; }
        private readonly Logger _eventLogger;

        public void WriteData()
        {
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

        public bool ProcessReturn(byte[] data)
        {
            switch (data[0])
            {
                case 0x06:
                    break;

                case 0x0A:
                    throw new Exception("Record Write Error:  Invalid Table Type (0x0A)");

                case 0x0B:
                    throw new Exception("Record Write Error: Invalid Action Type (0x0B)");

                case 0x0C:
                    throw new Exception("Record Write Error: <RECORD> Tags Missing (0x0C)");

                case 0x0D:
                    throw new Exception("Record Write Error: Empty Record (0x0D)");

                default:
                    throw new Exception("Record Write Error:  Unknown (" + data[0] + ")");
            }

            return true;
        }

        private void CheckDirectory(string dirName)
        {
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName ?? throw new ArgumentNullException(nameof(dirName)));
            }
        }

        public void WatchDirectory()
        {
            CheckDirectory(DirName);

            if (string.IsNullOrEmpty(DirName) || string.IsNullOrEmpty(TargetIp) || TargetPort == 0)
            {
                throw new ArgumentException($"Missing Dir or Address Argument");
            }

            Listening = true;

            while (Listening)
            {
                Thread.Sleep(1024);

                var fileEntries = Directory.GetFiles(DirName);

                if (fileEntries.Length <= 0)
                {
                    continue;
                }

                foreach (var fileName in fileEntries)
                {
                    if (fileName.Contains(".FAILED"))
                    {
                        continue;
                    }

                    try
                    {
                        using (var fileContent = new StreamReader(fileName))
                        {
                            StringProcessor?.Invoke(fileContent.ReadToEnd());
                        }
                        File.Delete(fileName);
                        
                    }
                    catch (Exception ex)
                    {
                        if (!File.Exists(fileName + ".FAILED"))
                        {
                            File.Move(fileName, fileName + ".FAILED");
                        }

                        if (File.Exists(fileName))
                        {
                            File.Delete(fileName);
                        }                       

                        _eventLogger.Error("Failed While Processing {0} : {1}", fileName, ex.Message);
                    }
                }
            }

            Console.WriteLine("Exiting Thread {0}", Thread.CurrentThread.Name);
        }

        public void WaitForWork()
        {
            if (string.IsNullOrEmpty(DirName) || StringProcessor == null)
            {
                throw new ArgumentException($"Missing Dir or Processor");
            }

            CheckDirectory(DirName);

            var t = Task.Run(() =>
            {
                while (Listening)
                {
                    Thread.Sleep(1024);

                    var fileEntries = Directory.GetFiles(DirName);

                    if (fileEntries.Length <= 0)
                    {
                        continue;
                    }

                    foreach (var fileName in fileEntries)
                    {
                        if (fileName.Contains(".FAILED"))
                        {
                            continue;
                        }

                        try
                        {
                            using (var fileContent = new StreamReader(fileName))
                            {
                                StringProcessor?.Invoke(fileContent.ReadToEnd());
                            }

                            File.Delete(fileName);
                        }
                        catch (Exception ex)
                        {
                            if (!File.Exists(fileName + ".FAILED"))
                            {
                                File.Move(fileName, fileName + ".FAILED");
                            }

                            if (File.Exists(fileName))
                            {
                                File.Delete(fileName);
                            }

                            _eventLogger.Error(ex.Message);
                        }
                    }
                }
            });

            t.Wait();
        }

        public void Go(StringStringDelegate stringProcessor = null, string dirName = null)
        {
            if (dirName != null)
            {
                DirName = dirName;
            }

            if (stringProcessor != null)
            {
                StringProcessor = stringProcessor;
            }

            
            if (RunAsTask)
            {
                if (StringProcessor != null)
                {
                    var workerThread = new Thread(WaitForWork)
                    {
                        Name = "Filesystem Listener"
                    };
                    workerThread.Start();

                    //WaitForWork();
                }
                else
                {
                    throw new Exception("Missing String Processor");
                }
            }
            else
            {
                var workerThread = new Thread(WatchDirectory)
                {
                    Name = "Filesystem Listener"
                };
                workerThread.Start();

               // WatchDirectory();
            }

        }

        public void ShutDown()
        {
            Listening = false;
        }

        public FilesystemListener()
        {
            Listening = false;
            _eventLogger = LogManager.GetLogger("FileSystemWatcher");
        }

        ///
        /// <c>MotFileSystemListener</c>
        /// Set up listener to run as a task with a processor callback
        /// <param name="dirName">The directory to monitor</param>
        /// <param name="stringProcessor">The method to call to process files</param>
        public FilesystemListener(string dirName, StringStringDelegate stringProcessor, bool RunAsTask = true)
        {
            this.RunAsTask = RunAsTask;

            CheckDirectory(dirName);

            DirName = dirName;
            StringProcessor = stringProcessor;
            _eventLogger = LogManager.GetLogger("FileSystemWatcher");

        }

        /// <summary>
        /// <c>MotFileSystemListener</c>
        /// Set up listener as a polling system
        /// </summary>
        /// <param name="dirName">The directory to monitor</param>
        /// <param name="address">The address to send processed data to</param>
        /// <param name="port">The port to send data to</param>
        public FilesystemListener(string dirName, string address, int port)
        {
            RunAsTask = false;

            CheckDirectory(dirName);

            DirName = dirName;
            TargetIp = address;
            TargetPort = port;
            _eventLogger = LogManager.GetLogger("FileSystemWatcher");
        }

        ~FilesystemListener()
        {
            Dispose(false);
        }
    }
}
