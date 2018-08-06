﻿using System;
using System.Collections.Generic;
using MotCommonLib;
using System.Net.Sockets;

namespace MotParserLib
{
    /*
    Field name	        Start	End	Length	Notes	 	Justified
    Patient name	        1   20	20	 	alphanumeric	L
    Patient location	    21	32	12	    Room:’/’:Bed:’/’:Nursing station	alphanumeric	L
    Patient ID	            33	44	12	    Patient number	alphanumeric	L
    Med ID	                45	59	15	    The first 9 digits of the NDC followed by one character code indicating pill segment:	alphanumeric	L
 	 	 	 	W = WHOLE PILL	 	L
 	 	 	 	H = HALF PILL	 	L
 	 	 	 	T = THIRD PILL	 	L
 	 	 	 	Q = QUARTER PILL    L
 	 	 	 	However, there is also a rule that supports a freeform medication MNEMONIC .  Following the medication MNEMONIC is the partial pill character code.* {discuss with Rescot}	 	L
    Med administration date	60	65	6	MMDDYY	numeric	L
    Med administration time	66	69	4	HHMM	numeric	L
    Dose	                70	71	2	Integer number of pills given at this Med administration time,. Always preceded with leading zero, e.g. ‘01’, ‘02’, etc.	numeric	R
    Prescriber name	        72	97	26	Last name:’  ‘:First name	alphanumeric	L
    Rx order number	        98	113	16	Prescription number	alphanumeric	R
    Notes	                114	137	24	Typically blank unless this is a PRN dose and the string “PRN” will be used in this record	alpha	L
    Facility ID	            138	145	8	Facility number	numeric	L
    Patient last name	    146	170	25	 	alphanumeric	L
    Patient first name	    171	190	20	 	alphanumeric	L
    Patient middle initial	191	191	1	 	alpha 	L   
    Facility name	        192	221	30	 	alphanumeric	L
    NDC.11	                222	232	11	NDC of the shipped item code	numeric	L
    SIG directions	        233	432	200	 	alphanumeric	L
    Expanded directions	    433	732	300	 	alphanumeric	L
    Ordered item code	    733	746	14	 	alphanumeric	L   
    Shipped item code	    747	760	14	 	alphanumeric	L
    Substitution type indicator	761	761	1	 	alpha	L
    Item like text	        762	811	50	 	alphanumeric	L
    Refills remaining	    812	814	3	 	numeric	L
    Drug caution messages	815	1814	1000	 	alphanumeric	L
    Patient Address (Facility Address)	1815	1922	108	Address of facility (where patient resides)	alphanumeric	L
    PV1 Initials	        1923	1930	8	Initials of of person performing PV1 (e.g. LBJ)	Alpha	L
    Pharmacy Name	        1931	1980	50	Name of Pharmacy	Alpha	L
    BranchID	            1981	1985	5	Hub identifier 3 byte alphanumeric, for example Omnicare of Indianopolis would be "PRN"	alphanumeric	L
    FacilityStation	        1986	1995	10	Facility Unit Identifier (e.g. "EAST","NORTH","WING1",..)	alphanumeric	L
    MagicCookie	            1996	2011	16	Magic Cookie value, Oasis will send a 16 digit barcode value; DX 16 spaces	alphanumeric	L
    */


