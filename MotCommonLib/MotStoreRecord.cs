// 
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
using System.Net.Sockets;
using System.Xml.Serialization;
using Newtonsoft.Json;

namespace Mot.Common.Interface.Lib
{
    /// <summary>
    /// Store Record (Key == A)
    /// </summary>
    [Serializable]
    [XmlRoot("MotStoreRecord")]
    public class MotStoreRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void CreateRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Store", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_StoreID", "", 36, true, 'k'));
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
            // ReSharper disable once RedundantCatchClause
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
            recordType = RecordType.Store;
        }

        /// <summary>
        /// MotStoreRecord
        /// </summary>
        /// <param name="action"></param>
        /// <param name="autoTruncate"></param>
        /// <exception cref="Exception"></exception>
        public MotStoreRecord(string action, bool autoTruncate = false)
        {
            recordType = RecordType.Store;
            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                CreateRecord(action);
            }
            catch (Exception ex)
            {
                var errorStirng = $"Failed to create Store record: {ex.Message}";
                EventLogger.Error(errorStirng);
                throw new Exception(errorStirng);
            }
        }

        public void SetField(string fieldName, string val, bool overrideTruncation = false)
        {
            try
            {
                base.SetField(_fieldList, val, fieldName, overrideTruncation);
            }
            // ReSharper disable once RedundantCatchClause
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// AddToQueue
        /// </summary>
        /// <param name="newQueue"></param>
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_storeid")));

            if (f != null && !string.IsNullOrEmpty(f.TagData))
            {
                if (newQueue != null)
                {
                    LocalWriteQueue = newQueue;
                }

                AddToQueue("A", _fieldList);
            }
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="doLogging"></param>
        /// <exception cref="Exception"></exception>
        public void Write(MotSocket socket)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(socket, _fieldList);
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
        /// <param name="logRecords"></param>
        /// <exception cref="Exception"></exception>
        public void Write(NetworkStream stream)
        {
            try
            {
                if (QueueWrites)
                {
                    AddToQueue();
                }
                else
                {
                    Write(stream, _fieldList);
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

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        /// <value>The store identifier.</value>
        [JsonProperty("RxSys_StoreID")]
        [XmlElement("RxSys_StoreID")]
        // ReSharper disable once InconsistentNaming
        public string RxSys_StoreID
        {
            get
            {

                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_storeid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? throw new ArgumentException("Store record must have an ID"), "RxSys_StoreID");
        }

        /// <summary>
        /// StoreID
        /// </summary>
        [JsonProperty("StoreID")]
        [XmlElement("StoreID")]
        public string StoreID
        {
            get { return RxSys_StoreID; }
            set => RxSys_StoreID = value;
        }

        /// <summary>
        /// Gets or sets the name of the store.
        /// </summary>
        /// <value>The name of the store.</value>
        [JsonProperty("StoreName")]
        [XmlElement("StoreName")]
        public string StoreName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("storename")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "StoreName");
        }

        /// <summary>
        /// Gets or sets the address1.
        /// </summary>
        /// <value>The address1.</value>
        [JsonProperty("Address1")]
        [XmlElement("Address1")]
        public string Address1
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("address1")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Address1");
        }

        /// <summary>
        /// Gets or sets the address2.
        /// </summary>
        /// <value>The address2.</value>
        [JsonProperty("Address2")]
        [XmlElement("Address2")]
        public string Address2
        {

            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("address2")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Address2");
        }

        /// <summary>
        /// Gets or sets the city.
        /// </summary>
        /// <value>The city.</value>
        [JsonProperty("City")]
        [XmlElement("City")]
        public string City
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("city")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "City");
        }

        /// <summary>
        /// Gets or sets the state.
        /// </summary>
        /// <value>The state.</value>
        [JsonProperty("State")]
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("state")));
                return f?.TagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    SetField(_fieldList, !string.IsNullOrEmpty(value) ? value.Substring(0,2).ToUpper() : "XX", "State");
                }
            }
        }

        /// <summary>
        /// Gets or sets the zipcode.
        /// </summary>
        /// <value>The zipcode.</value>
        [JsonProperty("Zipcode")]
        [XmlElement("Zipcode")]
        public string Zipcode
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("zip")));
                return f?.TagData;
            }

            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    while (value.Contains("-"))
                    {
                        value = value.Remove(value.IndexOf("-", StringComparison.Ordinal), 1);
                    }
                }

                SetField(_fieldList, NormalizeString(value), "Zip");
            }
        }

        /// <summary>
        /// Gets or sets the phone.
        /// </summary>
        /// <value>The phone.</value>
        [JsonProperty("Phone")]
        [XmlElement("Phone")]
        public string Phone
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("phone")));
                return f?.TagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? string.Empty), "Phone");
        }

        /// <summary>
        /// Gets or sets the fax.
        /// </summary>
        /// <value>The fax.</value>
        [JsonProperty("Fax")]
        [XmlElement("Fax")]
        public string Fax
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("fax")));
                return f?.TagData;
            }

            set => SetField(_fieldList, NormalizeString(value ?? string.Empty), "Fax");
        }

        /// <summary>
        /// Gets or sets the DEAN um.
        /// </summary>
        /// <value>The DEAN um.</value>
        [JsonProperty("DEANum")]
        [XmlElement("DEANum")]
        public string DEANum
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("deanum")));
                return f?.TagData;
            }

            set => SetField(_fieldList, ValidateDea(value ?? "XX0000000"), "DEANum");
        }

        /// <summary>
        /// Sets the web site.
        /// </summary>
        /// <value>The web site.</value>
        [JsonProperty("WebSite")]
        [XmlElement("WebSite")]
        public string WebSite
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));

                if (f != null)
                {
                    f.TagData += $"\nWebsite: {value ?? "none"}\n";
                }
            }
        }

        /// <summary>
        /// Sets the email.
        /// </summary>
        /// <value>The email.</value>
        [JsonProperty("Email")]
        [XmlElement("Email")]
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));

                if (f != null)
                {
                    f.TagData += $"\nEmail: {value ?? "none"}\n";
                }
            }
        }
    }
}