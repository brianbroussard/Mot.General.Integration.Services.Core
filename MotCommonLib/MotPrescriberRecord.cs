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

namespace MotCommonLib
{
    /// <summary>
    /// Prescriber Record (Key == C)
    /// </summary>
    [Serializable]
    [XmlRoot("MotPrescriberRecord")]
    public class MotPrescriberRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void CreateRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Prescriber", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_DocID", "", 10, true, 'k'));
                _fieldList.Add(new Field("LastName", "", 30, true, 'a'));
                _fieldList.Add(new Field("FirstName", "", 20, true, 'a'));
                _fieldList.Add(new Field("MiddleInitial", "", 2, false, 'n'));
                _fieldList.Add(new Field("Address1", "", 40, true, 'w'));
                _fieldList.Add(new Field("Address2", "", 40, true, 'w'));
                _fieldList.Add(new Field("City", "", 30, true, 'w'));
                _fieldList.Add(new Field("State", "", 2, true, 'w'));
                _fieldList.Add(new Field("Zip", "", 9, true, 'w'));
                _fieldList.Add(new Field("Phone", "", 10, true, 'w'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("DEA_ID", "", 10, true, 'w'));
                _fieldList.Add(new Field("TPID", "", 10, false, 'n'));
                _fieldList.Add(new Field("Specialty", "", 2, false, 'n'));
                _fieldList.Add(new Field("Fax", "", 10, true, 'w'));
                _fieldList.Add(new Field("PagerInfo", "", 40, false, 'n'));
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Prescriber record construction: {ex.Message}");
                throw;
            }
        }
        /// <summary>
        /// Empty ctor
        /// </summary>
        public MotPrescriberRecord()
        {
        }

        /// <summary>
        /// ctor
        /// </summary>
        /// <param name="action"></param>
        /// <param name="autoTruncate"></param>
        public MotPrescriberRecord(string action, bool autoTruncate = false)
        {
            base.AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                CreateRecord(action);
            }
            catch (Exception ex)
            {
                var errorStirng = $"Failed to create Prescriber record: {ex.Message}";
                EventLogger.Error(errorStirng);
                Console.Write(errorStirng);
                throw new Exception(errorStirng);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="fieldName"></param>
        /// <param name="val"></param>
        /// <param name="overrideTruncation"></param>
        public void SetField(string fieldName, string val, bool overrideTruncation = false)
        {
            try
            {
                SetField(_fieldList, val, fieldName, overrideTruncation);
            }
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
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("C", _fieldList);
        }

        /// <summary>
        /// Write
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="doLogging"></param>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="Exception"></exception>
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
                var errorString = $"Failed to write Prescriber record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        /// <summary>
        /// Write 
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="doLogging"></param>
        /// <exception cref="Exception"></exception>
        public void Write(NetworkStream stream, bool doLogging = false)
        {
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
            catch (Exception e)
            {
                var errorString = $"Failed to write Prescriber record: {e.Message}";
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
        /// RxSys_DocID
        /// </summary>
        [JsonProperty("RxSys_DocID")]
        [XmlElement("RxSys_DocID")]
        public string RxSys_DocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_docid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }

        /// <summary>
        /// PrescriberID
        /// </summary>
        [JsonProperty("PrescriberID")]
        [XmlElement("PrescriberID")]
        public string PrescriberID
        {
            get { return RxSys_DocID; }
            set => RxSys_DocID = value;
        }

        /// <summary>
        /// LastName
        /// </summary>
        [JsonProperty("LastName")]
        [XmlElement("LastName")]
        public string LastName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("lastname")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "LastName");
        }

        /// <summary>
        /// FirstName
        /// </summary>
        [JsonProperty("FirstName")]
        [XmlElement("FirstName")]
        public string FirstName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("firstname")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "FirstName");
        }

        /// <summary>
        /// MiddleInitial
        /// </summary>
        [JsonProperty("MiddleInitial")]
        [XmlElement("MiddleInitial")]
        public string MiddleInitial
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("middleinitial")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "MiddleInitial");
        }

        /// <summary>
        /// Address1
        /// </summary>
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
        /// Address2
        /// </summary>
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
        /// City
        /// </summary>
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
        /// State
        /// </summary>
        [JsonProperty("State")]
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("state")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value?.ToUpper() ?? String.Empty, "State");
        }

        /// <summary>
        /// Zip
        /// </summary>
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
                        if (value.Length > value.IndexOf("-"))
                        {
                            value = value.Remove(value.IndexOf("-"), 1);
                        }
                    }

                    SetField(_fieldList, value, "Zip");
                }
                else
                {
                    SetField(_fieldList, string.Empty, "Zip");
                }
            }
        }

        /// <summary>
        /// Phone
        /// </summary>
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
        /// Comments
        /// </summary>
        [JsonProperty("Comments")]
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Comments");
        }

        /// <summary>
        /// DEA_ID
        /// </summary>
        [JsonProperty("DEA_ID")]
        [XmlElement("DEA_ID")]
        public string DEA_ID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("dea_id")));
                return f?.TagData;
            }

            set => SetField(_fieldList, ValidateDea(value ?? String.Empty), "DEA_ID");
        }

        /// <summary>
        /// TPID
        /// </summary>
        [JsonProperty("TPID")]
        [XmlElement("TPID")]
        public string TPID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("tpid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "TPID");
        }

        /// <summary>
        /// Specialty
        /// </summary>
        [JsonProperty("Speciality")]
        [XmlElement("Speciality")]
        public string Specialty
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("speciality")));
                return !string.IsNullOrEmpty(f.TagData) ? f.TagData : "None";
            }

            set
            {
                SetField(_fieldList, value, "Specialty");
            }
        }

        /// <summary>
        /// Fax
        /// </summary>
        [JsonProperty("Fax")]
        [XmlElement("Fax")]
        public string Fax
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("fax")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "Fax");
        }

        /// <summary>
        /// PagerInfo
        /// </summary>
        [JsonProperty("PagerInfo")]
        [XmlElement("PagerInfo")]
        public string PagerInfo
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("pageringfo")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "PagerInfo");
        }

        /// <summary>
        /// Email
        /// </summary>
        [JsonProperty("Email")]
        [XmlElement("Email")]
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                f.TagData += "\nEmail: " + value;
            }
        }

        /// <summary>
        /// IM
        /// </summary>
        [JsonProperty("IM")]
        [XmlElement("IM")]
        public string IM
        {
            set
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("comments")));
                f.TagData += "\nIM: " + value;
            }
        }
    }
}