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
                var mutex = new Mutex();
                var gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    var patient = new PollPatient(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var doc = new PollPrescriber(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var facility = new PollFacility(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var scrip = new PollPrescription(motSqlServer, mutex, gatewayIp, gatewayPort);
                    var drug = new PollDrug(motSqlServer, mutex, gatewayIp, gatewayPort);
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
                var mutex = new Mutex();
                const string gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var patient = new PollPatient(motSqlServer, mutex, gatewayIp, gatewayPort))
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
        public void QueryRx()
        {
            try
            {
                var mutex = new Mutex();
                var gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var Rx = new PollPrescription(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            Rx.ReadPrescriptionRecords();
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
        public void QueryFacility()
        {
            try
            {
                var mutex = new Mutex();
                var gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var facility = new PollFacility(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            facility.ReadFacilityRecords();
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
        public void QueryDoc()
        {
            try
            {
                var mutex = new Mutex();
                var gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var doc = new PollPrescriber(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            doc.ReadPrescriberRecords();
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
        public void QueryDrug()
        {
            try
            {
                var mutex = new Mutex();
                var gatewayIp = "192.168.1.160";
                var gatewayPort = 24045;
                var dataSource = "PROXYPLAYGROUND";

                if(GetPlatformOs.Go() != PlatformOs.Windows)
                {
                    dataSource = gatewayIp;
                }

                using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
                {
                    using (var drug = new PollDrug(motSqlServer, mutex, gatewayIp, gatewayPort))
                    {
                        try
                        {
                            drug.ReadDrugRecords();
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
            var mutex = new Mutex();
            var gatewayIp = "192.168.1.160";
            var gatewayPort = 24045;
            var dataSource = "PROXYPLAYGROUND";

            if (GetPlatformOs.Go() != PlatformOs.Windows)
            {
                dataSource = gatewayIp;
            }

            using (var motSqlServer = new MotSqlServer($"Data Source={dataSource};Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"))
            {
                using (var ps = new Pharmaserve(motSqlServer, gatewayIp, gatewayPort))
                {
					ps.RefreshRate = 1000;
					ps.Go();
                    Thread.Sleep(10000);
					ps.Stop();                  
                }
            }
        }
    }
}