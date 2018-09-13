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
        protected string UserName { get; set; }
        protected string Password { get; set; }
        private bool IsNewUserName { get;set; }
        private bool IsNewPassword { get; set; }

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
            try
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

                //
                // If there is a username & password and its the first use, encode them and put
                // them back into the config file.
                //
                if (UserName != "None" && Password != "None")
                {
                    if (!MotAccessSecurity.IsEncoded(UserName))
                    {
                        IsNewUserName = true;
                    }
                    else
                    {
                        UserName = MotAccessSecurity.DecodeString(appSettings["UserName"]);
                    }

                    if (!MotAccessSecurity.IsEncoded(Password))
                    {
                        IsNewPassword = true;
                    }
                    else
                    {
                        Password = MotAccessSecurity.DecodeString(appSettings["Password"]);
                    }
                }
            }
            catch (Exception ex)
            {
                EventLogger.Error($"Error saving configuration file: {ex.Message}");
            }
        }

        protected void SaveConfiguration()
        {
            try
            {
                var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                var settings = configFile.AppSettings.Settings;

                settings["DbUser"].Value = DbUser;
                settings["DbPassword"].Value = DbPassword;
                settings["DbServer"].Value = DbServer;
                settings["DbServerType"].Value = DbServerType;
                settings["DbName"].Value = DbName;
                settings["DbServerIp"].Value = DbServerIp;
                settings["DbServerPort"].Value = DbServerPort;

                settings["GatewayIp"].Value = GatewayIp;
                settings["GatewayPort"].Value = GatewayPort.ToString();

                if (IsNewUserName)
                {
                    settings["UserName"].Value = MotAccessSecurity.EncodeString(UserName);
                }

                if (IsNewPassword)
                {
                    settings["Password"].Value = MotAccessSecurity.EncodeString(Password);
                }

                configFile.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
            }
            catch(Exception ex)
            {
                EventLogger.Error($"Error saving configuration file: {ex.Message}");
            }
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
