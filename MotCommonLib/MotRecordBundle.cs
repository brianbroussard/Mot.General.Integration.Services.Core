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
using System.Collections.Generic;
using System.Net.Sockets;
using NLog;

namespace MotCommonLib
{
    /// <summary>
    /// <c>RecordBundle</c>
    /// A collection of mot records that can be managed as a unit and written all at once. Spitting the records
    /// to the gatweway all at once is significantly faster than sending them one at a time as they're built
    /// </summary>
    public class RecordBundle : IDisposable
    {
        public MotPatientRecord Patient;
        public MotPrescriptionRecord Scrip;
        public MotPrescriberRecord Prescriber;
        public MotFacilityRecord Location;
        public MotStoreRecord Store;
        public MotDrugRecord Drug;
        public List<MotTimesQtysRecord> TQList;

        public List<MotStoreRecord> StoreList;
        public List<MotPrescriberRecord> PrescriberList;

        private Logger EventLogger;

        public bool MakeDupScrip { get; set; } = false;
        public DateTime NewStartDate { get; set; }

        private MotWriteQueue WriteQueue;
        protected bool UseQueue = true;
        public bool SendEof { get; set; }
        public bool DebugMode { get; set; }
        public string MessageType { get; set; }

        /// <summary>
        /// <c>SetDebug</c>
        /// Toggles debugging for the current bundle
        /// </summary>
        /// <param name="on"></param>
        /// <returns></returns>
        public bool SetDebug(bool on)
        {
            WriteQueue.LogRecords = on;
            return WriteQueue.LogRecords;
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="autoTruncate"></param>
        /// <param name="sendEof"></param>
        public RecordBundle(bool autoTruncate = false, bool sendEof = false)
        {
            this.SendEof = sendEof;
            EventLogger = LogManager.GetLogger("motRecordBundle.Manager");
            Patient = new MotPatientRecord("Add", autoTruncate);
            Scrip = new MotPrescriptionRecord("Add", autoTruncate);
            Prescriber = new MotPrescriberRecord("Add", autoTruncate);
            Location = new MotFacilityRecord("Add", autoTruncate);
            Store = new MotStoreRecord("Add", autoTruncate);
            Drug = new MotDrugRecord("Add", autoTruncate);

            TQList = new List<MotTimesQtysRecord>();
            StoreList = new List<MotStoreRecord>();
            PrescriberList = new List<MotPrescriberRecord>();

            if (UseQueue)
            {
                WriteQueue = new MotWriteQueue();
                Patient.LocalWriteQueue =
                Scrip.LocalWriteQueue =
                Location.LocalWriteQueue =
                Prescriber.LocalWriteQueue =
                Store.LocalWriteQueue =
                Drug.LocalWriteQueue =
                    WriteQueue;

                WriteQueue.SendEof = sendEof;
            }

            Patient.QueueWrites =
            Scrip.QueueWrites =
            Location.QueueWrites =
            Prescriber.QueueWrites =
            Store.QueueWrites =
            Drug.QueueWrites =
                UseQueue;

            Patient.SendEof =
            Scrip.SendEof =
            Location.SendEof =
            Prescriber.SendEof =
            Store.SendEof =
            Drug.SendEof =
                sendEof;

            Patient.AutoTruncate =
            Scrip.AutoTruncate =
            Location.AutoTruncate =
            Prescriber.AutoTruncate =
            Store.AutoTruncate =
            Drug.AutoTruncate =
                autoTruncate;

        }

        /// <summary>
        /// <c>Write</c>
        /// Depending on the queuing model selected, Write feeds the queue or does nothing
        /// </summary>
        public void Write()
        {
            try
            {
                if (UseQueue)
                {
                    if (MessageType != "ADT")
                    {
                        if (StoreList.Count > 0)
                        {
                            foreach (MotStoreRecord Store in StoreList)
                            {
                                Store.SendEof = SendEof;
                                Store.AddToQueue(WriteQueue);
                            }
                        }
                        else
                        {
                            Store.AddToQueue(WriteQueue);
                        }

                        foreach (MotTimesQtysRecord TQ in TQList)
                        {
                            TQ.SendEof = SendEof;
                            TQ.AddToQueue(WriteQueue);
                        }

                        Drug.AddToQueue();
                        Scrip.AddToQueue();
                    }

                    if (PrescriberList.Count > 0)
                    {
                        foreach (MotPrescriberRecord Prescriber in PrescriberList)
                        {
                            Prescriber.SendEof = SendEof;
                            Prescriber.AddToQueue(WriteQueue);
                        }
                    }
                    else
                    {
                        Prescriber.AddToQueue(WriteQueue);
                    }

                    Patient.AddToQueue();
                    Location.AddToQueue();

                    //Clear();
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error("Add To Bundle: {0}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// <c>Clear</c>
        /// CLears all the records out and resets Lists
        /// </summary>
        public void Clear()
        {
            StoreList.Clear();
            Location.Clear();
            PrescriberList.Clear();
            Patient.Clear();
            Drug.Clear();
            TQList.Clear();
            Scrip.Clear();
        }

        /// <summary>
        /// <c>Commit</c>
        /// Commits the queueud items to the database over ta socket 
        /// </summary>
        /// <param name="socket"></param>
        public void Commit(MotSocket socket)
        {
            try
            {
                WriteQueue.Write(socket);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Commit Bundle: {0}", ex.Message);
                throw new Exception("Commit: " + ex.Message);
            }
        }

        /// <summary>
        /// <c>Commit</c>
        /// Commits the queueud items to the database over ta stream 
        /// </summary>
        /// <param name="stream"></param>
        public void Commit(NetworkStream stream)
        {
            try
            {
                WriteQueue.Write(stream);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Commit Bundle: {0}", ex.Message);
                throw new Exception("Commit: " + ex.Message);
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// Conditional IDisposable destructor
        /// </summary>
        /// <param name="Disposing"></param>
        protected virtual void Dispose(bool Disposing)
        {
            if (Disposing)
            {
                Patient.Dispose();
                Scrip.Dispose();
                Prescriber.Dispose();
                Location.Dispose();
                Store.Dispose();
                Drug.Dispose();

                StoreList.Clear();
                StoreList = null;

                TQList.Clear();
                TQList = null;
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
}
