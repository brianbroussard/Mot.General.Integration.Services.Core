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
        [JsonProperty("LocationID")]
        [XmlElement("LocationID")]
        public string LocationID
        {
            get
            {
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_locid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_LocID");
        }

        /// <summary>
        /// Gets or sets the store identifier.
        /// </summary>
        /// <value>The store identifier.</value>
        [JsonProperty("StoreID")]
        [XmlElement("StoreID")]
        public string StoreID
        {
            get
            {

                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("rxsys_storeid")));
                return f?.tagData;
            }

            set => SetField(_fieldList, value ?? string.Empty, "RxSys_StoreID");
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("locationname")));
                return f?.tagData;
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

                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address1")));
                return f?.tagData;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("address2")));
                return f?.tagData;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("city")));
                return f?.tagData;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("phone")));
                return f?.tagData;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("comments")));
                return f?.tagData;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("cycledays")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
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
                var f = _fieldList?.Find(x => x.tagName.ToLower().Contains(("cycletype")));
                return !string.IsNullOrEmpty(f.tagData) ? Convert.ToInt32(f.tagData) : 0;
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