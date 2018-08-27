using System;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    public class PollDrug : MotSqlServerPollerBase<MotDrugRecord>
    {
        private MotDrugRecord _drug;

        public PollDrug(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _drug = new MotDrugRecord("Add");
        }

        public void ReadDrugRecords()
        {
            _drug.Clear();  
        }
    }
}
