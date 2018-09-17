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
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    /// TimeQtys Record  (Key == F)
    /// </summary>
    [Serializable]
    [XmlRoot("MotTimesQtysRecord")]
    public class MotTimesQtysRecord : MotRecordBase
    {
        /// <summary>
        /// <c>FieldList</c>
        /// Collection of field objects representing a TQ Record
        /// </summary>
        private readonly List<Field> _fieldList;

        private void CreateRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "TimesQtys", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_LocID", "", 36, true, 'k'));
                _fieldList.Add(new Field("DoseScheduleName", "", 10, true, 'k'));
                _fieldList.Add(new Field("DoseTimesQtys", "", 256, true, 'a'));
            }
            // ReSharper disable once RedundantCatchClause
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// <c>motTimeQtysRecord</c>
        /// Constructor
        /// </summary>
        public MotTimesQtysRecord()
        {
        }

        /// <summary>
        /// <c>motTimeQtysRecord</c>
        /// Constructor with a specific action (Add, Change, Delete) and sets the auto truncate option
        /// </summary>
        public MotTimesQtysRecord(string action, bool autoTruncate = false)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                CreateRecord(action);
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to create TimeQtys record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// <c>AddToQueue</c>
        /// </summary>
        /// Adds item to queue, optionally overriding the default queue
        /// <param name="newQueue"></param>
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("F", _fieldList);
        }

        /// <summary>
        /// Writes the current FieldList do the passed Socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="doLogging"></param>
        public void Write(MotSocket socket, bool doLogging = false)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList, doLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write TQ record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// <c>Write</c>
        /// Writes current field list to the passed stream or queues it as appropriate
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="doLogging"></param>
        public void Write(NetworkStream stream, bool doLogging = false)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList, doLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write TQ record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// <c>Clear</c>
        /// Empties the current FieldList
        /// </summary>
        public void Clear()
        {
            Clear(_fieldList);
        }


        /// <summary>
        /// <c>LocationID</c>
        /// The ID of the facility that uses the new dose schedule
        /// </summary>
        [JsonProperty("LocationID")]
        [XmlElement("LocationID")]
        public string LocationID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_locid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_LocID");
        }

        /// <summary>
        /// <c>DoseScheduleName</c>
        /// The name of the new dose schedule
        /// </summary>
        [JsonProperty("DoseScheduleName")]
        [XmlElement("DoseScheduleName")]
        public string DoseScheduleName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("doseschedulename")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "None", "DoseScheduleName");
        }

        /// <summary>
        /// <c>DoseTimesQtys</c>
        /// The string representing the dose times/qtys
        /// 
        /// Weekly:   X0000X0 where 0 means no dose and X means dose
        /// Monthly:  As above but with 31 cells
        /// Daily:    hh:mmnn.nn repeated for each dose in a day
        /// </summary>
        [JsonProperty("DoseTimesQtys")]
        [XmlElement("DoseTimesQtys")]
        public string DoseTimesQtys
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dosetimesqtys")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "DoseTimesQtys");
        }
    }
}