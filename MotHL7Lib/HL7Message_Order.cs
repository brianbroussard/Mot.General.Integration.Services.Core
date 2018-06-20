using System.Collections.Generic;

namespace MotHL7Lib
{
    /// <summary>
    /// Order
    /// </summary>
    public class Order // Triggered by a new ORC
    {
        public RXE RXE;
        public ORC ORC;
        public RXO RXO;
        public RXD RXD;
        public List<RXR> RXR;
        public List<NTE> NTE;
        public List<TQ1> TQ1;
        public List<RXC> RXC;
        public List<OBX> OBX;
        public List<PRT> PRT;
        public List<FT1> FT1;

        public Order()
        {
            RXR = new List<RXR>();
            NTE = new List<NTE>();
            TQ1 = new List<TQ1>();
            RXC = new List<RXC>();
            OBX = new List<OBX>();
            PRT = new List<PRT>();
            FT1 = new List<FT1>();
        }
        public bool Empty()
        {
            return ORC == null;
        }
    }
}