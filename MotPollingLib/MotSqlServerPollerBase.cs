using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Threading;
using Mot.Common.Interface.Lib;
using NLog;

namespace Mot.Polling.Interface.Lib
{
    public class MotSqlServerPollerBase<T> : IDisposable
    {
        protected string LastTouch { get; set; }
        protected string SqlView { get; set; }
        protected Dictionary<string, string> TranslationTable { get; set; }
        protected Dictionary<string, string> Lookup { get; set; }
        protected MotSqlServer Db { get; set; }
        protected readonly Mutex Mutex;
        protected readonly Logger EventLogger;
        protected string GatewayIp { get; set; }
        protected int GatewayPort { get; set; }
        public bool PreferAscii { get; set; }
        private Type type { get; set; }

        public string ByteArrayToHexString(byte[] b)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append("0x");

            foreach (byte val in b)
            {
                sb.Append(val.ToString("X2"));
            }

            return sb.ToString();
        }


        protected MotSqlServerPollerBase(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort)
        {
            type = typeof(T);

            if (db == null || mutex == null)
            {
                throw new ArgumentNullException(type.Name);
            }

            Db = db;
            Mutex = mutex;
            GatewayIp = gatewayIp;
            GatewayPort = gatewayPort;

            TranslationTable = new Dictionary<string, string>();
            Lookup = new Dictionary<string, string>();
            EventLogger = LogManager.GetLogger(type.Name);

            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                LastTouch = appSettings[type.Name].ToString();
            }
            catch
            {
                EventLogger.Warn("Failed to get Last Touch Time, defaulting to .Now");
                LastTouch = "0x0000000000000000";
            }
        }

        protected bool ValidTable(DataSet dataSet, string tableName = null)
        {

            if(tableName != null)
            {
                if (dataSet.Tables.Count > 0 && dataSet.Tables[tableName].Rows.Count > 0)
                {
                    return true;
                }
            }

            if(dataSet.Tables.Count > 0 && dataSet.Tables[0].Rows.Count > 0)
            {
                return true;
            }

            return false;
        }
        

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                var appSettings = ConfigurationManager.AppSettings;
                try
                {
                    var configFile = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                    var settings = configFile.AppSettings.Settings;

                    if (settings[type.Name] == null)
                    {
                        settings.Add(type.Name, LastTouch.ToString());
                    }
                    else
                    {
                        settings[type.Name].Value = LastTouch.ToString();
                    }

                    configFile.Save(ConfigurationSaveMode.Modified);
                    //ConfigurationManager.RefreshSection(configFile.AppSettings.SectionInformation.Name);
                }
                catch (Exception ex)
                {
                    EventLogger.Error($"Failed to save configuration: {ex.Message}");
                    throw;
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
