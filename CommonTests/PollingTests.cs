using System;
using System.Data;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using MotCommonLib;
using TransformerPollingService;

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
                var MotSqlServer = new MotSqlServer("Data Source=PROXYPLAYGROUND;" +
                                                    "Initial Catalog=McKessonTestDb;" +
                                                    "User ID=sa;Password=$MOT2018" +
                                                    "Connect Timeout=60;" +
                                                    "Encrypt=False;" +
                                                    "TrustServerCertificate=False" +
                                                    "ApplicationIntent=ReadWrite;" +
                                                    "MultiSubnetFailover=false");

                var p = new PollPatient(MotSqlServer, _mutex, GatewayIp, GatewayPort);
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
        }
    }
}