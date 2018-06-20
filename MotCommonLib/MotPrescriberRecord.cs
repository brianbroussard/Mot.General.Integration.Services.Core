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
    /// Prescriber Record (Key == C)
    /// </summary>
    [XmlRoot("motPrescriberRecord")]
    public class MotPrescriberRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void createRecord(string tableAction)
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
                createRecord(action);
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
        /// ID
        /// </summary>
        [XmlElement("ID")]
        public string ID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_docid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }
        /// <summary>
        /// Obsolete RxSys_DocID
        /// </summary>
        [Obsolete("Use ID")]
        public string RxSys_DocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_docid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? "0", "RxSys_DocID");
        }
        /// <summary>
        /// LastName
        /// </summary>
        [XmlElement("LastName")]
        public string LastName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("lastname")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "LastName");
        }
        /// <summary>
        /// FirstName
        /// </summary>
        [XmlElement("FirstName")]
        public string FirstName
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("firstname")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "FirstName");
        }
        /// <summary>
        /// MiddleInitial
        /// </summary>
        [XmlElement("MiddleInitial")]
        public string MiddleInitial
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("middleinitial")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "MiddleInitial");
        }
        /// <summary>
        /// Address1
        /// </summary>
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
        /// <summary>
        /// Address2
        /// </summary>
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
        /// <summary>
        /// City
        /// </summary>
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
        /// <summary>
        /// State
        /// </summary>
        [XmlElement("State")]
        public string State
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("state")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value?.ToUpper() ?? String.Empty, "State");
        }
        /// <summary>
        /// PostalCode
        /// </summary>
        [XmlElement("PostalCode")]
        public string PostalCode
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
                    while ((bool)value?.Contains("-"))
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
        /// Zip
        /// </summary>
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
        /// <summary>
        /// Comments
        /// </summary>
        [XmlElement("Comments")]
        public string Comments
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "Comments");
        }
        /// <summary>
        /// DEA_ID
        /// </summary>
        [XmlElement("DEA_ID")]
        public string DEA_ID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("dea_id")));
                return f?.tagData;
            }

            set => SetField(_fieldList, ValidateDea(value ?? String.Empty), "DEA_ID");
        }
        /// <summary>
        /// TPID
        /// </summary>
        [XmlElement("TPID")]
        public string TPID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("tpid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "TPID");
        }
        /// <summary>
        /// Specialty
        /// </summary>
        [XmlElement("Speciality")]
        public int Specialty
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("speciality")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
            }

            set
            {
                SetField(_fieldList, Convert.ToString(value), "Specialty");
            }
        }

        /// <summary>
        /// Fax
        /// </summary>
        [XmlElement("Fax")]
        public string Fax
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("fax")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "Fax");
        }
        /// <summary>
        /// PagerInfo
        /// </summary>
        [XmlElement("PagerInfo")]
        public string PagerInfo
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("pageringfo")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? String.Empty, "PagerInfo");
        }
        /// <summary>
        /// 
        /// </sumEmailmary>
        public string Email
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += "\nEmail: " + value;
            }
        }
        /// <summary>
        /// IM
        /// </summary>
        public string IM
        {
            set
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                f.tagData += "\nIM: " + value;
            }
        }
    }
}