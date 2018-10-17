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

        //
        // McKesson doesn't have a notion of TQ in their SQL interface, so this is a placeholder
        //
        public void ReadTQRecords()
        {
            _tq.Clear();
            return;
        }
    }
}