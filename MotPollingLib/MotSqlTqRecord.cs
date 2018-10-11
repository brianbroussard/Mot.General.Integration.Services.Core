using System.Threading;
using Mot.Common.Interface.Lib;

namespace Mot.Polling.Interface.Lib
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
            _tq._preferAscii = PreferAscii;

            return;
        }
    }
}