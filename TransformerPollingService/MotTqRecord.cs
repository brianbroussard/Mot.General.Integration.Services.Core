using System;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    public class PollTQ : MotSqlServerPollerBase<MotTimesQtysRecord>
    {
        private MotTimesQtysRecord _tq;

        public PollTQ(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _tq = new MotTimesQtysRecord("Add");
        }

        public void ReadTQRecords()
        {
            _tq.Clear();
        }
    }
}
