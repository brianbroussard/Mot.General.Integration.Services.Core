using System;
using System.Collections.Generic;
using System.Text;

namespace MotHL7Lib
{
    /// <summary>
    /// <c>HL7Exception</c>
    /// An enriched exception that contains additional HL7 meta data and error valuse
    /// </summary>
    [Serializable]
    public class HL7Exception : Exception
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public HL7Exception()
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public HL7Exception(int code, string message) : base(message)
        { }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        /// <param name="inner"></param>
        public HL7Exception(int code, string message, Exception inner) : base(message, inner)
        { }
    }
    public class HL7Base
    {
        public class UpdateStatusArgs : EventArgs
        {
            public string EventMessage { get; set; }
            public string MshIn { get; set; }
            public string MshOut { get; set; }
            public string Timestamp { get; set; }
        }

        public delegate void UpdateUIEventHandler(object sender, UpdateStatusArgs args);
        public delegate void UpdateUIErrorHandler(object sender, UpdateStatusArgs args);
    }
}
