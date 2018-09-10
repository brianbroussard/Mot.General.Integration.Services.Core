﻿using System;
using System.Threading;
using NLog;

using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
{
    public class Pharmaserve : IDisposable
    {
        public int RefreshRate { get; set; }

        /*
        private Thread _waitForDrug;
        private Thread _waitForFacility;
        private Thread _waitForPatient;
        private Thread _waitForPrescriber;
        private Thread _waitForPrescription;
        private Thread _waitForStore;
        private Thread _waitForTq;
        */

        private MotSqlServer MotSqlServer { get; set; }
        private readonly Mutex _mutex;

        private string GatewayIp { get; set; }
        private int GatewayPort { get; set; }

        private Logger EventLogger { get; set; }

        private volatile bool KeepRunning = true;

        public Pharmaserve(MotSqlServer motSqlServer, string gatewayIp, int gatewayPort)
        {
            try
            {
                this.MotSqlServer = motSqlServer;
                _mutex = new Mutex();
                EventLogger = LogManager.GetLogger("PharmaserveSql");
                GatewayIp = gatewayIp;
                GatewayPort = gatewayPort;
                               
                var _waitForPrescriber = new Thread(() => WaitForPrescriberRecord());
                var _waitForPrescription = new Thread(() => WaitForPrescriptionRecord());
                var _waitForPatient = new Thread(() => WaitForPatientRecord());
                var _waitForFacility = new Thread(() => WaitForFacilityRecord());
                var _waitForStore = new Thread(() => WaitForStoreRecord());
                var _waitForTq = new Thread(() => WaitForTqRecord());
                var _waitForDrug = new Thread(() => WaitForDrugRecord());
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to construct Pharmaserve object: {ex.Message}");
                throw;
            }
        }

        private void WaitForPrescriberRecord()
        {
            Thread.CurrentThread.Name = "Prescriber";

            try
            {
                var p = new PollPrescriber(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPrescriberRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Prescriber exiting");

            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForPrescriptionRecord()
        {
            Thread.CurrentThread.Name = "Prescription";

            try
            {
                var p = new PollPatient(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPatientRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Prescription exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForPatientRecord()
        {
            Thread.CurrentThread.Name = "Patient";

            try
            {
                var p = new PollPrescriber(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadPrescriberRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Prescriber exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForFacilityRecord()
        {
            Thread.CurrentThread.Name = "Facility";

            try
            {
                var p = new PollFacility(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadFacilityRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Facility exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForStoreRecord()
        {
            Thread.CurrentThread.Name = "Store"; 

            try
            {
                var p = new PollStore(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadStoreRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Store exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        private void WaitForTqRecord()
        {
            Thread.CurrentThread.Name = "TQ";

            try
            {
                var p = new PollTQ(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadTQRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("TQ exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        public void WaitForDrugRecord()
        {
            Thread.CurrentThread.Name = "Drugs";

            try
            {
                var p = new PollDrug(MotSqlServer, _mutex, GatewayIp, GatewayPort);

                while (KeepRunning)
                {
                    p.ReadDrugRecords();
                    Thread.Sleep(RefreshRate);
                }

                Console.WriteLine("Drug exiting");
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Failed in Prescriber {ex.Message}");
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // Shutdown all running threads
                KeepRunning = false;
                Thread.Sleep(RefreshRate * 2);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}