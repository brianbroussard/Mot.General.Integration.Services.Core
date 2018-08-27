using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Net.Sockets;
using System.Threading;
using MotCommonLib;

namespace TransformerPollingService
{
    public class PollFacility : MotSqlServerPollerBase<MotFacilityRecord>
    {
        private MotFacilityRecord _facility;
        
        public PollFacility(MotSqlServer db, Mutex mutex, string gatewayIp, int gatewayPort) :
               base(db, mutex, gatewayIp, gatewayPort)
        {
            _facility = new MotFacilityRecord("Add");
        }
        
        public void ReadFacilityRecords()
        {
            _facility.Clear();
        }
    }
}