    class PsEdiData
    {
#pragma warning disable CS0649
        [Layout(0, 19)]
        public string patientName;
        [Layout(20, 31)]
        public string pationLoc;
        [Layout(32, 43)]
        public string patientId;
        [Layout(44, 58)]
        public string medId;
        [Layout(59, 64)]
        public DateTime medAdminDate;
        [Layout(65, 68)]
        public DateTime medAdminTime;
        [Layout(69, 70)]
        public ushort dose;
        [Layout(71, 96)]
        public string prescriberName;
        [Layout(97, 112)]
        public string rxOrderNumber;
        [Layout(114, 136)]
        public string notes;
        [Layout(137, 144)]
        public string facilityId;
        [Layout(145, 169)]
        public string patientLastName;
        [Layout(170, 189)]
        public string patientFirstName;
        [Layout(190, 190)]
        public string patientMiddleInitial;
        [Layout(191, 220)]
        public string facilityName;
        [Layout(221, 231)]
        public string ndc11;
        [Layout(232, 431)]
        public string sig;
        [Layout(432, 731)]
        public string expandedDirections;
        [Layout(732, 745)]
        public string orderedItemCode;
        [Layout(746, 759)]
        public string shippedItemCode;
        [Layout(760, 760)]
        public string substitutionTypeIndicator;
        [Layout(761, 810)]
        public string itemLikeText;
        [Layout(811, 813)]
        public string refillsRemaining;
        [Layout(814, 1813)]
        public string drugCautionMessages;
        [Layout(1814, 1921)]
        public string patientAddressOrFacilityAddress;
        [Layout(1922, 1929)]
        public string pv1Initials;
        [Layout(1930, 1979)]
        public string pharmacyName;
        [Layout(1980, 1984)]
        public string branchId;
        [Layout(1985, 1994)]
        public string facilityStation;
        [Layout(1995, 2010)]
        public string magicCookie;
#pragma warning restore CS0649
    }

    class PsEdiParser : ParserBase, IDisposable
    {
        private List<EdiRecord> _recordList;

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

        public PsEdiParser(string inputStream) : base(inputStream)
        {
            _recordList = new List<EdiRecord>();
        }

        class EdiRecord
        {
            public MotPatientRecord Patient { get; }
            public MotPrescriptionRecord Scrip { get; }
            public MotFacilityRecord Facility { get; }

            public EdiRecord()
            {
                Patient = new MotPatientRecord();
                Scrip = new MotPrescriptionRecord();
                Facility = new MotFacilityRecord();
            }
        }

        protected override void WriteListToGateway()
        {
            using (var localTcpClient = new TcpClient(GatewaySocket.Address, GatewaySocket.Port))
            {
                using (var stream = localTcpClient.GetStream())
                {
                    foreach (var rec in _recordList)
                    {
                        rec.Patient.Write(stream);
                        rec.Scrip.Write(stream);
                        rec.Facility.Write(stream);
                    }
                }
            }
        }
        private void ProcessRecord(PsEdiData ediData)
        {
            try
            {
                var ediRecord = new EdiRecord();

                ediRecord.Patient.LastName = ediData.patientLastName;
                ediRecord.Patient.FirstName = ediData.patientFirstName;
                ediRecord.Patient.MiddleInitial = ediData.patientMiddleInitial;

                ediRecord.Scrip.DrugID = ediData.ndc11;
                ediRecord.Scrip.PrescriptionID = ediData.rxOrderNumber;
                ediRecord.Scrip.RxStartDate = ediData.medAdminDate;
                ediRecord.Scrip.DoseTimesQtys = ediData.medAdminTime.ToString("HHmm") + ediData.dose.ToString("00");
                ediRecord.Scrip.QtyPerDose = Convert.ToDouble(ediData.dose);
                ediRecord.Scrip.Refills = Convert.ToInt32(ediData.refillsRemaining ?? "0");
                ediRecord.Scrip.Sig = $"{ediData.sig}\n{ediData.expandedDirections}";

                ediRecord.Facility.LocationID = ediData.facilityId;
                ediRecord.Facility.LocationName = ediData.facilityName;

                //
                // Apparently the system will send over a record per dose.  We'll treat the first data found as the start date
                // and carry on from there.
                //
                if (_recordList.Find(x => x.Scrip.PrescriptionID == ediRecord.Scrip.PrescriptionID) == null)
                {
                    _recordList.Add(ediRecord);
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
            if (string.IsNullOrEmpty(data))
            {
                throw new ArgumentNullException();
            }

            if ((data.Length % 2011) != 0)
            {
                throw new ArgumentException("Invalid data length");
            }

            var blockCount = data.Length / 2011;

            for (var i = 0; i < blockCount; i += 2011)
            {
                var chunk = data.Substring(i, 2011);
                var record = new PsEdiData();
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
