using System;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    public class PollPrescription : MotSqlServerPollerBase<MotPrescriptionRecord>
    {
        private MotPrescriptionRecord _scrip;

        public PollPrescription(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
            base(db, mutex, gatewayIp, gatewayPort)
        {
            _scrip = new MotPrescriptionRecord("Add");
        }

        public void ReadPrescriptionRecords()
        {
            _scrip.Clear();
        }
    }
}
