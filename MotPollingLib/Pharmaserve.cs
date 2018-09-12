using System;
using System.Threading;
using NLog;

using Mot.Common.Interface.Lib;
using System.Data;

namespace Mot.Polling.Interface.Lib
{
	public class Pharmaserve : IDisposable
	{
		public int RefreshRate { get; set; }

		private MotSqlServer MotSqlServer { get; set; }
		private readonly Mutex _mutex;

		private string GatewayIp { get; set; }
		private int GatewayPort { get; set; }

		private Logger EventLogger { get; set; }

		private volatile bool KeepRunning = true;
		Thread _waitForPrescriber;
		Thread _waitForPrescription;
		Thread _waitForPatient;
		Thread _waitForFacility;
		Thread _waitForStore;
		Thread _waitForTq;
		Thread _waitForDrug;

		public void Go()
		{
			try
			{
				KeepRunning = true;

				_waitForPrescriber = new Thread(WaitForPrescriberRecord);
				_waitForPrescriber.Start();

				_waitForPrescription = new Thread(WaitForPrescriptionRecord);
				_waitForPrescription.Start();

				_waitForPatient = new Thread(WaitForPatientRecord);
				_waitForPatient.Start();

				_waitForFacility = new Thread(WaitForFacilityRecord);
				_waitForFacility.Start();

				_waitForStore = new Thread(WaitForStoreRecord);
				_waitForStore.Start();

				_waitForTq = new Thread(WaitForTqRecord);
				_waitForTq.Start();

				_waitForDrug = new Thread(WaitForDrugRecord);
				_waitForDrug.Start();
			}
			catch (Exception ex)
			{
				EventLogger.Error($"Failed starting monitor threads: {ex.Message}");
				throw;
			}
		}

		public void Stop()
		{
			KeepRunning = false;
		}

		public Pharmaserve(MotSqlServer motSqlServer, string gatewayIp, int gatewayPort)
		{
			try
			{
				MotSqlServer = motSqlServer;
				_mutex = new Mutex();
				EventLogger = LogManager.GetLogger("PharmaserveSql");
				GatewayIp = gatewayIp;
				GatewayPort = gatewayPort;
			}
			catch (Exception ex)
			{
				EventLogger.Error($"Failed to construct Pharmaserve object: {ex.Message}");
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
					try
					{
						p.ReadPrescriberRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}

					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Prescriber thread exiting");
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
					try
					{
						p.ReadPatientRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}

					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Prescription thread exiting");
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
					try
					{
						p.ReadPrescriberRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}

					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Prescriber thread exiting");
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
					try
					{
						p.ReadFacilityRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}

					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Facility thread exiting");
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
					try
					{
						p.ReadStoreRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}

					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Store thread exiting");
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
					try
					{
						p.ReadTQRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}


					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("TQ thread exiting");
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
					try
					{
						p.ReadDrugRecords();
					}
					catch (RowNotInTableException)
					{
						;
					}


					Thread.Sleep(RefreshRate);
				}

				Console.WriteLine("Drug thread exiting");
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