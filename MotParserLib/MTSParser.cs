using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using MotCommonLib;

namespace MotParserLib
{
    /*
    TABLE.CODE	5
    FILL.ACTION.CODE	10
    OL|DAILY.ACTIVITY.LOG.NUM	10
    OL|RX.ORDER.NUM	9
    OL|FACILITY.NUM	8
    OL|PATIENT.NUM	8
    OL|NURSING.STATION	10
    OL|ROOM	5
    OL|BED	2
    OL|LAST.NAME	25
    OL|FIRST.NAME	20
    OL|MIDDLE.INITIAL	1
    OL|ORDER.DATE.YYYYMMDD	8
    OL|ENTRY.DATE.YYYYMMDD	8
    OL|ENTRY.TIME.INTERNAL	6
    OL|ORDERED.ITEM.CODE	14
    OL|SHIPPED.ITEM.CODE	14
    OL|ITEM.LIKE.TEXT	50
    OL|QUANTITY	6
    OL|DAYS.SUPPLY	3
    OL|USER.CODE	8
    OL|RPH.INITIALS	8
    OL|TRUE.NUMBER.OF.LABELS	2
    OL|SUBSTITUTION.TYPE.INDICATOR	1
    OL|REFILLS.REMAINING	3
    OL|SIG.DIRECTIONS	200
    OL|EXPANDED.DIRECTIONS	300
    OL|PHYSICIAN.LAST.NAME	25
    OL|PHYSICIAN.FIRST.NAME	25
    OL|PHYSICIAN.DEA.NUM	9
    OL|PATIENT.STATUS	10
    OL|SPECIAL.PROCESSING.CODE	10
    OL|NEW.ORDER.INDICATOR	1
    OL|TRACKING.ID	15
    OL|ORIGINAL.ORDER.DATE.YYYYMMDD	8
    OL|ORDER.NOTES	300
    OL|TOTE.ID	10
    OL|PASS.TIMES	35
    OL|PASS.DOSES	35
    OL|WEEKLY.FREQUENCY	25
    OL|MONTHLY.FREQUENCY	25
    OL|ROLLING.FREQUENCY	10
    OL|ORDER.START.DATE.YYYYMMDD	8
    OL|RESPONSIBLE.PARTY	6
    OL|OEL.DELIVERY.CODE	10
    OL|REORDER.DATE.YYYYMMDD	10
    OL|BILL.TO.INDICATOR	1
    OL|RPH.NAME	40
    OL|USE.FREQUENCY.INDICATOR	1
    OL|FILL.START.DATE.YYYYMMDD	8
    OL|CYCLE.FILL	1
    OL|FULL.PASS.TIMES	80
    OL|FULL.PASS.DOSES	35
    OL|FULL.WEEKLY.FREQUENCY	25
    OL|FULL.MONTHLY.FREQUENCY	25
    OL|FULL.ROLLING.FREQUENCY	10
    OL|DRUG.CAUTION.MESSAGES	1000
    OL|COLOR.PASS.TIMES	20
    */
    class MTSData
    {
#pragma warning disable CS0649
        [Layout(0, 4)]
        public string tableCode;
        [Layout(5, 14)]
        public string fillActionCode;
        [Layout(15, 24)]
        public string dailyActivityLogNum;
        [Layout(25, 33)]
        public string rxOrderNum;
        [Layout(34, 41)]
        public string facilityNum;
        [Layout(42, 49)]
        public string patientNum;
        [Layout(50, 59)]
        public string nursingStation;
        [Layout(60, 64)]
        public string room;
        [Layout(65, 66)]
        public string bed;
        [Layout(67, 91)]
        public string lastName;
        [Layout(92, 111)]
        public string firstName;
        [Layout(112, 112)]
        public string middleInitial;
        [Layout(113, 120)]
        public DateTime orderDate;
        [Layout(121, 128)]
        public DateTime entryDate;
        [Layout(129, 134)]
        public DateTime entryTimeInternal;
        [Layout(135, 148)]
        public string orderedItemCode;
        [Layout(149, 162)]
        public string shippedItemCode;
        [Layout(163, 212)]
        public string itemLikeText;
        [Layout(213, 218)]
        public int quantity;
        [Layout(219, 221)]
        public int daysSupply;
        [Layout(222, 229)]
        public string userCode;
        [Layout(230, 237)]
        public string rphInitials;
        [Layout(238, 239)]
        public string trueNumberOfLabels;
        [Layout(240, 240)]
        public string substitutionTypeIndicator;
        [Layout(241, 243)]
        public string refillsRemaining;
        [Layout(244, 443)]
        public string sigDirections;
        [Layout(444, 743)]
        public string expandedDirections;
        [Layout(744, 768)]
        public string physicianLastName;
        [Layout(769, 793)]
        public string physicianFirstName;
        [Layout(794, 802)]
        public string physcianDeaCode;
        [Layout(803, 812)]
        public string patientStatus;
        [Layout(813, 822)]
        public string specialProcessingCode;
        [Layout(823, 823)]
        public string newOrderIndicator;
        [Layout(824, 838)]
        public string trackingId;
        [Layout(839, 846)]
        public DateTime originalOrderDate;
        [Layout(847, 1146)]
        public string orderNotes;
        [Layout(1147, 1156)]
        public string toteId;
        [Layout(1157, 1191)]
        public string passTimes;
        [Layout(1192, 1226)]
        public DateTime passDoses;
        [Layout(1227, 1251)]
        public string weeklyFrequency;
        [Layout(1252, 1276)]
        public string monthlyFrequency;
        [Layout(1277, 1286)]
        public string rollingFrequency;
        [Layout(1287, 1294)]
        public DateTime orderStartDate;
        [Layout(1294, 1300)]
        public string responsibleParty;
        [Layout(1301, 1310)]
        public string oelDeliveryCode;
        [Layout(1311, 1320)]
        public DateTime reorderDate;
        [Layout(1321, 1321)]
        public string billToIndicator;
        [Layout(1322, 1361)]
        public string rphName;
        [Layout(1362, 1362)]
        public string useFrequencyIndicator;
        [Layout(1363, 1370)]
        public DateTime fillStartDate;
        [Layout(1371, 1371)]
        public DateTime cycleFill;
        [Layout(1372, 1451)]
        public string fullPassTimes;
        [Layout(1452, 1486)]
        public string fullPassDoses;
        [Layout(1487, 1511)]
        public string fullWeeklyFrequency;
        [Layout(1512, 1536)]
        public string fullMonthlyFrequency;
        [Layout(1537, 1546)]
        public string fullRollingFrequency;
        [Layout(1547, 2546)]
        public string drugCautionMessages;
        [Layout(2547, 2566)]
        public string colorPassTimes;

#pragma warning restore CS0649
    }
    class MtsRecord
    {
        public MotPatientRecord Patient { get; set; }
        public MotPrescriptionRecord Scrip { get; set; }
        public MotFacilityRecord Facility { get; set; }
        public MotPrescriberRecord Doc { get; set; }

