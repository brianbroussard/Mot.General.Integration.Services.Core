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
using System.Linq;
using System.Net.Sockets;
using NLog;
using MotCommonLib;
using MotListenerLib;

namespace MotHL7Lib
{
    public delegate void MotHl7OutputChangeEventHandler(object sender, HL7Event7MessageArgs e);
    public delegate void MotHl7ErrorEventHandler(object sender, HL7Event7MessageArgs e);

    public class HL7TransformerBase : IDisposable
    {
        public HL7Event7MessageArgs Hl7Event7MessageArgs { get; set; }
        public bool Service { get; set; }
        protected string Data { get; set; }
        protected bool Encrypt { get; set; }
        public bool AllowZeroTQ { get; set; }
        public string DefaultStoreLocation { get; set; }

        public string ResponseMessage { get; set; }

        #region CommonVariables
        public Logger EventLogger;
        public bool AutoTruncate { get; set; }
        public bool SendEof { get; set; }
        public bool debugMode { get; set; }
        public MotSocket Socket { get; set; }
        #endregion

        #region Listener
        public Hl7SocketListener dataIn;
        public bool startListener;
        protected string gatewayAddress { get; set; }
        protected int gatewayPort { get; set; }
        protected int returnPort { get; set; }
        #endregion

        public HL7TransformerBase(string data)
        {
            this.Data = data;
            EventLogger = LogManager.GetLogger("MotHL7MessageProcessor.Library");
            Hl7Event7MessageArgs = new HL7Event7MessageArgs();
        }

        ~HL7TransformerBase()
        {
            EventLogger.Info("Shutting down MotHl7MessageProcessor");
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                dataIn?.ShutDown();
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }

    public class MotHl7MessageProcessor : HL7TransformerBase
    {
        #region variables
        private HL7SendingApplication Hl7SendingApp { get; set; }
        private int FirstDoW { get; set; }

        private HL7SendingApplication RxHl7SendingApp { get; set; }

        private readonly Dictionary<HL7SendingApplication, string> _rxSystem = new Dictionary<HL7SendingApplication, string>()
        {
            {HL7SendingApplication.Enterprise, "Enterprise" },
            {HL7SendingApplication.Epic, "Epic" },
            {HL7SendingApplication.FrameworkLTC, "FrameworkLTC" },
            {HL7SendingApplication.Pharmaserve, "Pharmaserve" },
            {HL7SendingApplication.QS1,"QS1" },
            {HL7SendingApplication.RX30, "RX30" },
            {HL7SendingApplication.RNA, "RNA" },
            {HL7SendingApplication.Unknown, "Unknown" }
        };

        private double Rnatq1DoseQty { get; set; }
        private string[] GlobalMonth;

        private string ProblemLocus { get; set; }
        private string MessageType { get; set; }
        private string EventCode { get; set; }
        private string Organization { get; set; }
        private string Processor { get; set; }
        public string RxSysVendorName { get; set; }
        private HL7SendingApplication RxSysType { get; set; } = HL7SendingApplication.Unknown;

        private int CumulativeDoseCount { get; set; }
        private MSH _msh;
        private string[] _segments;
        private DataInputType dataInputType = DataInputType.Unknown;

        Dictionary<HL7SendingApplication, string> RxSystem = new Dictionary<HL7SendingApplication, string>()
        {
            {HL7SendingApplication.Enterprise, "Enterprise" },
            {HL7SendingApplication.Epic, "Epic" },
            {HL7SendingApplication.FrameworkLTC, "FrameworkLTC" },
            {HL7SendingApplication.Pharmaserve, "Pharmaserve" },
            {HL7SendingApplication.QS1,"QS1" },
            {HL7SendingApplication.RX30, "RX30" },
            {HL7SendingApplication.RNA, "RNA" },
            {HL7SendingApplication.Unknown, "Unknown" }
        };

        #endregion


        #region Messaging     
        public event MotHl7OutputChangeEventHandler OutputTextChanged;
        public event MotHl7ErrorEventHandler OutputErrorText;
        /// <summary>
        /// <c>OnOutputTextChanged</c>
        /// Provides update message to any subscriber
        /// </summary>
        /// <param name="e">HL7Event7MessageArgs class with event data</param>
        protected virtual void OnOutputTextChanged(HL7Event7MessageArgs e)
        {
            OutputTextChanged?.Invoke(this, e);
        }

        protected virtual void OnError(HL7Event7MessageArgs e)
        {
            OutputErrorText?.Invoke(this, e);
        }
        #endregion

        public MotHl7MessageProcessor(string data) : base(data)
        {
            dataInputType = DataInputType.Unknown;
        }

        public MotHl7MessageProcessor(string data, int port, bool service, bool encrypt) : base(data)
        {
            dataInputType = DataInputType.WebService;
        }

        public MotHl7MessageProcessor(string data, string dirName, bool service, bool encrypt) : base(data)
        {
            dataInputType = DataInputType.File;
        }

