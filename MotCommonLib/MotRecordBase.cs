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
using System.Runtime.InteropServices;
using System.Globalization;
using System.Net.Sockets;
using System.Text;
using NLog;

namespace MotCommonLib
{
    /*
     * MotRecord Usage Example:
     * 
        using (var LocalTcpClient = new TcpClient(TargetIP, TargetPort))
        {
            using (var Stream = LocalTcpClient.GetStream())
            {
                var obj = new motStoreRecord("Add", boolAutoTruncate)
                {
                    ID = "1001",
                    Name = "Phred's Pharmacy",
                    Address1 = "123 Med Street",
                    City = "MedVille",
                    State = "AK",
                    Zip = "908176",
                    DEANum = "AF1234567"
                };

                obj.Write(Streamh);
            }
        }
    */

    /// <summary>
    /// <c>Field</c>
    /// Enhanced property defining data characteristics for Gateway suported data
    /// </summary>
    [Serializable]
    public class Field
    {
        public string TagName { get; set; }
        public string TagData { get; set; }
        public int MaxLen { get; set; }
        public bool Required { get; set; }
        public char When { get; set; }
        public bool AutoTruncate { get; set; }
        public bool IsNew { get; set; }
        public virtual void rules() { }

        /// <inheritdoc />
        public Field()
        { }

        /// <inheritdoc />
        public Field(string f, string t, int m, bool r, char w)
        {
            TagName = f;
            TagData = t;
            MaxLen = m;
            Required = r;
            When = w;
            AutoTruncate = IsNew = false;
        }

        /// <inheritdoc />
        public Field(string f, string t, int m, bool r, char w, bool a, bool n)
        {
            TagName = f;
            TagData = t;
            MaxLen = m;
            Required = r;
            When = w;
            AutoTruncate = true;
            IsNew = true;
        }

        /// <inheritdoc />
        public Field(string f, string t, int m, bool r, char w, bool a)
        {
            TagName = f;
            TagData = t;
            MaxLen = m;
            Required = r;
            When = w;
            AutoTruncate = a;
        }
    }
    /// <summary>
    /// <c>motRecordBase</c>
    /// Provides common I/O, Parsing, and Validation metods for derived classes
    /// </summary>
    [Serializable]
    public class MotRecordBase : IDisposable
    {
        /// <summary>
        /// DSN for the target database
        /// </summary>
        protected string Dsn;

        /// <summary>
        /// CRUD for records, Add, Create, Detele
        /// </summary>
        public string TableAction;

        /// <summary>
        /// Logger
        /// </summary>
        public Logger EventLogger;

        /// <summary>
        /// DefaultSocket
        /// </summary>
        public MotSocket DefaultSocket { get; set; }

        /// <summary>
        /// Gets or sets the gateway ip.
        /// </summary>
        /// <value>The gateway ip.</value>
        public string GatewayIP { get; set; }