        public MtsRecord()
        {
            Patient = new MotPatientRecord();
            Scrip = new MotPrescriptionRecord();
            Facility = new MotFacilityRecord();
            Doc = new MotPrescriberRecord();
        }
    }
    class MtsParser : ParserBase, IDisposable
    {
        private List<MtsRecord> _recordList;
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected new virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _recordList.Clear();
                _recordList = null;
            }
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public new void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public MtsParser(string inputStream) : base(inputStream)
        {
            _recordList = new List<MtsRecord>();
        }

        protected override void WriteListToGateway()
        {
            using (var localTcpClient = new TcpClient(GatewaySocket.Address, GatewaySocket.Port))
            {
                using (var stream = localTcpClient.GetStream())
                {
                    foreach (var rec in _recordList)
                    {
                        rec.Patient.Write(stream, DebugMode);
                        rec.Scrip.Write(stream, DebugMode);
                        rec.Doc.Write(stream, DebugMode);
                        rec.Facility.Write(stream, DebugMode);
                    }
                }
            }
        }
        private void ProcessRecord(MTSData mtsData)
        {
            try
            {
                var mtsRecord = new MtsRecord
                {
                    Patient =
                    {
                        PatientID = mtsData.patientNum,
                        LastName = mtsData.lastName,
                        FirstName = mtsData.firstName,
                        MiddleInitial = mtsData.middleInitial,
                        Room = mtsData.room,
                        ResponisbleName = mtsData.rphName
                    },
                    Scrip =
                    {
                        DrugID = mtsData.orderedItemCode,
                        RxSys_RxNum = mtsData.rxOrderNum,
                        RxStartDate = mtsData.fillStartDate,
                        Refills = Convert.ToInt32(mtsData.refillsRemaining ?? "0"),
                        Sig = $"{mtsData.sigDirections}\n{mtsData.expandedDirections}",
                        Comments = mtsData.drugCautionMessages
                    },
                    Facility = {LocationID = mtsData.facilityNum},
                    Doc =
                    {
                        LastName = mtsData.physicianLastName,
                        FirstName = mtsData.physicianFirstName,
                        DEA_ID = mtsData.physcianDeaCode
                    }
                };


                //mtsRecord.Scrip.DoseTimesQtys = mtsData.MedAdminTime.ToString("HHmm") + mtsData.Dose.ToString("00");
                //mtsRecord.Scrip.QtyPerDose = mtsData.Dose.ToString();



                //
                // Apparently the system will send over a record per dose.  We'll treat the first data found as the start date
                // and carry on from there.
                //
                if (_recordList.Find(x => x.Scrip.RxSys_RxNum == mtsRecord.Scrip.RxSys_RxNum) == null)
                {
                    _recordList.Add(mtsRecord);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        public void Go()
        {
            // Process protected data
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException();
            }

            if ((data.Length % 2011) != 0)
            {
                throw new ArgumentException("Invalid data length");
            }

            var blockCount = data.Length / 2011;

            for (var i = 0; i < blockCount; i += 2566)
            {
                var chunk = data.Substring(i, 2566);
                var record = new MTSData();

                using (var dataBlock = new FixedLengthReader(chunk))
                {
                    dataBlock.Read(record);
                    ProcessRecord(record);
                }
            }

            WriteListToGateway();
        }
    }
}