        /// <inheritdoc />
        public MotHl7MessageProcessor(bool startListener, string data, string gatewayAddress, int gatewayPort, bool service, bool encrypt) : base(data)
        {
            this.startListener = startListener;
            this.gatewayAddress = gatewayAddress;
            this.gatewayPort = gatewayPort;
            this.Service = service;
            this.Encrypt = encrypt;

            dataInputType = DataInputType.Socket;

            // data might be provided at Go()
            if (data != null)
            {
                if (!ValidateInput())
                {
                    // Not our data
                    throw new ArgumentException("Invalid Data Input - Not HL7");
                }
            }

            try
            {
                if (startListener)
                {
                    dataIn = new Hl7SocketListener(gatewayAddress, gatewayPort, Go, encrypt)
                    {
                        RunAsService = service
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                throw;
            }
        }

        /// <summary>
        /// <c>Segment Data</c>
        /// Break up the incominng message into its component segments
        /// </summary>
        /// <returns></returns>
        private string[] SegmentData()
        {
            // Clean delivery marks
            if (Data.Contains('\v'))
            {
                dataInputType = DataInputType.Socket;
                Data = Data.Remove(Data.IndexOf('\v'), 1);
                Data = Data.Remove(Data.IndexOf('\x1C'), 1);
            }
            else
            {
                dataInputType = DataInputType.File;
            }

            return Data.Split(new[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// <c>ValidateData</c>
        /// Make sure the incoming data is valid HL7
        /// </summary>
        /// <returns>true if the input is valid</returns>
        private bool ValidateInput()
        {
            if (!string.IsNullOrEmpty(Data))  // Data might be null depending on how it was initialized
            {
                if (!Data.Contains("\x0B") &&
                    !Data.Contains("\x1C"))
                {
                    if (!Data.Contains("MSH") || !Data.Contains(@"|^~\&")) // If its a file based input, it won't have the binary stuff
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// <c>ValidateMas</c>
        /// Make sure the MSH is valid and extract the sending system
        /// </summary>
        /// <returns></returns>
        private string ValidateMsh()
        {
            _msh = new MSH(_segments[0]);

            var sender = _msh.Get("MSH.3.1");

            if (!string.IsNullOrEmpty(sender))
            {
                Processor = _msh.Get("MSH.3.1");
                Organization = _msh.Get("MSH.4.1");
                return sender;
            }

            sender = _msh.Get("MSH.3");
            if (!string.IsNullOrEmpty(sender))
            {
                Processor = _msh.Get("MSH.3");
                Organization = _msh.Get("MSH.4");
                return sender;
            }

            throw new Exception("Malformed MSH-3 - Unknown Sender");
        }

        private void PrepForProcessing()
        {
            try
            {
                _segments = SegmentData();

                switch (ValidateMsh().ToLower())
                {
                    case "frameworkltc":
                        Hl7SendingApp = Hl7Event7MessageArgs.Hl7SendingApp = HL7SendingApplication.FrameworkLTC;
                        break;

                    case "epic":
                        Hl7SendingApp = Hl7Event7MessageArgs.Hl7SendingApp = HL7SendingApplication.Epic;
                        break;

                    case "rna":
                        Hl7SendingApp = Hl7Event7MessageArgs.Hl7SendingApp = HL7SendingApplication.RNA;
                        break;

                    default:
                        Hl7SendingApp = Hl7Event7MessageArgs.Hl7SendingApp = HL7SendingApplication.AutoDiscover;
                        break;
                }

                RxSysType = Hl7Event7MessageArgs.Hl7SendingApp;

                // RxSysType should default to AutoDiscover so we can simultaniously take input from disperate systems. In the 
                // case where systems don't self-identify the value will be specific and screw up if some other system
                // trys to send anything.  Catch it here and deal with it
                if (RxSysType != HL7SendingApplication.AutoDiscover && Hl7Event7MessageArgs.Hl7SendingApp != RxSysType)
                {
                    if (Hl7Event7MessageArgs.Hl7SendingApp == HL7SendingApplication.Unknown)
                    {
                        Hl7Event7MessageArgs.Hl7SendingApp = RxSysType;
                    }
                }
            }
            catch (Exception ex)
            {
                _msh = new MSH($@"MSH |^ ~\&|{Organization}|{Processor}|{ex.Message}|BAD MESSAGE|{DateTime.Now:yyyyMMddhhmm}|637300|UNKNOWN|2|T|276||||||UNICODE UTF-8|||||");

                ResponseMessage = new NAK(_msh, "AR", Organization, Processor).NakString;
                EventLogger.Error($"HL7 NAK: {ResponseMessage} Malormed Message: {Data}");

                // Update any subscribers and sending systems
                WriteResponse(ResponseMessage);

                // Update the caller
                throw new Exception(ResponseMessage);
            }
        }

        private void WriteResponse(string message, bool isError = false)
        {
            // Update subscribers
            Hl7Event7MessageArgs.Data = message;
            Hl7Event7MessageArgs.DataInputType = dataInputType;
            Hl7Event7MessageArgs.Timestamp = DateTime.Now;

            if (isError)
            {
                OnError(Hl7Event7MessageArgs);
            }
            else
            {
                OnOutputTextChanged(Hl7Event7MessageArgs);
            }

            switch (dataInputType)
            {
                case DataInputType.File:
                    break;

                case DataInputType.Socket:
                    // dataIn.WriteMessageToEndpoint(message, returnPort);
                    break;

                case DataInputType.WebService:
                    break;

                default:
                    break;
            }
        }

        private void GetEventCode()
        {
            EventCode = _msh.Get("MSH.9.2");
        }

        private void GetMessageType()
        {
            MessageType = _msh.Get("MSH.9.1");
            if (MessageType == null)
            {
                throw new ArgumentNullException($"No Message Type");
            }
        }

        System.Xml.Linq.XDocument GetStructuredData(string data)
        {
            if (data == null)
            {
                throw new ArgumentNullException($"Null Data in GetStructuredData");
            }

            var hl7Xml = new XmlToHL7Parser();
            return hl7Xml.Go(data);
        }

        public void Go(string inputStream = null)
        {
            if (inputStream != null)
            {
                Data = inputStream;
            }

            PrepForProcessing();

            try
            {
                GetMessageType();
                GetEventCode();

                // Figure out what kind of message it is              
                // switch (type93.Contains("MALFORMED") ? $"{type91}_{type92}" : type93)
                switch ($"{MessageType}_{EventCode}")
                {
                    case "RDE_O11":
                    case "RDE_011":
                        Hl7Event7MessageArgs.Data = Data;
                        Hl7Event7MessageArgs.Timestamp = DateTime.Now;
                        ProcessRDEMessage(this, Hl7Event7MessageArgs);
                        break;

                    case "OMP_O09":
                    case "OMP_009":
                    case "OMP_OO9":
                    case "OMP_0O9":
                        Hl7Event7MessageArgs.Data = Data;
                        Hl7Event7MessageArgs.Timestamp = DateTime.Now;
                        ProcessOMPMessage(this, Hl7Event7MessageArgs);
                        break;

                    case "RDS_O13":
                    case "RDS_013":
                        Hl7Event7MessageArgs.Data = Data;
                        Hl7Event7MessageArgs.Timestamp = DateTime.Now;
                        ProcessRDSMessage(this, Hl7Event7MessageArgs);
                        break;

                    case "ADT_A01":
                    case "ADT_AO1":
                    case "ADT_A03":
                    case "ADT_AO3":
                    case "ADT_A06":
                    case "ADT_AO6":
                    case "ADT_A08":
                    case "ADT_AO8":
                    case "ADT_A12":
                        Hl7Event7MessageArgs.Timestamp = DateTime.Now;
                        Hl7Event7MessageArgs.Data = Data;
                        ProcessADTEvent(this, Hl7Event7MessageArgs);
                        break;

                    default:
                        EventLogger.Error("Missing or Unhandled Message Type {0}", _msh.Get("MSH.9.3"));
                        throw new HL7Exception(201, _msh.Get("MSH.9.3") + " - Missing or Unhandled Message Type");
                }

                ACK ackMessageOut = new ACK(_msh, Organization, Processor);
                ResponseMessage = ackMessageOut.AckString;
                WriteResponse(ResponseMessage);

                if (debugMode)
                {
                    EventLogger.Debug("MessageIn:\n{0}", Data);
                    EventLogger.Debug("HL7 ACK:\n{0}", ResponseMessage);
                }
            }
            catch (HL7Exception ex)
            {
                var errorCode = "AP";

                // Parse the message, look for REJECTED
                if (ex.Message.Contains("REJECTED"))
                {
                    errorCode = "AR";
                }

                var nakMessageOut = new NAK(_msh, errorCode, Organization, Processor, ex.Message);
                ResponseMessage = nakMessageOut.NakString;
                WriteResponse(ResponseMessage, true);

                EventLogger.Error("HL7 NAK: {0}", ResponseMessage);
                EventLogger.Error("Failed Message: {0} Failed Reason: {1}", Data, ex.Message);
            }
            catch (Exception ex)
            {
                var nakOut = new NAK(_msh, "AP", Organization, Processor, "Unknown Processing Error " + ex.Message);
                ResponseMessage = nakOut.NakString;
            }
        }

        //---------------------------------------------------------------
        private string ParsePatternedDoseSchedule(string pattern, int thisRxType, TQ1 tq1, MotPrescriptionRecord scrip, DateTime StartDate, DateTime StopDate)
        {
            //  Frameworks repeat patterns are:
            //
            //  D (Daily)           QJ# where 1 == Monday - QJ123 = MWF, in MOT QJ123 = STT
            //  E (Every x Days)    Q#D e.g. Q2D is every 2nd s
            //  M (Monthly)         QL#,#,... e.g. QL3 QL1,15 QL1,5,10,20

            //DateTime.TryParseExact(sStartDate, "yyyyMMddhhmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime StartDate);
            //DateTime.TryParseExact(sEndDate, "yyyyMMddhhmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime StopDate);


            var startDoW = (int)StartDate.DayOfWeek;
            var tq14List = tq1.GetList("TQ1.4");
            var tq121 = tq1.Get("TQ1.2.1");

            CumulativeDoseCount += Convert.ToInt32(tq121);  // Increment the Dose Count

            scrip.DoseScheduleName = pattern;

            // Brute force the determination
            if (pattern[1] == 'J')   // Ex: QJ135 DoW field, Sunday = 1 "XX0XX0"
            {
                string newPattern = string.Empty;
                char[] dayBytes = { 'O', 'O', 'O', 'O', 'O', 'O', 'O' };
                var i1 = 0;

                // Each digit that follows J is an adjusted offset into the array
                var numberPattern = pattern.Substring(pattern.IndexOf("J", StringComparison.Ordinal) + 1);

                var dayArray = new[,]
                { // Dose Day
                  // S M T W T F S
                    {1,2,3,4,5,6,7 }, // S - First Day of Week
                    {7,1,2,3,4,5,6 }, // M
                    {6,5,4,3,2,1,7 }, // T 
                    {5,4,3,2,1,7,6 }, // W
                    {4,3,2,1,7,6,5 }, // T
                    {3,2,1,7,6,5,4 }, // F
                    {2,1,0,6,5,4,3 }  // S
                };

                if (Hl7SendingApp == HL7SendingApplication.FrameworkLTC)
                {
                    FirstDoW = 1;
                }

                foreach (var n in numberPattern)
                {
                    i1 = Convert.ToInt16(n.ToString());
                    var j = 0;

                    while (j < 7)
                    {
                        if (dayArray[FirstDoW, j] == i1)
                        {
                            dayBytes[dayArray[0, j] - 1] = 'X';
                            break;
                        }

                        j++;
                    }
                }

                var i2 = 0;
                while (i2 < 7)
                {
                    newPattern += dayBytes[i2++];
                }

                scrip.DoW = newPattern;
                scrip.RxType = 5;

                foreach (var tq14 in tq14List)
                {
                    scrip.DoseTimesQtys += $"{tq14}{Convert.ToDouble(tq121):00.00}";
                }
            }
            else if (pattern[0] == 'Q' && pattern.Contains("D"))  // Daily, need to qualify with the Q
            {
                string[] monthBuf = new string[35];  // 7x5 Card, HHMM + Total DoseQty/Day or HHMM00.00 if there is no dose
                                                     // The HHMM is ignored by the system, and is there to mintain format continuity

                var skipDays = Convert.ToInt32(pattern.Substring(1, 1));
                var i = 0;

                while (i < monthBuf.Length) // Intialize the entire array
                {
                    monthBuf[i++] = "HHMM00.00";
                }

                for (i = 0; i < monthBuf.Length; i += skipDays)
                {
                    monthBuf[i] = $"HHMM{Convert.ToDouble(CumulativeDoseCount):00.00}";  // SpecialDoses:  Monthly Pattern (00.00/Day for RxType 18 [Alternating])
                                                                                         //                The number represents total doses per day
                }


                i = 1;
                while (i < monthBuf.Length)
                {
                    monthBuf[0] += monthBuf[i++];
                }

                scrip.RxType = 18;
                scrip.MDOMStart = pattern.Substring(1, 1);  // Extract the number 
                scrip.DoseScheduleName = pattern;           // => This is the key part.  The DoseScheduleName has to exist on MOTALL 
                scrip.SpecialDoses = monthBuf[0];

                foreach (var tq14 in tq14List)
                {
                    scrip.DoseTimesQtys += $"{tq14}{Convert.ToDouble(tq121):00.00}";
                }
            }
            else if (pattern[1] == 'L') // Monthly TCustom (Unsupported by MOTALL) -- QLn,n,n,...
            {
                char[] toks = { ',' };
                var i = 0;

                pattern = pattern.Substring(2);
                var days = pattern.Split(toks);

                scrip.DoseScheduleName = pattern;            // => This is a key part.  The DosScheduleName has to be unique in MOTALL 
                foreach (var d in days)
                {
                    scrip.DoseScheduleName += d;
                }

                if (GlobalMonth == null)
                {
                    GlobalMonth = new string[35];

                    while (i < GlobalMonth.Length)
                    {
                        GlobalMonth[i++] = "00.00";
                    }
                }

                foreach (var d in days)
                {
                    GlobalMonth[Convert.ToInt16(d) - 1] = $"HHMM{Convert.ToDouble(CumulativeDoseCount):00.00}";
                }

                scrip.RxType = 20;  // Not Supported in Legacy -- Reported by PCHS 20160822, they "suggest" trying 18

                i = 1;
                while (i < GlobalMonth.Length)
                {
                    GlobalMonth[0] += GlobalMonth[i++];
                }

                scrip.SpecialDoses = GlobalMonth[0];  //  HHMM00.00 * 35  replaces -->"01011011011 ...  Up to 35

                foreach (var tq14 in tq14List)
                {
                    scrip.DoseTimesQtys += $"{tq14}{Convert.ToDouble(tq121):00.00}"; // 080001.00120002.00180001.00200002.00
                }

                EventLogger.Warn("Logging QLX record with new values: Special Doses {0}, DoseTimeQtys {1}", scrip.SpecialDoses, scrip.DoseTimesQtys);
            }

            // Get the Time/Qtys
            return $"{tq14List.FirstOrDefault()}{Convert.ToDouble(tq121):00.00}";
        }

        /// <summary>
        /// IsHL7DoseSchedule()
        /// HL7 v2.7 defines a collection of stock Dose Schedules that are in general use.  Several were set up for Frameworks already.  Many more are defined by Hl7.
        /// </summary>
        /// <param name="schedulePattern"></param>
        /// <returns></returns>
        private static bool IsHL7DoseSchedule(string schedulePattern)
        {
            var hasNum = schedulePattern.Any(char.IsDigit);

            if (!hasNum)
            {
                return false;
            }

            if (schedulePattern[0] == 'Q' && schedulePattern[2] == 'D')         // QnD - Every n Days
                return true;

            if (schedulePattern[0] == 'Q' && schedulePattern[1] == 'J')         // QJn - Frameworks - QJ135 DoW field, Sunday = 1, pattern = "XX0XX0"
                return true;

            /*
            if (__sched[0] == 'Q' && char.IsDigit(__sched[1]) && __sched[2] == 'J' && char.IsDigit(__sched[3]))         // QnJn  - Hl7Specific DOW
                return true;
            */

            if (schedulePattern[0] == 'Q' && schedulePattern[1] == 'L')         // QnL - - Every n Months
                return true;

            /*
            if(__sched[0] == 'Q' && __sched.Count(c => char.IsDigit(c)) > 1)  // Qn... Repeats on Days of week
                return true;

            if (__sched[0] == 'Q' && char.IsDigit(__sched[1]) && __sched[2] == 'M')         // QnM - Hl7 Every n Minutes
                return true;

            if (__sched[0] == 'Q' && char.IsDigit(__sched[1]) && __sched[2] == 'S')         // QnM - Hl7 Every n Seconds
                return true;

            if (__sched[0] == 'Q' && char.IsDigit(__sched[1]) && __sched[2] == 'W')         // QnM - Hl7 Every n Weeks
                return true;
            */
            return false;
        }

        public string ProcessAL1(RecordBundle recBundle, AL1 al1)
        {
            if (recBundle == null || al1 == null)
            {
                return string.Empty;
            }

            ProblemLocus = "AL1";

            return
                $"Type Code: {al1.Get("AL1.2.1")}\n" +
                $"Mnemonic: {al1.Get("AL1.3.1")}\n" +
                $"Desc: {al1.Get("AL1.3.2")}\n" +
                $"Severity: {al1.Get("AL1.4.1")}\n" +
                $"Reaction: {al1.Get("AL1.5")}\n" +
                $"ID Date: {al1.Get("AL1.6")}\n" +
                $"**\n";
        }

        private string ProcessDG1(RecordBundle recBundle, DG1 dg1)
        {
            if (recBundle == null || dg1 == null)
            {
                return string.Empty;
            }

            ProblemLocus = "DG1";

            return string.Format("Coding Method: {0}\nDiagnosis Code: {1}\nDescription: {2}\nDate/Time: {3}\nDiagnosis Type: {4}\nMajor Catagory: {5}\nDiagnostic Related Group: {6}\n******\n",
                                 dg1.Get("DG1.2"),   // 0 - Diagnosis Coding Method
                                 dg1.Get("DG1.3"),   // 1 - Diagnosis Code
                                 dg1.Get("DG1.4"),   // 2 - Diagnosis Description
                                 dg1.Get("DG1.5"),   // 3 - Diagnosis Date/Time
                                 dg1.Get("DG1.6"),   // 4 - Diagnosis Type
                                 dg1.Get("DG1.7"),   // 5 - Major Diagnostic Catagory
                                 dg1.Get("DG1.8"));  // 6 - Diagnostic Related Group
        }

        private string ProcessEVN(RecordBundle recBundle, EVN evn)
        {
            if (recBundle == null || evn == null)
            {
                return string.Empty;
            }

            ProblemLocus = "EVN";
            var currentDate = recBundle.Patient.TransformDate(evn.Get("EVN.2"));

            switch (evn.Get("EVN.1"))
            {
                case "A01": // Admit/Visit  
                    recBundle.Patient.AdmitDate = currentDate;
                    recBundle.Patient.Status = 1;
                    break;

                case "A03": // Discharge
                    recBundle.Patient.Comments = $"Discharge Date: {currentDate}";
                    recBundle.Patient.Status = 0;
                    break;

                case "A06": // Change Outpatient to Inpatient                  
                    break;

                case "A08": // Update patient Informtion
                    break;

                case "A12": // Cancel Transfer
                    break;

                default:
                    break;
            }

            return EventCode;
        }

        private void ProcessIN1(RecordBundle recBundle, IN1 in1)
        {
            if (recBundle == null || in1 == null)
            {
                return;
            }

            ProblemLocus = "IN1";

            //                              Ins/Group
            recBundle.Patient.InsPNo = in1.Get("IN1.2.1") + "/" + in1.Get("IN1.8");
            recBundle.Patient.InsName = in1.Get("IN1.4.1") + "/" + in1.Get("IN1.8"); ;

            // No Insurancec Policy Number per se, but there is a group name(1-9) and a group number(1-8) 
            //__recs.__pr.InsPNo = __in1.Get("IN1.1.9") + "-" + __in1.Get("IN1.1.8");
        }

        private void ProcessIN2(RecordBundle recBundle, IN2 in2)
        {
            if (recBundle == null || in2 == null)
            {
                return;
            }

            ProblemLocus = "IN2";

            if (string.IsNullOrEmpty(recBundle.Patient.SSN))
            {
                recBundle.Patient.SSN = in2.Get("IN2.1.1");
            }
        }

        private string ProcessNK1(RecordBundle recBundle, NK1 nk1)
        {
            if (recBundle == null || nk1 == null)
            {
                return string.Empty;
            }

            ProblemLocus = "NK1";

            return $"{nk1.Get("NK1.2.2")} {nk1.Get("NK1.2.3")} {nk1.Get("NK1.2.1")} [{nk1.Get("NK1.3.1")}]\n";
        }

        private string ProcessNTE(RecordBundle recBundle, NTE nte)
        {
            if (recBundle == null || nte == null)
            {
                return string.Empty;
            }
            ProblemLocus = "NTE";

            return nte.Get("NTE.3");
        }

        private void ProcessOBX(RecordBundle recBundle, OBX obx)
        {
            if (recBundle == null || obx == null)
            {
                return;
            }

            ProblemLocus = "OBX";

            if (obx.Get("OBX.3.2").ToLower().Contains("weight"))
            {
                if (obx.Get("OBX.6.1").ToLower().Contains("kg"))
                {
                    var doubleTmp = Convert.ToDouble(obx.Get("OBX.5"));
                    doubleTmp *= 2.2;
                    recBundle.Patient.Weight = Convert.ToInt32(doubleTmp);
                }
                else
                {
                    recBundle.Patient.Weight = Convert.ToInt32(obx.Get("OBX.3.2"));
                }
            }

            if (obx.Get("OBX.3.2").ToLower().Contains("height"))
            {

                if (obx.Get("OBX.6.1").ToLower().Contains("cm"))
                {
                    var doubleTmp = Convert.ToDouble(obx.Get("OBX.5"));
                    doubleTmp *= 2.54;
                    recBundle.Patient.Height = Convert.ToInt32(doubleTmp);
                }
                else
                {
                    recBundle.Patient.Height = Convert.ToInt32(obx.Get("OBX.3.2"));
                }
            }
        }

        private void ProcessORC(RecordBundle recBundle, ORC orc)
        {
            if (recBundle == null || orc == null)
            {
                return;
            }

            var newScrip = false;
            var refillScrip = false;
            var toDc = false;
            var changeOrder = false;

            ProblemLocus = "ORC";

            switch (orc.Get("ORC.1"))
            {
                case "NW": // New order/service
                    newScrip = true;
                    recBundle.Scrip.Status = 1;
                    break;

                case "DC":  // Discontinue order/service request
                    toDc = true;
                    recBundle.Scrip.DiscontinueDate = recBundle.Scrip.TransformDate(orc.Get("ORC.15.1"));
                    recBundle.Scrip.Status = 0;
                    break;

                case "RF":  // Refill order/service request                
                    refillScrip = true;
                    recBundle.Scrip.Status = 1;
                    //recBundle.MakeDupRnaScrip = true;
                    break;

                case "XO":  // Change order/service request
                case "CA":  // Change order/service request
                    changeOrder = true;
                    recBundle.Scrip.Status = 1;
                    recBundle.MakeDupRnaScrip = true;
                    recBundle.NewStartDate = recBundle.Scrip.TransformDate(orc.Get("ORC.15.1"));
                    break;

                case "RE":  // Observations/Performed Service to follow
                default:
                    break;
            }

            // For FrameworkLTC RDE messages, the ORC format is represented as FacilityID\F\PatientID\F\OrnderNum which parses to 
            //  <Facility ID> | <Patient ID> | <Order Number>
            //
            if (Hl7SendingApp == HL7SendingApplication.FrameworkLTC)
            {
                char[] delim = { '|' };
                var part = orc.Get("ORC.3").Split(delim);

                if (part.Length >= 3)
                {
                    recBundle.Location.LocationID = part[0];
                    recBundle.Scrip.PrescriptionID = part[2];
                }
            }
            else if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                recBundle.Location.LocationID = orc.Get("ORC.21.3");
                recBundle.Prescriber.TPID = orc.Get("ORC.12.10.1");

                /* From Michael Dix at RNA
                 * I also wanted to mention how we populated ORC-2 and ORC-3.  In our system, the prescription number on an order will change for 
                 * various reasons, one being we only store 6 fills per prescription.  The 7th time it is filled it will generate a new number.
                 *
                 * When that occurs:
                 *   ORC-2 will contain the current/new prescription number
                 *   ORC-3 will always have the original prescription number                
                 *   ORC-4 will contain the last prescription number or be empty
                 *
                 *   If ORC-4 is empty and ORC-2 == ORC-3, its an original orderl regardless if its flagged "XO"
                 */
                if (!newScrip && !refillScrip)
                {
                    // If ORC-4 has a value, this is a Renew event and the last RxNum is not the first one
                    if (string.IsNullOrEmpty(orc.Get("ORC.4.1")))
                    {
                        recBundle.Scrip.PrescriptionID = orc.Get("ORC.3.1");
                    }
                    else
                    {
                        recBundle.Scrip.PrescriptionID = orc.Get("ORC.4.1");
                    }

                    recBundle.Scrip.RxSys_NewRxNum = orc.Get("ORC.2.1");
                    recBundle.Scrip.DiscontinueDate = DateTime.Today;
                    recBundle.Scrip.Status = 100;
                }
                else
                {
                    recBundle.Scrip.PrescriptionID = orc.Get("ORC.3.1");
                }
            }
            else
            {
                recBundle.Location.LocationID = orc.Get("ORC.21.3");
                recBundle.Scrip.PrescriptionID = orc.Get("ORC.2.1");
            }

            recBundle.Scrip.RxStartDate = recBundle.Scrip.TransformDate(orc.Get("ORC.11.19") ?? "");
            recBundle.Scrip.RxStopDate = recBundle.Scrip.TransformDate(orc.Get("ORC.11.20") ?? "");

            recBundle.Location.LocationName = orc.Get("ORC.21.1");
            recBundle.Location.Address1 = orc.Get("ORC.22.1");
            recBundle.Location.Address2 = orc.Get("ORC.22.2");
            recBundle.Location.City = orc.Get("ORC.22.3");
            recBundle.Location.State = orc.Get("ORC.22.4");
            recBundle.Location.Zipcode = orc.Get("ORC.22.5");
            recBundle.Location.Phone = orc.Get("ORC.23.1");

            var prescriberID = orc.Get("ORC.12.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (prescriberID.Length > 9)
                {
                    prescriberID = prescriberID.Substring(0, 9);
                }
            }

            recBundle.Prescriber.PrescriberID = prescriberID;
            recBundle.Prescriber.LastName = orc.Get("ORC.12.2");
            recBundle.Prescriber.FirstName = orc.Get("ORC.12.3");
            recBundle.Prescriber.Address1 = orc.Get("ORC.24.1");
            recBundle.Prescriber.Address2 = orc.Get("ORC.24.2");
            recBundle.Prescriber.City = orc.Get("ORC.24.3");
            recBundle.Prescriber.State = orc.Get("ORC.24.4");
            recBundle.Prescriber.Zipcode = orc.Get("ORC.24.5");

            recBundle.Patient.PrimaryPrescriberID = prescriberID;
            recBundle.Scrip.PrescriberID = prescriberID;
        }

        private void ProcessPID(RecordBundle recBundle, PID pid)
        {
            if (recBundle == null || pid == null)
            {
                return;
            }

            ProblemLocus = "PID";

            // For FrameworkLTC RDE messages, the PID format is represented as FacilityID\F\PatientID which is converted to 
            // FacilityID | PatientID by the parser.  So the tag is still <PID.3> but we need to split it.         
            //
            // and for ADT Messages the PID format is a pure CX record and rpresented as PatientID^CheckDigit^Check Digit ID Code, so
            //  PID-3-1 is the ID.  
            //
            // Epic uses a CX for RDE messages, so, we might need to follow a rule chain to maintain system independence
            // Both PID-2-1 and PID-4-1 can also contain a patient ID but its unclear what the rules are there.
            //  
            // PID-2-1, PID-3-1, PID-4-1 have the Patient ID. Sample A01 records have a blank PID-2 and populated 
            // PID-3-1, though the RDE_O11 is the reverse.  Try them both and take what's there.  3-1 wins in a draw
            // 
            // For QS1 i looks like they send the PID over as the "Alternative" 4-1

            if (Hl7SendingApp == HL7SendingApplication.FrameworkLTC)
            {
                char[] delim = { '|', '\\' };

                string tmp = pid.Get("PID.3");
                if (!string.IsNullOrEmpty(tmp))
                {
                    var part = tmp.Split(delim);
                    if (part.Length >= 2)
                    {
                        recBundle.Patient.LocationID = part[0];
                        recBundle.Patient.PatientID = part[1];
                    }
                }
            }
            else
            {
                // Walk the potential types looking for the right one -- Generically 
                if (!string.IsNullOrEmpty(pid.Get("PID.2.1")))
                {
                    recBundle.Patient.PatientID = pid.Get("PID.2.1");
                }
                else if (!string.IsNullOrEmpty(pid.Get("PID.3.1")))
                {
                    recBundle.Patient.PatientID = pid.Get("PID.3.1");
                }
                else if (!string.IsNullOrEmpty(pid.Get("PID.4.1")))
                {
                    recBundle.Patient.PatientID = pid.Get("PID.4.1");
                }
                else
                {
                    recBundle.Patient.PatientID = "UnKnown";
                }
            }

            recBundle.Patient.LastName = pid.Get("PID.5.1");
            recBundle.Patient.FirstName = pid.Get("PID.5.2");
            recBundle.Patient.MiddleInitial = pid.Get("PID.5.3");

            var date = pid.Get("PID.7").Length >= 8 ? pid.Get("PID.7")?.Substring(0, 8) : pid.Get("PID.7");
            recBundle.Patient.DOB = recBundle.Patient.TransformDate(date);

            if (!string.IsNullOrEmpty(pid.Get("PID.8")))
            {
                recBundle.Patient.Gender = pid.Get("PID.8")?.Substring(0, 1);
            }

            recBundle.Patient.Address1 = pid.Get("PID.11.1"); // In a PID Segment this is always an XAD structure
            recBundle.Patient.Address2 = pid.Get("PID.11.2");
            recBundle.Patient.City = pid.Get("PID.11.3");
            recBundle.Patient.State = pid.Get("PID.11.4");
            recBundle.Patient.Zipcode = pid.Get("PID.11.5");
            recBundle.Patient.Phone1 = pid.Get("PID.13.1");
            recBundle.Patient.WorkPhone = pid.Get("PID.14.1");
            recBundle.Patient.SSN = pid.Get("PID.19");

            // Set the default patient status to active, it can be changed with an ADT^A03 or ADT^A08
            recBundle.Patient.Status = 1;


            //__scrip.RxSys_PatID = __pr.RxSys_PatID;
        }

        private void ProcessPV1(RecordBundle recBundle, PV1 pv1)
        {
            if (recBundle == null || pv1 == null)
            {
                return;
            }

            ProblemLocus = "PV1";

            var Temp = pv1.Get("PV1.7.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (Temp.Length > 9)
                {
                    Temp = Temp.Substring(0, 9);
                }
            }

            recBundle.Prescriber.PrescriberID = Temp;
            recBundle.Prescriber.LastName = pv1.Get("PV1.7.2");
            recBundle.Prescriber.FirstName = pv1.Get("PV1.7.3");
            recBundle.Prescriber.MiddleInitial = pv1.Get("PV1.7.4");

            // Look for the NPI here.  We set this up for RNA but there's no reason we couldn't use it generally
            recBundle.Prescriber.TPID = pv1.Get("PV1.7.9.2");

            // Check for the Patient DocID to match the Visit DocID, update as needed [Frameworks Message]
            if (string.IsNullOrEmpty(recBundle.Patient.PrimaryPrescriberID))
            {
                recBundle.Patient.PrimaryPrescriberID = Temp;
            }
        }

        private void ProcessPV2(RecordBundle recBundle, PV2 pv2)
        {
            if (recBundle == null || pv2 == null)
            {
                return;
            }

            ProblemLocus = "PV2";
        }

        private void ProcessPD1(RecordBundle recBundle, PD1 pd1)
        {
            if (recBundle == null || pd1 == null)
            {
                return;
            }

            ProblemLocus = "PD1";
        }

        private void ProcessPRT(RecordBundle recBundle, PRT prt)  // this is an EPIC or 2.7 record
        {
            if (recBundle == null || prt == null)
            {
                return;
            }

            var tempDoc = new MotPrescriberRecord("Add", AutoTruncate);
            var tempStore = new MotStoreRecord("Add", AutoTruncate);

            ProblemLocus = "PRT";


            // Participant Person
            var tempId = prt.Get("PRT.5.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (tempId.Length > 9)
                {
                    tempId = tempId.Substring(0, 9);
                }
            }

            tempDoc.PrescriberID = tempId;
            tempDoc.LastName = prt.Get("PRT.5.2");
            tempDoc.FirstName = prt.Get("PRT.5.3");
            tempDoc.MiddleInitial = prt.Get("PRT.5.4");
            tempDoc.Address1 = prt.Get("PRT.14.1");
            tempDoc.Address2 = prt.Get("PRT.14.2");
            tempDoc.City = prt.Get("PRT.14.3");
            tempDoc.State = prt.Get("PRT.14.4");
            tempDoc.Zipcode = prt.Get("PRT.14.5");
            tempDoc.DEA_ID = "XD0123456";


            // Participant Organization
            tempStore.StoreID = prt.Get("PRT.7.1");
            tempStore.StoreName = prt.Get("PRT.7.2");
            tempStore.Address1 = prt.Get("PRT.14.1");
            tempStore.Address2 = prt.Get("PRT.14.2");
            tempStore.City = prt.Get("PRT.14.3");
            tempStore.State = prt.Get("PRT.14.4");
            tempStore.Zipcode = prt.Get("PRT.14.5");
            tempStore.DEANum = "XS0123456";

            var phoneList = prt.GetList("PRT.15.1");

            // Get the phones  format is NNNNNNNNNNTT with TT being PH or FX
            for (var i = 0; i < 2; i++)
            {
                if (phoneList[i].Contains("PH"))
                {
                    tempStore.Phone = tempDoc.Phone = phoneList[i].Substring(0, phoneList[i].Length - 2);
                }
                else
                {
                    tempStore.Fax = tempDoc.Fax = phoneList[i].Substring(0, phoneList[i].Length - 2);
                }
            }

            if (!string.IsNullOrEmpty(tempDoc.PrescriberID))
            {
                recBundle.PrescriberList.Add(tempDoc);
            }

            if (!string.IsNullOrEmpty(tempStore.StoreID))
            {
                recBundle.StoreList.Add(tempStore);
            }
        }

        private void ProcessRXC(RecordBundle recBundle, RXC rxc)  // Process Compound Components
        {
            if (recBundle == null || rxc == null)
            {
                return;
            }

            ProblemLocus = "RXC";

            recBundle.Scrip.Comments += "Compound Order Segment\n__________________\n";
            recBundle.Scrip.Comments += "Component Type:  " + rxc.Get("RXC.1.1");
            recBundle.Scrip.Comments += "Component Amount " + rxc.Get("RXC.3.1");
            recBundle.Scrip.Comments += "Component Units  " + rxc.Get("RXC.4.1");
            recBundle.Scrip.Comments += "Component Strength" + rxc.Get("RXC.5");
            recBundle.Scrip.Comments += "Component Strngth Units  " + rxc.Get("RXC.6.1");
            recBundle.Scrip.Comments += "Component Drug Strength Volume " + rxc.Get("RXC.8.1");
            recBundle.Scrip.Comments += "Component Drug Strength Volume Units" + rxc.Get("RXC.9.1");
            recBundle.Scrip.Comments += "\n\n";
        }

        private void ProcessRXD(RecordBundle recBundle, RXD rxd)
        {
            if (recBundle == null || rxd == null)
            {
                return;
            }

            ProblemLocus = "RXD";

            recBundle.Scrip.DrugID = rxd.Get("RXD.2.1");
            recBundle.Scrip.QtyDispensed = Convert.ToDouble(rxd.Get("RXD.4") ?? "0.00");
            recBundle.Scrip.PrescriptionID = rxd.Get("RXD.7");

            recBundle.Drug.DrugID = rxd.Get("RXD.2.1");
            recBundle.Drug.DrugName = rxd.Get("RXD.2.2");
            recBundle.Drug.Strength = Convert.ToDouble(rxd.Get("RXD.16") ?? "0.00");

            // This  popped up in McKesson Pharmaserv.  RNA also sends over 4 extra 0's with each so we need to strip those off
            var tempDea = rxd.Get("RXD.10.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (tempDea.Length > 9)
                {
                    tempDea = tempDea.Substring(0, 9);
                }
            }

            recBundle.Prescriber.DEA_ID = tempDea;

            //__recs.__doc.DEA_ID = __rxd.Get("RXD.10.1");

            var tempLastName = rxd.Get("RXD.10.2");
            var tempFirstName = rxd.Get("RXD.10.3");

            // Check First and Last Names before grabbing [Frameworks doesn't populate RXD]
            if (!string.IsNullOrEmpty(tempLastName))
            {
                recBundle.Prescriber.LastName = tempLastName;
            }

            if (!string.IsNullOrEmpty(tempFirstName))
            {
                recBundle.Prescriber.FirstName = tempFirstName;
            }

            var tempDocId = rxd.Get("RXD.10.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (tempDocId.Length > 9)
                {
                    tempDocId = tempDocId.Substring(0, 9);
                }
            }

            // Test to make sure there's a valid DocID [Frameworks Doesn't Seem to send one in RXD]
            if (!string.IsNullOrEmpty(tempDocId))
            {
                recBundle.Scrip.PrescriberID = tempDocId;
            }

            recBundle.Scrip.DoseScheduleName = rxd.Get("RXD.15.1");

            // RNA only seds the Sig in TQ1
            if (Hl7SendingApp != HL7SendingApplication.RNA)
            {
                recBundle.Scrip.Sig = rxd.Get("RXD.15.2");
            }

            recBundle.Scrip.QtyPerDose = Convert.ToDouble(rxd.Get("RXD.12.1") ?? "0.00");

            if (string.IsNullOrEmpty(rxd.Get("RXD.9")))
            {
                recBundle.Scrip.Comments = "Patient Notes: ";
            }

            recBundle.Scrip.Comments += "\n" + rxd.Get("RXD.9");

            recBundle.Scrip.Refills = Convert.ToInt32(rxd.Get("RXD.8") ?? "0");
            recBundle.Scrip.RxType = 0;

            recBundle.Drug.NDCNum = rxd.Get("RXD.2.1");
            recBundle.Drug.Unit = rxd.Get("RXD.5.1");
            recBundle.Drug.DoseForm = rxd.Get("RXD.6.1");
            recBundle.Drug.TradeName = rxd.Get("RXD.2.2");

            // Apparently the RXD doesn't identify the patient -- Assume a pid always comes first
            recBundle.Scrip.PatientID = recBundle.Patient.PatientID;
        }

        int ConvertFromRoman(string number)
        {
            if (number.Contains("I") || number.Contains("V"))
            {
                switch (number)
                {
                    case "I":
                        return 1;
                    case "II":
                        return 2;
                    case "III":
                        return 3;
                    case "IV":
                        return 4;
                    case "V":
                        return 5;
                    case "VI":
                        return 6;
                    case "VII":
                        return 7;

                }
            }

            try
            {
                return Convert.ToInt32(number);
            }
            catch (Exception e)
            {
                return 0;
            }
        }
        private void ProcessRXE(RecordBundle recBundle, RXE rxe)
        {
            if (recBundle == null || rxe == null)
            {
                return;
            }

            ProblemLocus = "RXE";

            var tempDea = rxe.Get("RXE.13.1");

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (tempDea.Length > 9)
                {
                    tempDea = tempDea.Substring(0, 9);
                }
            }

            recBundle.Prescriber.DEA_ID = tempDea;

            // If there's no prescriber ID, use the DEA
            if (string.IsNullOrEmpty(recBundle.Prescriber.PrescriberID))
            {
                recBundle.Prescriber.PrescriberID = tempDea;
            }

            recBundle.Drug.DrugID = rxe.Get("RXE.2.1");
            recBundle.Drug.NDCNum = rxe.Get("RXE.2.1");
            recBundle.Drug.DrugName = rxe.Get("RXE.2.2");
            recBundle.Drug.TradeName = recBundle.Drug.DrugName;
            recBundle.Drug.GenericFor = rxe.Get("RXE.37.1");

            if (rxe.Get("RXE.25") != string.Empty)
            {
                // Check for concatenated RXE.25 & 26 -- RNA Helix does it, maybe others do too
                if (!string.IsNullOrEmpty(rxe.Get("RXE.25")))
                {
                    var dose = rxe.Get("RXE.25");
                    var unit = rxe.Get("RXE.26");
                    var doseLen = 0;

                    for (var i = 0; i < dose.Length; i++)
                    {
                        if (!char.IsDigit(dose[i]))
                        {
                            unit = dose.Substring(i);
                            dose = dose.Substring(0, doseLen);
                        }

                        doseLen++;
                    }

                    recBundle.Drug.Strength = Convert.ToDouble(dose);
                    recBundle.Drug.Unit = unit;
                }
            }

            recBundle.Drug.DoseForm = rxe.Get("RXE.6.1");

            if (!string.IsNullOrEmpty(rxe.Get("RXE.35.1")))
            {
                recBundle.Drug.DrugSchedule = ConvertFromRoman(rxe.Get("RXE.35.1") ?? "0");
            }

            recBundle.Scrip.DrugID = recBundle.Drug.NDCNum;
            recBundle.Scrip.QtyPerDose = Rnatq1DoseQty = Convert.ToDouble(rxe.Get("RXE.3.1") ?? "0.00");

            // This gets processed in ORC too but presumably RXE is preferred
            if (string.IsNullOrEmpty(recBundle.Scrip.PrescriptionID))
            {
                recBundle.Scrip.PrescriptionID = rxe.Get("RXE.15");
            }

            // Catch a Dose Schedule/Sig misplacement
            if (!string.IsNullOrEmpty(rxe.Get("RXE.7.1")) && rxe.Get("RXE.7.1").Trim().Length > 8)
            {
                if (Hl7SendingApp != HL7SendingApplication.RNA)
                {
                    recBundle.Scrip.Sig = rxe.Get("RXE.7.1");
                }
            }
            else
            {
                recBundle.Scrip.DoseScheduleName = rxe.Get("RXE.7.1");
            }

            if (!string.IsNullOrEmpty(rxe.Get("RXE.7.2")))
            {
                if (Hl7SendingApp != HL7SendingApplication.RNA)
                {
                    recBundle.Scrip.Sig += $" {rxe.Get("RXE.7.2")}";
                }
            }

            if (string.IsNullOrEmpty(recBundle.Scrip.DoseScheduleName))
            {
                recBundle.Scrip.DoseScheduleName = $"{DateTime.Now:yyyyddmmss}";
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (recBundle.Scrip.QtyDispensed == 0.00)
            {
                recBundle.Scrip.QtyDispensed = Convert.ToDouble(rxe.Get("RXE.10") ?? "0.00");
            }

            recBundle.Scrip.Refills = Convert.ToInt32(rxe.Get("RXE.16") ?? "0");

            //if (recBundle.Scrip.Refills == 0)
            //{
            //   recBundle.Scrip.Refills = Convert.ToInt32(rxe.Get("RXE.16") ?? "0");
            //}

            recBundle.Scrip.RxType = 0;
            recBundle.Scrip.PrescriberID = recBundle.Prescriber.PrescriberID;

            recBundle.Store.StoreID = rxe.Get("RXE.40.1");
            if (string.IsNullOrEmpty(recBundle.Store.StoreID))
            {
                recBundle.Store.StoreID = "0";
            }

            recBundle.Location.StoreID = recBundle.Store.StoreID;

            recBundle.Store.StoreName = rxe.Get("RXE.40.2");

            // 
            // There are 2 alternatives for entering data, for example an apartment building where the 
            // street name and is the same but the apt number is different.  It can be used lots of different
            // ways. It's more important with patient records.
            //
            recBundle.Store.Address1 = rxe.Get("RXE.41.1");
            recBundle.Store.Address2 = rxe.Get("RXE.41.2") + " " + rxe.Get("RXE.41.3");

            if (string.IsNullOrEmpty(recBundle.Store.Address1))
            {
                recBundle.Store.Address1 = recBundle.Store.Address2;
                recBundle.Store.Address2 = string.Empty;
            }

            recBundle.Store.City = rxe.Get("RXE.41.3");
            recBundle.Store.State = rxe.Get("RXE.41.4");
            recBundle.Store.Zipcode = rxe.Get("RXE.41.5");
        }

        private void ProcessRXO(RecordBundle recBundle, RXO rxo)
        {
            if (recBundle == null || rxo == null)
            {
                return;
            }

            ProblemLocus = "RXO";

            recBundle.Patient.DxNotes = rxo.Get("RXO.20.2") + " - " + rxo.Get("RXO.20.5");
        }

        private void ProcessRXR(RecordBundle recBundle, RXR rxr)
        {
            if (recBundle == null || rxr == null)
            {
                return;
            }

            ProblemLocus = "RXR";

            recBundle.Drug.Route = rxr.Get("RXR.1.1");
        }

        private string ProcessTQ1(RecordBundle recBundle, TQ1 tq1, bool newSet = false, int tq1RxType = 0)
        {
            if (recBundle == null || tq1 == null)
            {
                return string.Empty;
            }

            if (newSet)
            {
                CumulativeDoseCount = 0;
            }

            ProblemLocus = "TQ1";

            var doseTq = string.Empty;
            var doseScheduleName = tq1.Get("TQ1.3.1");
            var dosePriority = tq1.Get("TQ1.9.1") ?? "N";

            if (dosePriority.Contains("PRN"))
            {
                doseScheduleName = "PRN";
            }

            if (Hl7SendingApp == HL7SendingApplication.RNA)
            {
                if (string.IsNullOrEmpty(doseScheduleName))
                {
                    doseScheduleName = $"{DateTime.Now:yyyyyddmmss})";
                }
            }

            // Sanity Check
            string tq2 = tq1.Get("TQ1.2.1");
            var tq141 = tq1.GetList("TQ1.4.1");

            if (tq141.Count == 0)
            {
                tq141 = tq1.GetList("TQ1.4");

                if (tq141.Count == 0 && doseScheduleName != "PRN")
                {
                    if (!AllowZeroTQ)
                    {
                        throw new Exception("TQ1 Processing Failure - No Dose Times");
                    }
                }

                if (doseScheduleName == "PRN")
                {
                    tq141[0] = "1200";
                }
                else if (doseScheduleName == "PRNP") // [Frameworks usage - Take 1-2 tablets 3 times a day PRNP
                {
                    tq141.Add("0800");
                    tq141.Add("1200");
                    tq141.Add("1600");
                    tq2 = "1";
                    recBundle.Scrip.QtyDispensed = 3;
                }
            }

            if (Hl7SendingApp != HL7SendingApplication.RNA)
            {
                if (string.IsNullOrEmpty(tq2) || tq2 == "0")
                {
                    if (!AllowZeroTQ)
                    {
                        throw new Exception("TQ1 Processing Failure - Dose Quantity must be > 0");
                    }
                }
            }
            else
            {
                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (recBundle.Scrip.QtyPerDose == 0.00)
                {
                    if (!AllowZeroTQ)
                    {
                        throw new Exception("RNA/TQ1 Processing Failure - Dose Quantity must be > 0");
                    }
                }
            }

            recBundle.Scrip.RxType = 0; // Default Type
            recBundle.Scrip.DoseScheduleName = !string.IsNullOrEmpty(doseScheduleName) ? doseScheduleName : $"{DateTime.Now:yyyyddmmss})";

            var tqStartDate = recBundle.Scrip.TransformDate(tq1.Get("TQ1.7"));
            var tqStopDate = recBundle.Scrip.TransformDate(tq1.Get("TQ1.8"));


            if (tqStartDate.ToString("yyyy-MM-dd") != "1970-01-01")
            {
                recBundle.Scrip.RxStartDate = tqStartDate;
            }

            if (tqStopDate.ToString("yyyy-MM-dd") != "1970-01-01")
            {
                recBundle.Scrip.RxStopDate = tqStopDate;
            }


            var tq111 = tq1.Get("TQ1.11");

            if (!string.IsNullOrEmpty(tq111))
            {
                if (recBundle.Scrip.Sig != string.Empty)
                {
                    if (!recBundle.Scrip.Sig.Contains(tq111))
                    {
                        recBundle.Scrip.Sig += "\n";
                        recBundle.Scrip.Sig += tq111;
                    }
                }
                else
                {
                    recBundle.Scrip.Sig += tq111;
                }
            }

            recBundle.Scrip.Status = 1;

            // Get PRN's out of the way first
            if (doseScheduleName == "PRN")
            {
                recBundle.Scrip.DoseScheduleName = "PRN";
                recBundle.Scrip.RxType = 2;

                //RNA never sends a dose count for PRNs. It might be in RXE
                if (Hl7SendingApp == HL7SendingApplication.RNA)
                {
                    recBundle.Scrip.QtyPerDose = Rnatq1DoseQty;

                    foreach (var time in tq141)
                    {
                        recBundle.Scrip.DoseTimesQtys += $"{time}{Convert.ToDouble(tq2):00.00}";
                    }

                    return recBundle.Scrip.DoseTimesQtys;
                }

                recBundle.Scrip.QtyPerDose = Convert.ToDouble(tq2 ?? "00.00");
                return recBundle.Scrip.DoseTimesQtys = $"{tq141.FirstOrDefault()}{Convert.ToDouble(string.IsNullOrEmpty(tq2) ? "E" : tq2):00.00}";
            }


            // HL7 specific dose schedule pattern?
            if (IsHL7DoseSchedule(recBundle.Scrip.DoseScheduleName))
            {
                return ParsePatternedDoseSchedule(recBundle.Scrip.DoseScheduleName, tq1RxType, tq1, recBundle.Scrip, tqStartDate, tqStopDate);
            }

            // See if its a dose schedule we know about
            try
            {
                foreach (var tqTemp in tq141)
                {
                    if (tqTemp != "")
                    {
                        doseTq += $"{tqTemp}{Convert.ToDouble(tq2):00.00}";
                    }
                }

                return doseTq;
            }
            catch (Exception ex)
            {
                throw new Exception("Failure processing TQ1: " + ex.Message);
            }


            // recBundle.Scrip.QtyPerDose = TQ2;
            // return DoseTQ;
        }

        private void ProcessZAS(RecordBundle recBundle, ZAS zas)
        {
            if (recBundle == null || zas == null)
            {
                return;
            }

            ProblemLocus = "ZAS";
        }

        private void ProcessZLB(RecordBundle recBundle, ZLB zlb)
        {
            if (recBundle == null || zlb == null)
            {
                return;
            }

            ProblemLocus = "ZLB";
        }

        private void ProcessZPI(RecordBundle recBundle, ZPI zpi)
        {
            if (recBundle == null || zpi == null)
            {
                return;
            }

            ProblemLocus = "ZPI";

            //__scrip.RxSys_RxNum = __assign("ZPI-34", __fields);
            //__scrip.RxSys_DrugID = __assign("ZPI-21", __fields);
            //__scrip.RxStopDate = __assign("ZPI-26", __fields);


            // TODO:  Locate the store DEA Num
            //__store.DEANum = __assign("ZPI-21", __fields)?.Substring(0, 10);
        }

        private void ProcessZFI(RecordBundle recBundle, ZFI zfi)
        {
            if (recBundle == null || zfi == null)
            {
                return;
            }

            ProblemLocus = "ZFI";

            recBundle.Drug.DrugID = zfi.Get("ZF1.1");  // Item Id
            recBundle.Drug.TradeName = zfi.Get("ZF1.1");  // Item Id
            recBundle.Drug.DrugName = zfi.Get("ZFI.4");
            recBundle.Drug.GenericFor = zfi.Get("ZFI-4");
            recBundle.Drug.DoseForm = zfi.Get("ZFI.5");
            //__recs.__drug.Strength = Convert.ToInt32(__zfi.Get("ZFI.6") == null ? "0" : __zfi.Get("ZFI.6"));
            recBundle.Drug.Strength = Convert.ToDouble(zfi.Get("ZFI.6") ?? "0.00");
            recBundle.Drug.Unit = zfi.Get("ZFI.7");
            recBundle.Drug.NDCNum = zfi.Get("ZFI.8");
            //recBundle.Drug.DrugSchedule = Convert.ToInt32(_doseScheduleNameLookup.DrugSchedules[(zfi.Get("ZFI.10") ?? "0")]);
            recBundle.Drug.Route = zfi.Get("ZFI.11");
            recBundle.Drug.ProductCode = zfi.Get("ZFI.14");
        }

        private void ProcessCX1(RecordBundle recBundle, CX1 Cx1)
        {
            if (recBundle == null || Cx1 == null)
            {
            }
        }

        public string StackTrace(string st)
        {
            string trace = string.Empty;

            if (debugMode)
            {
                var lines = st.Split('\n');
                trace = lines.Where(l => l.Contains(" in ")).Aggregate(trace, (current, l) => current + (l.Substring(l.IndexOf("in", StringComparison.Ordinal)) + "\n"));
            }

            return trace;
        }

        public MSH GlobalMsh;

        private bool ProcessHeader(Header header, RecordBundle recBundle)
        {
            GlobalMsh = header.MSH;
            return true;
        }

        private bool ProcessPatient(hl7Patient newPatient, RecordBundle recBundle)
        {
            ProcessPID(recBundle, newPatient.PID);
            ProcessPV1(recBundle, newPatient.PV1);
            ProcessPV2(recBundle, newPatient.PV2);
            ProcessPD1(recBundle, newPatient.PD1);
            ProcessIN1(recBundle, newPatient.IN1);
            ProcessIN2(recBundle, newPatient.IN2);

            foreach (var prt in newPatient.PRT) { ProcessPRT(recBundle, prt); }
            foreach (var nte in newPatient.NTE) { recBundle.Patient.Comments += ProcessNTE(recBundle, nte); }
            foreach (var al1 in newPatient.AL1) { recBundle.Patient.Allergies += ProcessAL1(recBundle, al1); }
            foreach (var dg1 in newPatient.DG1) { recBundle.Patient.DxNotes += ProcessDG1(recBundle, dg1); }

            return true;
        }

        #region RNA Special Processing
        void ProcessRnaRenewOrder(NetworkStream stream, RecordBundle recBundle)
        {
            if (recBundle.MakeDupRnaScrip && Hl7SendingApp == HL7SendingApplication.RNA)
            {
                recBundle.MakeDupRnaScrip = false;
                var ds = recBundle.Scrip.ShallowCopy();
                ds.QueueWrites = false;
                ds.RxStartDate = recBundle.Scrip.RxStartDate;
                ds.DiscontinueDate = DateTime.MinValue;
                ds.PrescriptionID = ds.RxSys_NewRxNum;
                ds.RxSys_NewRxNum = string.Empty;
                ds.Status = 1;
                ds.Write(stream);
            }
        }
        #endregion

        private bool ProcessOrder(Order newOrder, RecordBundle recBundle)
        {
            var tq1RecordsProcessed = 0;
            var commentCounter = 0;

            recBundle.Patient.SetField("Action", "Change");
            recBundle.Scrip.Clear();
            recBundle.Prescriber.Clear();
            recBundle.Location.Clear();
            recBundle.Store.Clear();
            recBundle.Drug.Clear();

            ProcessORC(recBundle, newOrder.ORC);
            ProcessRXD(recBundle, newOrder.RXD);
            ProcessRXE(recBundle, newOrder.RXE);
            ProcessRXO(recBundle, newOrder.RXO);

            foreach (var prt in newOrder.PRT)
            {
                ProcessPRT(recBundle, prt);
            }

            var newTq1Set = true;

            foreach (var tq1 in newOrder.TQ1)
            {
                var tempTq = new MotTimesQtysRecord("Add", AutoTruncate);

                var tq1RxType = !string.IsNullOrEmpty(recBundle.Location.LocationID) ? Convert.ToInt32(recBundle.Scrip.RxType) : 0;
                tempTq.LocationID = !string.IsNullOrEmpty(recBundle.Location.LocationID) ? recBundle.Location.LocationID : "Home Care";
                tempTq.DoseTimesQtys = ProcessTQ1(recBundle, tq1, newTq1Set, tq1RxType);
                tempTq.DoseScheduleName = recBundle.Scrip.DoseScheduleName;

                // Sanity Check, TQ1 overrides RXE
                var tqDt = Convert.ToDouble(tempTq.DoseTimesQtys?.Substring(4, 4) ?? "00.00");

                // ReSharper disable once CompareOfFloatsByEqualityOperator
                if (recBundle.Scrip.QtyPerDose != tqDt)
                {
                    recBundle.Scrip.QtyPerDose = tqDt;
                }

                recBundle.TQList.Add(tempTq);
                recBundle.Scrip.Comments += $"({++tq1RecordsProcessed}) Dose Schedule: {recBundle.Scrip.DoseScheduleName}\n";

                newTq1Set = false;
            }

            foreach (var rxr in newOrder.RXR)
            {
                recBundle.Drug.Route = rxr.Get("RXR.1.2");
            }

            if (string.IsNullOrEmpty(recBundle.Scrip.Comments))
            {
                recBundle.Scrip.Comments += "Patient Notes:";
            }
            foreach (var nte in newOrder.NTE)
            {
                recBundle.Scrip.Comments += $"\n  {commentCounter++}) {ProcessNTE(recBundle, nte)}\n";
            }

            foreach (var rxc in newOrder.RXC)
            {
                ProcessRXC(recBundle, rxc);
            }

            // Ugly Kludge but HL7 doesn't seem to have the notion of a store DEA Number -- I'm still looking
            //if (string.IsNullOrEmpty(recBundle.Store.DEANum))
            //{
            //    recBundle.Store.DEANum = "XX1234567";   // This should get the attention of the pharmacist
            //}

            if (string.IsNullOrEmpty(recBundle.Scrip.PatientID))
            {
                recBundle.Scrip.PatientID = recBundle.Patient.PatientID;
            }

            // There's no place but the order to get the location ID, so grab it now
            recBundle.Patient.LocationID = newOrder.ORC.Get("ORC.21.3") ?? DefaultStoreLocation;

            return true;
        }

        /// <summary>
        /// Returns result codes from MOT 
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ReturnProcessor(byte[] data)
        {
            if (data == null || data[0] == 0x6)
            {
                return true;
            }

            // Process return code here ...
            EventLogger.Error("!!! {0} Failed to write {1}", DateTime.Now, data);
            throw new Exception("Data IO Failed [" + data + "]");

        }

        public void ProcessADTEvent(object sender, HL7Event7MessageArgs args)
        {
            var recBundle = new RecordBundle(AutoTruncate, SendEof);
            var tq = string.Empty;
            var temp = string.Empty;

            recBundle.MessageType = MessageType;
            recBundle.SetDebug(debugMode);

            var xmlDoc = GetStructuredData(args.Data);
            var adt = new ADT_A01(xmlDoc);

            // Setup fofr ADT processing
            EventCode = ProcessEVN(recBundle, adt.EVN);

            try
            {
                using (var localTcpClient = new TcpClient(Socket.Address, Socket.Port))
                {
                    using (var stream = localTcpClient.GetStream())
                    {
                        ProcessPID(recBundle, adt.PID);
                        ProcessPV1(recBundle, adt.PV1);
                        ProcessPV2(recBundle, adt.PV2);
                        ProcessPD1(recBundle, adt.PD1);

                        foreach (var obx in adt.OBX) { ProcessOBX(recBundle, obx); }
                        foreach (var al1 in adt.AL1) { recBundle.Patient.Allergies += ProcessAL1(recBundle, al1); }
                        foreach (var dg1 in adt.DG1) { recBundle.Patient.DxNotes += ProcessDG1(recBundle, dg1); }
                        foreach (var in1 in adt.IN1) { ProcessIN1(recBundle, in1); }
                        foreach (var in2 in adt.IN2) { ProcessIN2(recBundle, in2); }
                        foreach (var nk1 in adt.NK1) { recBundle.Patient.ResponisbleName += ProcessNK1(recBundle, nk1); }

                        // Make sure the proper actions are taken for the event
                        ProcessEVN(recBundle, adt.EVN);

                        if (string.IsNullOrEmpty(recBundle.Prescriber.LastName))
                        {
                            recBundle.Prescriber.DontSend = true;
                        }

                        if (string.IsNullOrEmpty(recBundle.Patient.LocationID))
                        {
                            recBundle.Patient.LocationID = DefaultStoreLocation;
                        }

                        recBundle.Write();
                        recBundle.Commit(stream);
                    }
                }

                EventLogger.Info("Processed {0}\n{1}", $"{MessageType}^{EventCode}", args.Data);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Processing Failure: {0}", ex.StackTrace);
                throw new HL7Exception(199, $"Message Processing Failure: {MessageType}^{EventCode}/{ProblemLocus}: {ex.Message}");
            }
        }

        public void ProcessOMPMessage(object sender, HL7Event7MessageArgs args)
        {
            var recBundle = new RecordBundle(AutoTruncate, SendEof);

            // Only OMP_0O9 for now, abstract this next and then specifiy it in the following switch
            OMP_O09 omp;

            recBundle.MessageType = MessageType;
            recBundle.SetDebug(debugMode);

            var xmlDoc = GetStructuredData(args.Data);

            switch (EventCode)
            {
                case "O09":
                    omp = new OMP_O09(xmlDoc);
                    break;

                default:
                    throw new NotSupportedException($"RDE {EventCode} not supported");
            }

            try // Process the Patient
            {
                ProcessHeader(omp.Header1, recBundle);
                ProcessPatient(omp.Patient, recBundle);

                using (var localTcpClient = new TcpClient(Socket.Address, Socket.Port))
                {
                    using (var stream = localTcpClient.GetStream())
                    {
                        recBundle.Patient.Write(stream, debugMode);
                        recBundle.Patient.SetField("Action", "Change");

                        foreach (Order order in omp.Orders)
                        {
                            ProcessOrder(order, recBundle);
                            recBundle.Write();
                        }


                        recBundle.Commit(stream);

                        ProcessRnaRenewOrder(stream, recBundle);
                    }
                }

                EventLogger.Info("Processed {0}\n{1}", $"{MessageType}^{EventCode}", args.Data);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Processing Failure: {0}", ex.StackTrace);
                throw new HL7Exception(199, $"Message Processing Failure: {MessageType}^{EventCode}/{ProblemLocus}: {ex.Message}");
            }
        }

        // Drug Order       MSH, [ PID, [PV1] ], { ORC, [RXO, {RXR}, RXE, [{NTE}], {TQ1}, {RXR}, [{RXC}] }, [ZPI]
        // Literal Order    MSH, PID, [PV1], ORC, [TQ1], [RXE], [ZAS]
        // TODO:  CHeck the Framework SPec for where the order types live
        public void ProcessRDEMessage(object sender, HL7Event7MessageArgs args)
        {
            var recBundle = new RecordBundle(AutoTruncate, SendEof);

            recBundle.MessageType = MessageType;
            recBundle.SetDebug(debugMode);

            // Only RDE_O11 for now, abstract this next and then specifiy it in the following switch
            RDE_O11 rde;

            var xmlDoc = GetStructuredData(args.Data);

            switch (EventCode)
            {
                case "O11":
                    rde = new RDE_O11(xmlDoc);
                    break;

                default:
                    throw new NotSupportedException($"RDE {EventCode} not supported");
            }

            try
            {
                using (var localTcpClient = new TcpClient(Socket.Address, Socket.Port))
                {
                    using (var stream = localTcpClient.GetStream())
                    {
                        ProcessHeader(rde.Header, recBundle);
                        ProcessPatient(rde.Patient, recBundle);

                        ProblemLocus = "Patient";
                        recBundle.Patient.Write(stream, debugMode);

                        foreach (var order in rde.Orders)
                        {
                            ProcessOrder(order, recBundle);
                            ProblemLocus = "Order";
                            recBundle.Write();
                        }

                        ProblemLocus = "Commit";
                        recBundle.Commit(stream);

                        ProcessRnaRenewOrder(stream, recBundle);
                    }
                }

                EventLogger.Info("Processed {0}\n{1}", $"{MessageType}^{EventCode}", args.Data);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Processing Failure: {0}", ex.StackTrace);
                throw new HL7Exception(199, $"Message Processing Failure: {MessageType}^{EventCode}/{ProblemLocus}: {ex.Message}");
            }
        }

        public void ProcessRDSMessage(object sender, HL7Event7MessageArgs args)
        {
            var recBundle = new RecordBundle(AutoTruncate, SendEof);
            var problemSegment = $"{MessageType}^{EventCode}";

            recBundle.MessageType = MessageType;
            recBundle.SetDebug(debugMode);

            // Only RDS_O13 for now, abstract this next and then specifiy it in the following switch
            RDS_O13 rds;

            var xmlDoc = GetStructuredData(args.Data);

            switch (EventCode)
            {
                case "O13":
                    rds = new RDS_O13(xmlDoc);
                    break;

                default:
                    throw new NotSupportedException($"RDE {EventCode} not supported");
            }

            try
            {
                using (var localTcpClient = new TcpClient(Socket.Address, Socket.Port))
                {
                    using (var stream = localTcpClient.GetStream())
                    {
                        ProcessHeader(rds.Header, recBundle);
                        ProcessPatient(rds.Patient, recBundle);

                        recBundle.Patient.Write(stream, debugMode);
                        recBundle.Patient.SetField("Action", "Change");

                        foreach (Order order in rds.Orders)
                        {
                            ProcessOrder(order, recBundle);
                            recBundle.Write();
                        }

                        recBundle.Commit(stream);

                        ProcessRnaRenewOrder(stream, recBundle);
                    }
                }

                EventLogger.Info("Processed {0}\n{1}", problemSegment, args.Data);
            }
            catch (Exception ex)
            {
                EventLogger.Error("Processing Failure: {0}", ex.StackTrace);
                throw new HL7Exception(199, $"Message Processing Failure: {MessageType}^{EventCode}/{ProblemLocus}: {ex.Message}");
            }
        }
    }
}
