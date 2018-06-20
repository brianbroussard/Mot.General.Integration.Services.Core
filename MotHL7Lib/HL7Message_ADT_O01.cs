// MIT license
//
// Copyright (c) 2016 by Peter H. Jenney and Medicine-On-Time, LLC.
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 

using System;
using System.Collections.Generic;
using System.Xml.Linq;

// ReSharper disable InconsistentNaming


namespace MotHL7Lib

{
    //
    // Messages - Specific to Softwriters FrameworkLTE
    //
    /// <summary>
    /// ADT Messages for patient management
    /// </summary>
    public class ADT_A01 : Hl7ElementBase   // Register (A04), Update (A08) a Patient
    {
        // A04 - MSH, EVN, PID, PV1, [{OBX}], [{AL1}], [{DG1}]
        // A08 - MSH, EVN, PID, PV1, [{OBX}], [{AL1}], [{DG1}]

        // Note that IN1 seems to show up with ADT_A04

        public MSH MSH;
        public EVN EVN;
        public PID PID;
        public PV1 PV1;
        public PV2 PV2;
        public UAC UAC;
        public PD1 PD1;

        public GT1 GT1;
        public List<OBX> OBX;
        public List<AL1> AL1;
        public List<ROL> ROL;
        public List<DG1> DG1;
        public List<SFT> SFT;
        public List<IN1> IN1;
        public List<IN2> IN2;
        public List<NK1> NK1;
        public List<DB1> DB1;

        private List<Dictionary<string, string>> MessageStore;

        public ADT_A01(XDocument xmlDoc, List<Dictionary<string, string>> messageStore = null) : base()
        {
            if (xmlDoc == null)
            {
                throw new ArgumentNullException(nameof(xmlDoc));
            }

            MessageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));

            OBX = new List<OBX>();
            AL1 = new List<AL1>();
            DG1 = new List<DG1>();
            ROL = new List<ROL>();
            DB1 = new List<DB1>();
            SFT = new List<SFT>();
            IN1 = new List<IN1>();
            IN2 = new List<IN2>();
            NK1 = new List<NK1>();

            if (xmlDoc.Root == null)
            {
                return;
            }

            foreach (var xElement in xmlDoc.Root.Elements())
            {
                if (xElement != null)
                {
                    switch (xElement.Name.ToString())
                    {
                        case "AL1":
                            AL1.Add(new AL1(xElement));
                            break;

                        case "SFT":
                            SFT.Add(new SFT(xElement));
                            break;

                        case "UAC":
                            UAC = new UAC(xElement);
                            break;

                        case "EVN":
                            EVN = new EVN(xElement);
                            break;

                        case "DG1":
                            DG1.Add(new DG1(xElement));
                            break;

                        case "DB1":
                            DB1.Add(new DB1(xElement));
                            break;

                        case "GT1":
                            GT1 = new GT1(xElement);
                            break;

                        case "IN1":
                            IN1.Add(new IN1(xElement));
                            break;

                        case "IN2":
                            IN2.Add(new IN2(xElement));
                            break;

                        case "MSH":
                            MSH = new MSH(xElement);
                            break;

                        case "NK1":
                            NK1.Add(new NK1(xElement));
                            break;

                        case "OBX":
                            OBX.Add(new OBX(xElement));
                            break;

                        case "PID":
                            PID = new PID(xElement);
                            break;

                        case "PD1":
                            PD1 = new PD1(xElement);
                            break;

                        case "PV1":
                            PV1 = new PV1(xElement);
                            break;

                        case "PV2":
                            PV2 = new PV2(xElement);
                            break;

                        case "ROL":
                            ROL.Add(new ROL(xElement));
                            break;

                        default:
                            break;
                    }
                }
            }
        }

        public ADT_A01(List<Dictionary<string, string>> messageStore) : base()
        {
            MessageStore = messageStore ?? throw new ArgumentNullException(nameof(messageStore));
        }
    }
};