        /// <summary>
        /// Gets or sets the gateway port.
        /// </summary>
        /// <value>The gateway port.</value>
        public string GatewayPort { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> is empty.
        /// </summary>
        /// <value><c>true</c> if is empty; otherwise, <c>false</c>.</value>
        public bool IsEmpty { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> log records.
        /// </summary>
        /// <value><c>true</c> if log records; otherwise, <c>false</c>.</value>
        public bool LogRecords { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> auto truncate.
        /// </summary>
        /// <value><c>true</c> if auto truncate; otherwise, <c>false</c>.</value>
        public bool AutoTruncate { get; set; } = false;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> use strong validation.
        /// </summary>
        /// <value><c>true</c> if use strong validation; otherwise, <c>false</c>.</value>
        public bool UseStrongValidation { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> send EOF.
        /// </summary>
        /// <value><c>true</c> if send EOF; otherwise, <c>false</c>.</value>
        public bool SendEof { get; set; } = false;

        // External Ordered Queue
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="T:MotCommonLib.MotRecordBase"/> queue writes.
        /// </summary>
        /// <value><c>true</c> if queue writes; otherwise, <c>false</c>.</value>
        public bool QueueWrites { get; set; } = false;

        /// <summary>
        /// Gets or sets the local write queue.
        /// </summary>
        /// <value>The local write queue.</value>
        public MotWriteQueue LocalWriteQueue { get; set; } = null;
        public bool DontSend { get;  set; }

        /// <summary>
        /// <c>AddToQueue</c>
        /// Stuffs new records into the write queue along with their asociated types and formats them
        /// </summary>
        /// <param name="type"></param>
        /// <param name="fieldList"></param>
        public void AddToQueue(string type, List<Field> fieldList)
        {
            if (DontSend)
            {
                return;
            }

            var dataLen = 0;

            if (IsEmpty)
            {
                return;
            }

            if (LocalWriteQueue == null)
            {
                EventLogger.Error("Null Queue");
                throw new ArgumentNullException($"Invalid Queue");
            }

            if (fieldList == null)
            {
                EventLogger.Error("Null parameters to base.Write()");
                throw new ArgumentNullException($"Bad Tag List");
            }

            var record = "<Record>";

            try
            {
                CheckDependencies(fieldList);

                foreach (var tag in fieldList)
                {
                    // Qualify field requirement
                    // if required and when == action && is_blank -> throw

                    record += "<" + tag.TagName + ">" +
                                    tag.TagData + "</" +
                                    tag.TagName + ">";

                    // If there's no data other than Table and Action we should ignore it.
                    if (tag.TagName.ToUpper() != "TABLE" && tag.TagName.ToUpper() != "ACTION")
                    {
                        dataLen += tag.TagData.Length;
                    }
                }

                record += "</Record>";

                if (dataLen > 0)
                {
                    LocalWriteQueue.Add(type, record);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error("Add To Queue: {0)\n{1}", ex.Message, record);
                throw;
            }
        }

        /// <summary>
        /// <c>Write Queue</c>
        /// Pushes everything in the queue to the passed stream
        /// </summary>
        /// <param name="stream"></param>
        public void WriteQueue(NetworkStream stream)
        {
            try
            {
                if (LocalWriteQueue == null)
                {
                    throw new ArgumentNullException($"Invalid Queue");
                }

                LocalWriteQueue.Write(stream);
                LocalWriteQueue.Clear();
            }
            catch
            {
                throw;
            }
        }

        /// <summary>
        /// <c>WriteQueue</c>
        /// Pushes everything in the queue to the passed socket
        /// </summary>
        /// <param name="socket"></param>
        public void WriteQueue(MotSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            try
            {
                if (LocalWriteQueue == null)
                {
                    throw new ArgumentNullException($"Invalid Queue");
                }

                LocalWriteQueue.Write(socket);
                LocalWriteQueue.Clear();
            }
            catch (Exception ex)
            {
                EventLogger.Error($"WriteQueue: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// <c>Commit</c>
        /// Wrapper for WriteQueue that takes a NetworkStream parameter
        /// </summary>
        /// <param name="stream"></param>
        public void Commit(NetworkStream stream)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            WriteQueue(stream);
        }

        /// <summary>
        /// <c>Commit</c>
        /// Wrapper for WriteQueue that takes a motSocket parameter
        /// </summary>
        /// <param name="socket"></param>
        public void Commit(MotSocket socket)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            WriteQueue(socket);
        }

        private string[] datePatterns =  // Hope I got them all
        {
            "yyyyMMdd",
            "yyyyMMd",
            "yyyyMdd",
            "yyyyMd",

            "yyyyddMM",
            "yyyyddM",
            "yyyydMM",
            "yyyydM",

            "ddMMyyyy",
            "ddMyyyy",
            "dMMyyyy",
            "dMyyyy",

            "MMddyyyy",
            "MMdyyyy",
            "Mddyyyy",
            "Mdyyyy",

            "dd/MM/yyyy",
            "dd/M/yyyy",
            "d/MM/yyyy",
            "d/M/yyyy",

            "MM/dd/yyyy",
            "MM/dd/yyyy hh:mm:ss tt",
            "MM/dd/yyyy h:mm:ss tt",
            "MM/dd/yyyy hh:m:ss tt",
            "MM/dd/yyyy h:m:ss tt",
            "MM/dd/yyyyhhmmss",            // HL7 Full Date Format 20110802085759
            "yyyyMMddhhmmss",

            "MM/d/yyyy",
            "MM/d/yyyy hh:mm:ss tt",

            "M/dd/yyyy",
            "M/dd/yyyy hh:mm:ss tt",

            "M/d/yyyy",
            "M/d/yyyy hh:mm:ss tt",

            "yyyy-MM-dd",
            "yyyy-M-dd",
            "yyyy-MM-d",
            "yyyy-M-d",

            "yyyy-dd-MM",
            "yyyy-d-MM",
            "yyyy-dd-M",
            "yyyy-d-M"
        };
        /// <summary>
        /// <c>NormalizeDate</c>
        /// Convers a bunch of different date formats into the one motLegacy understands
        /// </summary>
        /// <param name="origDate"></param>
        /// <returns></returns>
        protected string NormalizeDate(string origDate)
        {
            if (string.IsNullOrEmpty(origDate))
            {
                return string.Empty;
            }

            // Extract the date pard
            var dateOnly = origDate.Split(' ');

            if (DateTime.TryParseExact(dateOnly[0], datePatterns, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dt))
            {
                return dt.ToString("yyyy-MM-dd"); // return MOT Legacy Gateway Format
            }

            //DateFormatError = true;
            dateOnly[0] = "BADDATE";

            return dateOnly[0];
        }

        public DateTime TransformDate(string date)
        {
            DateTime dateOut;

            if (date == null)
            {
                return DateTime.Parse("1970-01-01");
            }

            try
            {
                DateTime.TryParseExact(date, datePatterns, CultureInfo.InvariantCulture, DateTimeStyles.None, out dateOut);                                                   
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }

            return dateOut;
        }
        
        /// <summary>
        /// <c>NormalizeString</c>
        /// Scrubs unneeded junk out of data 
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        protected string NormalizeString(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }

            char[] junk = { '-', '.', ',', ' ', ';', ':', '(', ')' };

            while (val?.IndexOfAny(junk) > -1)
            {
                val = val.Remove(val.IndexOfAny(junk), 1);
            }

            return val;
        }

        /// <summary>
        /// <c>ValidateDEA</c>
        /// Does a quick validation for DEA length and format
        /// </summary>
        /// <param name="deaId"></param>
        /// <returns></returns>
        protected string ValidateDea(string deaId)
        {
            if (string.IsNullOrEmpty(deaId))
            {
                return string.Empty;
            }

            // DEA Number Format is 2 letters, 6 numbers, & 1 check digit (CC-NNNNNNN) 
            // The first letter is a code identifying the type of registrant (see below)
            // The second letter is the first letter of the registrant's last name
            deaId = NormalizeString(deaId);

            if (deaId.Length == 13 && deaId.Substring(9, 4) == "0000")
            {
                return deaId.Substring(0, 9);
            }

            if (UseStrongValidation == true)
            {
                if (deaId.Length < 9)
                {
                    throw new FormatException("REJECTED: Invalid DEA Number, minimum length is 9. Received " + deaId.Length + " in " + deaId);
                }

                if (deaId.Length > 9)
                {
                    throw new FormatException("REJECTED: Invalid DEA Number, maximum length is 9. Received " + deaId.Length + " in " + deaId);
                }

                if (deaId[1] != '9' && !Char.IsLetter(deaId[1]))
                {
                    throw new FormatException("REJECTED: Invalid DEA Number, the id " + deaId.Substring(0, 2) + " in " + deaId + " is incorrect");
                }

                for (int i = 2; i < 7; i++)
                {
                    if (!Char.IsNumber(deaId[i]))
                    {
                        throw new FormatException("REJECTED: Invalid DEA Number, the trailing 6 characters must be digits, not " + deaId.Substring(2) + " in " + deaId);
                    }
                }
            }

            return deaId;
        }

        /// <summary>
        /// <c>CheckDependencies</c>
        /// Makes sure all required fields have appropriate content
        /// </summary>
        /// <param name="fieldSet"></param>
        private void CheckDependencies(List<Field> fieldSet)
        {
            //
            // Legacy MOT Gateway rules (Required Column)
            //
            //      'K' - Key Field, required for all actions
            //      'A' - Required for all Add actions
            //      'W' - Would like to have but not required
            //      'C' - Required for all Change actions
            //

            var f = fieldSet?.Find(x => x.TagName.ToLower().Contains("action"));

            //  There are rules for fields that are required in add/change/delete.  Test them here
            if (fieldSet != null)
            {
                foreach (var t in fieldSet)
                {
                    // required == true, when == 'k', _table action == '*', tagData == empty -> Exception
                    // required == true, when == 'a', _table action == 'change'  -> Pass
                    // required == true, when == 'a', _table action == 'add', tagData == live data -> Pass
                    // required == true, when == 'a', _table action == 'add', tagData == empty -> Exception
                    // required == true, when == 'c', _table_action == 'change', tagData == empty -> Exception

                    if (t.Required && (t.When == f.TagData.ToLower()[0] || t.When == 'k')) // look for a,c,k
                    {
                        if (string.IsNullOrEmpty(t.TagData))
                        {
                            if (t.TagName == "RxSys_LocID")
                            {
                                t.TagData = " ";
                                continue;
                            }

                            if (t.TagName == "LocationName")
                            {
                                t.TagData = "Home Care";
                                continue;
                            }

                            if (!AutoTruncate)
                            {
                                var errorString = $"REJECTED: Field {t.TagName} empty but required for the {f.TagData} operation on a {fieldSet[0].TagData} record!";

                                EventLogger.Error(errorString);
                                throw new Exception(errorString);
                            }
                            else
                            {
                                var errorString = $"Attention: Empty {t.TagName}";
                                EventLogger.Error(errorString);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <c>Clear</c>
        /// Clears out all the tag data in the set
        /// </summary>
        /// <param name="FieldSet"></param>
        protected void Clear(List<Field> FieldSet)
        {
            DontSend = false;

            string Type = FieldSet[0].TagData;
            string Action = FieldSet[1].TagData;

            foreach (var field in FieldSet)
            {
                field.TagData = string.Empty;
            }

            FieldSet[0].TagData = Type;
            FieldSet[1].TagData = Action;

            IsEmpty = true;
        }

        /// <summary>
        /// <c>SetField</c>
        /// Sets the value associated with the tag with consideration for truncation
        /// </summary>
        /// <param name="fieldSet"></param>
        /// <param name="val"></param>
        /// <param name="tag"></param>
        /// <param name="overrideTruncate"></param>
        /// <returns></returns>
        protected bool SetField(List<Field> fieldSet, string val, string tag, bool overrideTruncate = false)
        {
            var logData = string.Empty;

            if (fieldSet == null ||
                     val == null ||
                     tag == null)
            {
                EventLogger.Warn($"Null value in SetField: tag = {tag ?? "tag"}");
                return false;
                //throw new ArgumentNullException();
            }

            var f = fieldSet?.Find(x => x.TagName.ToLower().Contains((tag.ToLower())));

            if (f == null)
            {
                fieldSet.Add(new Field(tag, val, -1, false, 'n', false, true));
                return false;   // Field doesn't exist
            }

            if (!string.IsNullOrEmpty(val) && val.Length > f.MaxLen)
            {
                if (AutoTruncate && overrideTruncate)
                {
                    logData = $"Field Overflow at: <{tag}>, Data: {val}. Maxlen = {f.MaxLen} but got: {val.Length}";
                    EventLogger.Error(logData);
                    throw new Exception(logData);
                }

                logData = $"Autotruncated Overflowed Field at: <{tag}>, Data: {val}. Maxlen = {f.MaxLen} but got: {val.Length}";
                EventLogger.Warn(logData);

                val = val?.Substring(0, f.MaxLen);
            }

            f.TagData = string.IsNullOrEmpty(val) ? string.Empty : val;

            IsEmpty = false;

            return true;
        }

        /// <summary>
        /// <c>Write</c>
        /// Pushes the current set of fields to the passed stream after formatting
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="fieldSet"></param>
        /// <param name="doLogging"></param>
        protected void Write(NetworkStream stream, List<Field> fieldSet, bool doLogging = false)
        {
            if (DontSend)
            {
                return;
            }

            var record = "<Record>";
            var dataLen = 0;

            if (IsEmpty)
            {
                return;
            }

            if (stream == null || fieldSet == null)
            {
                EventLogger.Error("Null parameters to base.Write()");
                throw new ArgumentNullException($"Null Arguments");
            }

            try
            {
                CheckDependencies(fieldSet);

                foreach (var tag in fieldSet)
                {
                    if (tag.TagName.ToUpper().Contains("DATE") && tag.TagData.Contains("0001"))
                    {
                        tag.TagData = "";
                    }

                    record += "<" + tag.TagName + ">" +
                                    tag.TagData + "</" +
                                    tag.TagName + ">";

                    // If there's no data other than Table and Action we should ignore it.
                    if (tag.TagName.ToUpper() != "TABLE" && tag.TagName.ToUpper() != "ACTION")
                    {
                        dataLen += tag.TagData.Length;
                    }

                   
                }

                record += "</Record>";

                if (dataLen > 0)
                {
                    // Push it to the port
                    stream.Write(Encoding.UTF8.GetBytes(record), 0, record.Length);

                    if (SendEof)
                    {
                        stream.Write(Encoding.UTF8.GetBytes("<EOF/>"), 0, 7);
                    }
                }

                if (doLogging)
                {
                    EventLogger.Debug(record);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"{ex.Message}\n{record}");
                throw;
            }
        }

        /// <summary>
        /// <c>Write</c>
        /// Pushes the current set of fields to the passed socket after formatting
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="fieldSet"></param>
        /// <param name="doLogging"></param>
        protected void Write(MotSocket socket, List<Field> fieldSet, bool doLogging = false)
        {
            if (DontSend)
            {
                return;
            }

            var record = "<Record>";
            var dataLen = 0;

            if (IsEmpty)
            {
                return;
            }

            if (socket == null || fieldSet == null)
            {
                EventLogger.Error("Null parameters to base.Write()");
                throw new ArgumentNullException($"Null Arguments");
            }

            try
            {
                CheckDependencies(fieldSet);

                foreach (var tag in fieldSet)
                {
                    if (tag.TagName.ToUpper().Contains("DATE") && tag.TagData.Contains("0001"))
                    {
                        tag.TagData = "";
                    }

                    record += "<" + tag.TagName + ">" +
                              tag.TagData + "</" +
                              tag.TagName + ">";

                    // If there's no data other than Table and Action we should ignore it.
                    if (tag.TagName.ToUpper() != "TABLE" && tag.TagName.ToUpper() != "ACTION")
                    {
                        dataLen += tag.TagData.Length;
                    }

                    if (tag.TagName.ToUpper().Contains("DATE") && tag.TagData.Contains("0001"))
                    {
                        tag.TagData = "";
                    }
                }

                record += "</Record>";

                if (dataLen > 0)
                {
                    // Push it to the port
                    socket.Write(record);

                    if (SendEof)
                    {
                        socket.Write("<EOF/>");
                    }
                }

                if (doLogging)
                {
                    EventLogger.Debug(record);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error("{0)\n{1}", ex.Message, record);
                throw;
            }
        }

        /// <summary>
        /// <cr>Write</cr>
        /// Write string data directly to the stream
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="data"></param>
        public void Write(NetworkStream stream, string data)
        {
            if (stream == null)
            {
                throw new ArgumentNullException(nameof(stream));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (IsEmpty)
            {
                return;
            }

            try
            {
                stream.Write(Encoding.UTF8.GetBytes(data), 0, data.Length);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Failed to write {0} to port.  Error {1}", data, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// <cr>Write</cr>
        /// Write string data directly to the socket
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="data"></param>
        public void Write(MotSocket socket, string data)
        {
            if (socket == null)
            {
                throw new ArgumentNullException(nameof(socket));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (IsEmpty)
            {
                return;
            }

            try
            {
                socket.Write(data);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Failed to write {0} to port.  Error {1}", data, ex.Message);
                throw;
            }
        }

        /// <summary>
        /// ctor
        /// </summary>
        public MotRecordBase()
        {
            EventLogger = LogManager.GetLogger("MotCommonLib.Standard20.Records");
        }
        /// <summary>
        /// Destructor
        /// </summary>
        ~MotRecordBase()
        {
            Dispose(false);
        }

        /// <summary>
        /// Dispose
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                DefaultSocket?.Dispose();
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