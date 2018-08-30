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


namespace Mot.HL7.Interface.Lib
{
    public class OMP_O09 : Hl7ElementBase   // Pharmacy Treatment Order Message
    {
        // Control Code NW (New)            MSH, PID, [PV1], { ORC, [{TQ1}], [{RXR}], RXO, [{RXC}] }, [{NTE}]
        // Control Code DC (Discontinue)    MSH, PID, [PV1], { ORC, [{TQ1}], RXO, [{RXC}] }
        // Control Code RF (Refill)         MSH, PID, [PV1], { ORC, [{TQ1}], RXO, [{RXC}] }

        //public MSH MSH;
        public List<Order> Orders;
        public hl7Patient Patient { get; set; }
        public Header Header1 { get; set; }

        public OMP_O09(XDocument xmlDoc)
        {
            var lastSignificantItem = "NONE";

            Header1 = new Header();
            Orders = new List<Order>();
            Patient = new hl7Patient();
            var currentOrder = new Order();

            Hl7XmLset = xmlDoc ?? throw new ArgumentNullException(nameof(xmlDoc));

            if (xmlDoc.Root != null)
            {
                foreach (var xElement in xmlDoc.Root.Elements())
                {
                    switch (xElement.Name.ToString())
                    {
                        case "AL1":
                            Patient.AL1.Add(new AL1(xElement));
                            break;

                        case "MSH":
                            Header1.MSH = new MSH(xElement);
                            lastSignificantItem = "MSH";
                            break;

                        case "OBX":
                            currentOrder.OBX.Add(new OBX(xElement));
                            break;

                        case "PID":
                            Patient.PID = new PID(xElement);
                            lastSignificantItem = "PID";
                            break;

                        case "PV1":
                            Patient.PV1 = new PV1(xElement);
                            lastSignificantItem = "PV1";
                            break;

                        case "PV2":
                            Patient.PV2 = new PV2(xElement);
                            lastSignificantItem = "PV2";
                            break;

                        case "PD1":
                            Patient.PD1 = new PD1(xElement);
                            break;

                        case "IN1":
                            Patient.IN1 = new IN1(xElement);
                            break;

                        case "IN2":
                            Patient.IN2 = new IN2(xElement);
                            break;

                        case "TQ1":
                            currentOrder.TQ1.Add(new TQ1(xElement));
                            break;

                        case "RXC":
                            currentOrder.RXC.Add(new RXC(xElement));
                            break;

                        case "RXE":
                            currentOrder.RXE = new RXE(xElement);
                            lastSignificantItem = "RXE";
                            break;

                        case "RXO":
                            currentOrder.RXO = new RXO(xElement);
                            break;

                        case "RXR":
                            currentOrder.RXR.Add(new RXR(xElement));
                            break;

                        case "ORC": // Need to parse the order       
                            if (!currentOrder.Empty()) // Is this a new order
                            {
                                Orders.Add(currentOrder);
                                // ReSharper disable once RedundantAssignment
                                currentOrder = null;
                            }

                            currentOrder = new Order
                            {
                                ORC = new ORC(xElement)
                            };
                            lastSignificantItem = "ORC";
                            break;

                        case "PRT":

                            if (lastSignificantItem == "PID" || lastSignificantItem.Contains("PV"))
                            {
                                Patient.PRT.Add(new PRT(xElement));
                            }
                            else
                            {
                                currentOrder.PRT.Add(new PRT(xElement));
                            }

                            break;

                        case "ZAS":
                            Header1.ZAS = new ZAS(xElement);
                            break;

                        case "ZLB":
                            Header1.ZLB = new ZLB(xElement);
                            break;

                        case "ZPI":
                            Header1.ZPI = new ZPI(xElement);
                            break;

                        case "NTE":
                            if (lastSignificantItem == "PID")
                            {
                                Patient.NTE.Add(new NTE(xElement));
                            }
                            else if (lastSignificantItem == "MSH")
                            {
                                Header1.NTE.Add(new NTE(xElement));
                            }
                            else
                            {
                                currentOrder.NTE.Add(new NTE(xElement));
                            }

                            break;

                        case "DG1":
                            Patient.DG1.Add(new DG1(xElement));
                            break;

                        case "SFT":
                            Header1.SFT.Add(new SFT(xElement));
                            break;

                        // ReSharper disable once RedundantEmptySwitchSection
                        default:
                            break;
                    }
                }
            }

            Orders.Add(currentOrder);
        }
    }
};
