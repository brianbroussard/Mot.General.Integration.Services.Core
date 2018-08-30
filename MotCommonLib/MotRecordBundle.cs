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

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    ///     <c>RecordBundle</c>
    ///     A collection of mot records that can be managed as a unit and written all at once. Spitting the records
    ///     to the gatweway all at once is significantly faster than sending them one at a time as they're built
    /// </summary>
    public class RecordBundle : IDisposable
    {
        private readonly Logger _eventLogger;

        private readonly MotWriteQueue _writeQueue;
        public MotDrugRecord Drug;
        public MotFacilityRecord Location;
        public MotPatientRecord Patient;
        public MotPrescriberRecord Prescriber;
        public List<MotPrescriberRecord> PrescriberList;
        public MotPrescriptionRecord Scrip;
        public MotStoreRecord Store;

        public List<MotStoreRecord> StoreList;
        public List<MotTimesQtysRecord> TQList;

        protected bool UseQueue = true;

        /// <summary>
        ///     Default constructor
        /// </summary>
        /// <param name="relaxTq">Allows 0 qty records to be sent to gateway</param>
        /// <param name="autoTruncate">Forces trim to max length for the field</param>
        /// <param name="sendEof">Forces stream closure</param>
        public RecordBundle(bool relaxTq = false, bool autoTruncate = false, bool sendEof = false)
        {
            SendEof = sendEof;

            _eventLogger = LogManager.GetLogger("motRecordBundle.Manager");
            Patient = new MotPatientRecord("Add", autoTruncate);
            Scrip = new MotPrescriptionRecord("Add", autoTruncate);
            Prescriber = new MotPrescriberRecord("Add", autoTruncate);
            Location = new MotFacilityRecord("Add", autoTruncate);
            Store = new MotStoreRecord("Add", autoTruncate);
            Drug = new MotDrugRecord("Add", autoTruncate);

            TQList = new List<MotTimesQtysRecord>();
            StoreList = new List<MotStoreRecord>();
            PrescriberList = new List<MotPrescriberRecord>();

            Patient.RelaxTqRequirements = Scrip.RelaxTqRequirements = Location.RelaxTqRequirements = Prescriber.RelaxTqRequirements = Store.RelaxTqRequirements = Drug.RelaxTqRequirements = relaxTq;

            if (UseQueue)
            {
                _writeQueue = new MotWriteQueue();
                Patient.LocalWriteQueue = Scrip.LocalWriteQueue = Location.LocalWriteQueue = Prescriber.LocalWriteQueue = Store.LocalWriteQueue = Drug.LocalWriteQueue = _writeQueue;

                _writeQueue.SendEof = sendEof;
                _writeQueue.DebugMode = DebugMode;
            }

            Patient.QueueWrites = Scrip.QueueWrites = Location.QueueWrites = Prescriber.QueueWrites = Store.QueueWrites = Drug.QueueWrites = UseQueue;

            Patient.SendEof = Scrip.SendEof = Location.SendEof = Prescriber.SendEof = Store.SendEof = Drug.SendEof = sendEof;

            Patient.AutoTruncate = Scrip.AutoTruncate = Location.AutoTruncate = Prescriber.AutoTruncate = Store.AutoTruncate = Drug.AutoTruncate = autoTruncate;
        }

        public bool MakeDupRnaScrip { get; set; } = false;
        public DateTime NewStartDate { get; set; }
        public bool SendEof { get; set; }
        public bool DebugMode { get; set; }
        public string MessageType { get; set; }

        /// <summary>
        ///     <c>Dispose</c>
        ///     Direct IDisposable destructor that destroys and nullifies everything
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     <c>SetDebug</c>
        ///     Toggles debugging for the current bundle
        /// </summary>
        /// <param name="on"></param>
        /// <returns></returns>
        public bool SetDebug(bool on)
        {
            _writeQueue.DebugMode = on;
            return _writeQueue.DebugMode;
        }

        /// <summary>
        ///     <c>Write</c>
        ///     Depending on the queuing model selected, Write feeds the queue or does nothing
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
                            foreach (var store in StoreList)
                            {
                                store.SendEof = SendEof;
                                store.AddToQueue(_writeQueue);
                            }
                        else
                            Store.AddToQueue(_writeQueue);

                        foreach (var tq in TQList)
                        {
                            tq.RelaxTqRequirements = Scrip.RelaxTqRequirements;
                            tq.SendEof = SendEof;
                            tq.AddToQueue(_writeQueue);
                        }

                        Drug.AddToQueue();
                        Scrip.AddToQueue();
                    }

                    if (PrescriberList.Count > 0)
                        foreach (var prescriber in PrescriberList)
                        {
                            prescriber.SendEof = SendEof;
                            prescriber.AddToQueue(_writeQueue);
                        }
                    else
                        Prescriber.AddToQueue(_writeQueue);

                    Patient.AddToQueue();
                    Location.AddToQueue();

                    //Clear();
                }
            }
            catch (Exception ex)
            {
                _eventLogger.Error($"Add To Bundle: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     <c>Clear</c>
        ///     CLears all the records out and resets Lists
        /// </summary>
        // ReSharper disable once UnusedMember.Global
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
        ///     <c>Commit</c>
        ///     Commits the queueud items to the database over ta socket
        /// </summary>
        /// <param name="socket"></param>
        // ReSharper disable once UnusedMember.Global
        public void Commit(MotSocket socket)
        {
            try
            {
                _writeQueue.Write(socket);
            }
            catch (Exception ex)
            {
                _eventLogger.Error("Commit Bundle: {0}", ex.Message);
                throw new Exception("Commit: " + ex.Message);
            }
        }

        /// <summary>
        ///     <c>Commit</c>
        ///     Commits the queueud items to the database over ta stream
        /// </summary>
        /// <param name="stream"></param>
        public void Commit(NetworkStream stream)
        {
            try
            {
                _writeQueue.Write(stream);
            }
            catch (Exception ex)
            {
                _eventLogger.Error("Commit Bundle: {0}", ex.Message);
                throw new Exception("Commit: " + ex.Message);
            }
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
    }
}