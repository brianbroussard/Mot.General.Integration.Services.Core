using System;
using System.Configuration;
using Mot.Common.Interface.Lib;
using NLog;

namespace Mot.Polling.Interface.Lib
{
    public class MotTransformerPollingInterfaceBase
    {
        protected string DbUser { get; set; }
        protected string DbPassword { get; set; }
        protected string DbName { get; set; }
        protected string DbServer { get; set; }
        protected string DbServerType { get; set; }
        protected string DbServerIp { get; set; }
        protected string DbServerPort { get; set; }
        protected int RefreshRate { get; set; }
        protected string GatewayIp { get; set; }
        protected int GatewayPort { get; set; }

        //public PlatformOs Platform { get; }
        protected Logger EventLogger;

        public MotTransformerPollingInterfaceBase()
        {
            LoadConfiguration();
            EventLogger = LogManager.GetLogger("MotPoller");
            //Platform = GetPlatformOs.Go();
        }

        protected void LoadConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;

            DbUser = appSettings["DbUser"];
            DbPassword = appSettings["DbPassword"];
            DbServer = appSettings["DbServer"];
            DbServerType = appSettings["DbServerType"];
            DbName = appSettings["DbName"];
            DbServerIp = appSettings["DbServerIp"];
            DbServerPort = appSettings["DbServerPort"];
            RefreshRate = Convert.ToInt32(appSettings["RefreshRate"] ?? "60");

            GatewayIp = appSettings["GatewayIp"];
            GatewayPort = Convert.ToInt32(appSettings["GatewayPort"] ?? "24042");
        }

        protected void SaveConfiguration()
        {
            var appSettings = ConfigurationManager.AppSettings;
            appSettings["DbUser"] = DbUser;
            appSettings["DbPassword"] = DbPassword;
            appSettings["DbServer"] = DbServer;
            appSettings["DbServerType"] = DbServerType;
            appSettings["DbName"] = DbName;
            appSettings["DbServerIp"] = DbServerIp;
            appSettings["DbServerPort"] = DbServerPort;

            appSettings["GatewayIp"] = GatewayIp;
            appSettings["GatewayPort"] = GatewayPort.ToString();
        }
    }

    public class MotTransformerPollingInterface : MotTransformerPollingInterfaceBase
    {
        private  MotSqlServer _motDatabaseServer;
		private Pharmaserve _pharmaserve;
        private string _connectString;

        public void Start()
        {
            try
            {
                LoadConfiguration();
                // "Data Source=PROXYPLAYGROUND;Initial Catalog=McKessonTestDb;User ID=sa;Password=$MOT2018"

                _connectString = $"Data Source={DbServer};Initial Catalog={DbName};User ID={DbUser};Password={DbPassword};";
                _motDatabaseServer = new MotSqlServer(_connectString);

				_pharmaserve = new Pharmaserve(_motDatabaseServer, GatewayIp, GatewayPort);
				_pharmaserve.RefreshRate = RefreshRate;
				_pharmaserve.Go();
                
                EventLogger.Info("Service started");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                EventLogger.Error($"Failed to start service: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        ///     <c>StopListener</c>
        ///     The method called by Topshelf to halt the service
        /// </summary>
        public void Stop()
        {
			_pharmaserve.Stop();
            EventLogger.Info("Sevice stopped");
        }

        /// <summary>
        ///     <c>ShutDown</c>
        ///     Same as stop for now
        /// </summary>
        public void ShutDown()
        {
            Stop();
        }

        /// <summary>
        ///     <c>Restart</c>
        ///     Method called by Topshellf to restart the service
        /// </summary>
        public void Restart()
        {
			_pharmaserve.Dispose();
			_pharmaserve = new Pharmaserve(_motDatabaseServer, GatewayIp, GatewayPort);
			_pharmaserve.Go();

            EventLogger.Info("Service restarted");
        }
    }
}
