using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using NLog;
using MotCommonLib;

namespace TransformerPollingService
{
    public class MotSqlServerPollerBase<T> : IDisposable
    {
        protected long LastTouch { get; set; }
        protected string SqlView { get; set; }
        protected Dictionary<string, string> TranslationTable { get; set; }
        protected Dictionary<string, string> Lookup { get; set; }
        protected MotSqlServer Db { get; set; }
        protected readonly Mutex Mutex;
        protected readonly Logger EventLogger;
        protected string GatewayIp { get; set; }
        protected int GatewayPort { get; set; }
        private Type type { get; set; }

        protected MotSqlServerPollerBase(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort)
        {
            type = typeof(T);

            if (db == null || mutex == null)
            {
                throw new ArgumentNullException(type.Name);
            }

            Db = db;
            Mutex = mutex;

            TranslationTable = new Dictionary<string, string>();
            Lookup = new Dictionary<string, string>();
            EventLogger = LogManager.GetLogger(type.Name);

            try
            {
                var appSettings = ConfigurationManager.AppSettings;
                LastTouch = Convert.ToInt64(appSettings[type.Name]);
            }
            catch
            {
                EventLogger.Warn("Failed to get Last Touch Time, defaulting to .Now");
                LastTouch = 0L;
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
                appSettings[type.Name] = LastTouch.ToString();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

    }
}
