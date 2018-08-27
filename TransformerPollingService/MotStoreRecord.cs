using System;
using System.Threading;
using MotCommonLib;
    
namespace TransformerPollingService
{
    public class PollStore : MotSqlServerPollerBase<MotStoreRecord>
    {
        private MotStoreRecord _store;

        public PollStore(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _store = new MotStoreRecord("Add");
        }

        public void ReadStoreRecords()
        {
            _store.Clear();
        }
    }
}
