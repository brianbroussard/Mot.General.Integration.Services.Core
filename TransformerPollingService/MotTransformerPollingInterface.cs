using System;
using System.Collections.Generic;
using System.Configuration;
using NLog;

using MotCommonLib;

namespace TransformerPollingService
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

        //public PlatformOs Platform { get; }
        protected Logger EventLogger;

        public MotTransformerPollingInterfaceBase()
        {
            LoadConfiguration();
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
        }
    }

    public class MotTransformerPollingInterface : MotTransformerPollingInterfaceBase
    {
        private  MotSqlServer _motDatabaseServer;
        private string _connectString;

        public void Start()
        {
            try
            {
                _connectString = $"Data Source={DbServerIp},{DbServerPort};Network Library = DBMSSOCN;Initial Catalog={DbName};User Id={DbUser};Password={DbPassword};";
                _motDatabaseServer = new MotSqlServer(_connectString);

                LoadConfiguration();
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
            EventLogger.Info("Service restarted");
        }
    }
}
