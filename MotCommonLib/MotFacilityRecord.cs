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
    /// Location/Facility Record (Key == B)
    /// </summary>
    [XmlRoot("MotFacilityRecord")]
    public class MotFacilityRecord : MotRecordBase
    {
        private readonly List<Field> _fieldList;

        private void createRecord(string tableAction)
        {
            try
            {
                _fieldList.Add(new Field("Table", "Location", 10, true, 'a'));
                _fieldList.Add(new Field("Action", tableAction, 10, true, 'a'));
                _fieldList.Add(new Field("RxSys_LocID", "", 11, true, 'k'));
                _fieldList.Add(new Field("RxSys_StoreID", "", 11, false, 'n'));
                _fieldList.Add(new Field("LocationName", "", 60, true, 'a'));
                _fieldList.Add(new Field("Address1", "", 40, true, 'w'));
                _fieldList.Add(new Field("Address2", "", 40, false, 'w'));
                _fieldList.Add(new Field("City", "", 30, true, 'w'));
                _fieldList.Add(new Field("State", "", 10, true, 'w'));
                _fieldList.Add(new Field("Zip", "", 9, true, 'w'));
                _fieldList.Add(new Field("Phone", "", 10, true, 'w'));
                _fieldList.Add(new Field("Comments", "", 32767, false, 'n'));
                _fieldList.Add(new Field("CycleDays", "", 2, false, 'n'));
                _fieldList.Add(new Field("CycleType", "", 2, false, 'n'));
            }
            catch
            {
                throw;
            }
        }
        /// <summary>
        /// MotFacilityRecord
        /// </summary>
        public MotFacilityRecord()
        {
        }

        /// <summary>
        /// MotFacilitty Record Constructor
        /// </summary>
        /// <param name="action"></param>
        /// <param name="autoTruncate"></param>
        /// <exception cref="Exception"></exception>
        public MotFacilityRecord(string action, bool autoTruncate = false)
        {
            AutoTruncate = autoTruncate;

            try
            {
                _fieldList = new List<Field>();
                createRecord(action);
            }
            catch (Exception ex)
            {
                var errorString = $"Failed to create Location record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }

        public void SetField(string fieldName, string val, bool overrideTruncation = false)
        {
            try
            {
                base.SetField(_fieldList, val, fieldName, overrideTruncation);
            }
            catch
            {
                throw;
            }
        }
        public void AddToQueue(MotWriteQueue newQueue = null)
        {
            if (newQueue != null)
            {
                LocalWriteQueue = newQueue;
            }

            AddToQueue("B", _fieldList);
        }

        public void Write(MotSocket socket, bool doLogging = false)
        {
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
                var errorString = $"Failed to write Location record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
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
            catch (Exception ex)
            {
                var errorString = $"Failed to write Location record: {ex.Message}";
                EventLogger.Error(errorString);
                throw new Exception(errorString);
            }
        }
        public void Clear()
        {
            Clear(_fieldList);
        }

        /// <summary>
        /// Gets or sets the location identifier.
        /// </summary>
        /// <value>The location identifier.</value>
        [JsonProperty("RxSys_LocID")]
        [XmlElement("RxSys_LocID")]
        public string RxSys_LocID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_locid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_LocID");
        }

        /// <summary>
        /// LocationID
        /// </summary>
        [JsonProperty("LocationID")]
        [XmlElement("LocationID")]
        public string LocationID
        {
            get { return RxSys_LocID; }
            set => RxSys_LocID = value;
        }

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        /// <value>The store identifier.</value>
        [JsonProperty("RxSys_StoreID")]
        [XmlElement("RxSys_StoreID")]
        public string RxSys_StoreID
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("rxsys_storeid")));
                return f?.TagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_StoreID");
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
        /// Gets or sets the name of the location.
        /// </summary>
        /// <value>The name of the location.</value>
        [JsonProperty("LocationName")]
        [XmlElement("LocationName")]
        public string LocationName
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("locationname")));
                return f?.TagData;
            }

            set
            {
                if (string.IsNullOrEmpty(value))
                {
                    var tmp = AutoTruncate;
                    AutoTruncate = false;

                    SetField(_fieldList, "Pharmacist Attention - Missing Location Name", "LocationName");

                    AutoTruncate = tmp;
                    return;
                }

                SetField(_fieldList, value, "LocationName");
            }
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
                    SetField(_fieldList, value?.ToUpper(), "State");
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
                        value = value.Remove(value.IndexOf("-"), 1);
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

            set => SetField(_fieldList, NormalizeString(value ?? "999-999-9999"), "Phone");
        }

        /// <summary>
        /// Gets or sets the comments.
        /// </summary>
        /// <value>The comments.</value>
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
        /// Gets or sets the cycle days.
        /// </summary>
        /// <value>The cycle days.</value>
        [JsonProperty("CycleDays", ItemConverterType = typeof(int))]
        [XmlElement("CycleDays", typeof(int))]
        public int CycleDays
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("cycledays")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                if (value > 35 || value < 0)
                {
                    throw new Exception("CycleDays must be (0-35)");
                }

                SetField(_fieldList, Convert.ToString(value), "CycleDays");
            }
        }

        /// <summary>
        /// Gets or sets the type of the cycle.
        /// </summary>
        /// <value>The type of the cycle.</value>
        [JsonProperty("CycleType", ItemConverterType = typeof(int))]
        [XmlElement("CycleType", typeof(int))]
        public int CycleType
        {
            get
            {
                var f = _fieldList?.Find(x => x.TagName.ToLower().Contains(("cycletype")));
                return !string.IsNullOrEmpty(f.TagData) ? Convert.ToInt32(f.TagData) : 0;
            }

            set
            {
                // Actual error - it would be wrong to convert it to a default value
                if (value != 0 && value != 1)
                {
                    throw new Exception("CycleType must be '0 - Monthly' or '1 - Weekly'");
                }

                SetField(_fieldList, Convert.ToString(value), "CycleType");
            }
        }
    }
}