using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Mot.Common.Interface.Lib;
using Mot.Polling.Interface.Lib;

namespace CommonTests
{
    [TestClass]
    public class PollingTests
    {
        [TestMethod]
        public void CreateObjects()
        {
            try
            {
                var _mutex = new Mutex();
                var GatewayIp = "192.168.1.160";
                var GatewayPort = 24045;
                var DataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    DataSource = GatewayIp;
                }

                using (var MotSqlServer = new MotSqlServer($"Data Source={DataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    var patient = new PollPatient(MotSqlServer, _mutex, GatewayIp, GatewayPort);
                    var prescriber = new PollPrescriber(MotSqlServer, _mutex, GatewayIp, GatewayPort);
                    var facility = new PollFacility(MotSqlServer, _mutex, GatewayIp, GatewayPort);
                    var prescription = new PollPrescription(MotSqlServer, _mutex, GatewayIp, GatewayPort);
                    var drug = new PollDrug(MotSqlServer, _mutex, GatewayIp, GatewayPort);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void QueryPatient()
        {
            try
            {
                var _mutex = new Mutex();
                var GatewayIp = "192.168.1.160";
                var GatewayPort = 24045;
                var DataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    DataSource = GatewayIp;
                }

                using (var MotSqlServer = new MotSqlServer($"Data Source={DataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var patient = new PollPatient(MotSqlServer, _mutex, GatewayIp, GatewayPort))
                    {
                        try
                        {
                            patient.ReadPatientRecords();
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e);
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
               Assert.Fail(ex.Message);
            }
        }

        [TestMethod]
        public void StartPharmaserve()
        {
            var _mutex = new Mutex();
            var GatewayIp = "192.168.1.160";
            var GatewayPort = 24045;
            var DataSource = "PROXYPLAYGROUND";

            if (GetPlatformOs.Go() != PlatformOs.Windows)
            {
                DataSource = GatewayIp;
            }

            using (var MotSqlServer = new MotSqlServer($"Data Source={DataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
            {
                using (var ps = new Pharmaserve(MotSqlServer, GatewayIp, GatewayPort))
                {
                    Thread.Sleep(500000);
                }
            }
        }
    }
}