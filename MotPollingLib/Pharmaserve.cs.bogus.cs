using System;
using System.Threading;
using Mot.Common.Interface.Lib;
using NLog;

namespace Mot.Polling.Interface.Lib
{
    public class Pharmaserve : IDisposable
    {
        public int RefreshRate { get; set; }

        private Thread _waitForDrug;
        private Thread _waitForFacility;
        private Thread _waitForPatient;
        private Thread _waitForPrescriber;
        private Thread _waitForPrescription;
        private Thread _waitForStore;
        private Thread _waitForTq;

        private MotSqlServer MotSqlServer { get; set; }
        private readonly Mutex _mutex;

        private string GatewayIp { get; set; }
        private int GatewayPort { get; set; }

        private Logger EventLogger { get; set; }

        private bool KeepRunning { get; set; }

        public Pharmaserve(MotSqlServer motSqlServer, string gatewayIp, int gatewayPort)
        {
            try
            {
                this.MotSqlServer = motSqlServer;
                _mutex = new Mutex();
                EventLogger = LogManager.GetLogger("WaitForPrescriber");
                GatewayIp = gatewayIp;
                GatewayPort = gatewayPort;

                _waitForPrescriber = new Thread(WaitForPrescriberRecord);
                _waitForPrescription = new Thread(WaitForPrescriptionRecord);
                _waitForPatient = new Thread(WaitForPatientRecord);
                _waitForFacility = new Thread(WaitForFacilityRecord);
                _waitForStore = new Thread(WaitForStoreRecord);
                _waitForTq = new Thread(WaitForTqRecord);
                _waitForDrug = new Thread(WaitForDrugRecord);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to construct Pharmaserve object: {ex.Message}");
                throw;
            }
        }

        private void WaitForPrescriberRecord()
        {
            try
            {
                var p = new PollPrescriber(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPrescriberRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForPrescriptionRecord()
        {
            try
            {
                var p = new PollPatient(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPatientRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForPatientRecord()
        {
            try
            {
                var p = new PollPrescriber(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPrescriberRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForFacilityRecord()
        {
            try
            {
                var p = new PollFacility(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadFacilityRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForStoreRecord()
        {
            try
            {
                var p = new PollStore(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadStoreRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForTqRecord()
        {
            try
            {
                var p = new PollTQ(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadTQRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        public void WaitForDrugRecord()
        {
            try
            {
                var p = new PollDrug(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadDrugRecords();
                    Thread.Sleep(RefreshRate);
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }


    }
}