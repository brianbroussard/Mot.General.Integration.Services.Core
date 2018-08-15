using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MotCommonLib;
using Newtonsoft.Json;
using MotWebApiLib;

namespace CommonTests
{
    public class Rx
    {
        public string RxSystemId { get; set; }
        public string Status { get; set; }
        public DateTime DcDate { get; set; }
        public string CardSig { get; set; }
        public string ReportSig { get; set; }
        public bool IsSigFieldsLinked { get; set; }
        public double QuantityWritten { get; set; }
        public int Refills { get; set; }
        public double RemainingRefills { get; set; }
        public string WrittenDate { get; set; }
        public string DrugId { get; set; }
        public object ChartOnly { get; set; }
        public object Isolate { get; set; }
        public bool IsolateOnChartingForm { get; set; }
        public object PreviousRxId { get; set; }
        public bool MailOrder { get; set; }
        public bool IsHidden { get; set; }
        public string RefillRequestType { get; set; }
        public object RefillRequestDate { get; set; }
        public int CardGroup { get; set; }
        public object RxTypeChangeReason { get; set; }
        public object LotExpirationDate { get; set; }
        public object LotNumber { get; set; }
        public object PrnDaycareQuantity { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime StopDate { get; set; }
        public string PrescriberId { get; set; }
        public int ArResultLines { get; set; }
        public string PatientId { get; set; }
        public string StoreId { get; set; }
        public string FacilityId { get; set; }
        public string RxDosageRegimenId { get; set; }
        public bool IsIncomplete { get; set; }
        public string Id { get; set; }
        public int Version { get; set; }
    }

    public class NextRx
    {
        public string odatacContext { get; set; }
        public List<Rx> value { get; set; }
    }

    [TestClass]
    public class WebTests
    {
        public class Test1 : MotJsonObjectBase
        {
            public bool Built { get; set; }

            public Test1(ServerContext context) : base(context)
            {
                Built = true;
            }
        }

        [TestMethod]
        public IEnumerable<Rx> GetRxList()
        {
            try
            {
                var context = new ServerContext("https://proxyplayground.medicineontime.com", "odata", "Pete.Jenney", "$!Secure2017");
                var connection = new Test1(context);

                var scrips = connection.Get<NextRx>("Rxes", $"?$filter=Refills eq 5").Result;
                if (scrips == null)
                {
                    Assert.Fail("Failed to get data");
                }

                return scrips.value;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }

            return null;
        }

        public Rx GetRx()
        {
            try
            {
                var context = new ServerContext("https://proxyplayground.medicineontime.com", "odata", "Pete.Jenney", "$!Secure2017");
                var connection = new Test1(context);

                var scrip = connection.Get<NextRx>("Rxes", "?$filter=RxSystemId eq 'Q541321'").Result;
                if (scrip == null)
                {
                    Assert.Fail("No data found");
                }

                return scrip.value.First();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }

            return null;
        }
    }
}