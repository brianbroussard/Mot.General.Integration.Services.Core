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

namespace MotCommonLib
{
    /// <summary>
    /// Store Record (Key == A)
    /// </summary>
    [XmlRoot("MotStoreRecord")]
    public class MotStoreRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void createRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Store", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_StoreID", "", 10, true, 'k'));
                _fieldList.Add(new Field("StoreName", "", 60, true, 'a'));
                _fieldList.Add(new Field("Address1", "", 40, false, 'n'));
                _fieldList.Add(new Field("Address2", "", 40, false, 'n'));
                _fieldList.Add(new Field("City", "", 30, false, 'n'));
                _fieldList.Add(new Field("State", "", 2, false, 'n'));
                _fieldList.Add(new Field("Zip", "", 9, true, 'w'));
                _fieldList.Add(new Field("Phone", "", 10, true, 'w'));
                _fieldList.Add(new Field("Fax", "", 10, false, 'a'));
                _fieldList.Add(new Field("DEANum", "", 10, false, 'a'));
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// MotStoreRecord
        /// </summary>
        public MotStoreRecord()
        {
        }

        /// <summary>
        /// MotStoreRecord
        /// </summary>
        /// <param name="Action"></param>
        /// <param name="AutoTruncate"></param>
        /// <exception cref="Exception"></exception>
        public MotStoreRecord(string Action, bool AutoTruncate = false) : base()
        {
            base.AutoTruncate = AutoTruncate;

            try
            {
                _fieldList = new List<Field>();
                createRecord(Action);
            }
            catch (Exception ex)
            {
                var errorStirng = $"Failed to create Store record: {ex.Message}";
                EventLogger.Error(errorStirng);
                throw new Exception(errorStirng);
            }
        }

        public void SetField(string FieldName, string Val, bool OverrideTruncation = false)
        {
            try
            {
                base.SetField(_fieldList, Val, FieldName, OverrideTruncation);
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// AddToQueue
        /// </summary>
        /// <param name="NewQueue"></param>
        public void AddToQueue(MotWriteQueue NewQueue = null)
        {
            var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_storeid")));

            if (f != null && !string.IsNullOrEmpty(f.tagData))
            {
                if (NewQueue != null)
                {
                    LocalWriteQueue = NewQueue;
                }

                AddToQueue("A", _fieldList);
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="DoLogging"></param>
        /// <exception cref="Exception"></exception>
        public void Write(MotSocket socket, bool DoLogging = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList, DoLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = string.Format("Failed to write Store record: {0}", ex.Message);
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        /// <summary>
        /// Write
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="DoLogging"></param>
        /// <exception cref="Exception"></exception>
        public void Write(NetworkStream stream, bool DoLogging = false)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList, DoLogging);
                }
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to write Store record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        /// <summary>
        /// Clear
        /// </summary>
        public void Clear()
        {
            Clear(_fieldList);
        }

        [XmlElement("RxSys_StoreID")]
        public string RxSys_StoreID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_storeid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_StoreID");
        }
        [XmlElement("StoreName")]
        public string StoreName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("storename")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "StoreName");
        }
        [XmlElement("Address1")]
        public string Address1
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address1")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Address1");
        }
        [XmlElement("Address2")]
        public string Address2
        {

            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address2")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Address2");
        }
        [XmlElement("City")]
        public string City
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("city")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "City");
        }
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("state")));
                return f?.tagData;
            }
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetField(_fieldList, value?.ToUpper(), "State");
                }
            }
        }
        [XmlElement("Zip")]
        public string Zip
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("zip")));
                return f?.tagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    while (value.Contains("-"))
                    {
                        value = value.Remove(value.IndexOf("-"), 1);
                    }
                }

                SetField(_fieldList, NormalizeString(value), "Zip");
            }
        }
        [XmlElement("Phone")]
        public string Phone
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("phone")));
                return f?.tagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? string.Empty), "Phone");
        }
        [XmlElement("Fax")]
        public string Fax
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("fax")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Fax");
        }
        [XmlElement("DEANum")]
        public string DEANum
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("deanum")));
                return f?.tagData;
            }

            set => SetField(_fieldList, ValidateDea(value), "DEANum");
        }
        [XmlElement("WebSite")]
        public string WebSite
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += string.Format("\nWebsite: {0}\n", value);
            }

        }
        [XmlElement("Email")]
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += $"\nEmail: {value}\n";
            }
        }
    }
}