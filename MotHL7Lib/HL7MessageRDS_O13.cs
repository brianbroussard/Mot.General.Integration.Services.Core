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

using System.Collections.Generic;
using System.Xml.Linq;

// ReSharper disable InconsistentNaming


namespace MotHL7Lib
{
    public class RDS_O13 : Hl7ElementBase   // Pharmacy/Treatment Dispense Message
    {
        // Dispense Msg      MSH, [ PID, [PV1] ], { ORC, [RXO], {RXE}, [{NTE}], {TQ1}, {RXR}, [{RXC}], RXD }, [ZPI] 

        public MSH MSH;
        public ZPI __zpi;
        public List<Order> Orders;
        public hl7Patient Patient;
        public Header Header;

        public RDS_O13(XDocument xdoc)
        {
            var __last_significant_item = "NONE";

            Orders = new List<Order>();
            Header = new Header();
            Patient = new hl7Patient();

            var currentOrder = new Order();

            if (xdoc.Root == null)
            {
                return;
            }

            foreach (var xe in xdoc.Root.Elements())
            {
                switch (xe.Name.ToString())
                {
                    case "AL1":
                        Patient.AL1.Add(new AL1(xe));
                        break;

                    case "MSH":
                        Header.MSH = new MSH(xe);
                        __last_significant_item = "MSH";
                        break;

                    case "OBX":
                        currentOrder.OBX.Add(new OBX(xe));
                        break;

                    case "PID":
                        Patient.PID = new PID(xe);
                        __last_significant_item = "PID";
                        break;

                    case "PV1":
                        Patient.PV1 = new PV1(xe);
                        __last_significant_item = "PV1";
                        break;

                    case "PV2":
                        Patient.PV2 = new PV2(xe);
                        __last_significant_item = "PV2";
                        break;

                    case "PD1":
                        Patient.PD1 = new PD1(xe);
                        break;

                    case "PRT":

                        if (__last_significant_item == "PID" || __last_significant_item.Contains("PV"))
                        {
                            Patient.PRT.Add(new PRT(xe));
                        }
                        else
                        {
                            currentOrder.PRT.Add(new PRT(xe));
                        }

                        break;

                    case "IN1":
                        Patient.IN1 = new IN1(xe);
                        break;

                    case "IN2":
                        Patient.IN2 = new IN2(xe);
                        break;

                    case "TQ1":
                        currentOrder.TQ1.Add(new TQ1(xe));
                        break;

                    case "RXC":
                        currentOrder.RXC.Add(new RXC(xe));
                        break;

                    case "RXD":
                        currentOrder.RXD = new RXD(xe);
                        break;

                    case "RXE":
                        currentOrder.RXE = new RXE(xe);
                        __last_significant_item = "RXE";
                        break;

                    case "RXO":
                        currentOrder.RXO = new RXO(xe);
                        break;

                    case "RXR":
                        currentOrder.RXR.Add(new RXR(xe));
                        break;

                    case "ORC": // Need to parse the order       
                        if (!currentOrder.Empty()) // Is this a new order
                        {
                            Orders.Add(currentOrder);
                            // ReSharper disable once RedundantAssignment
                            currentOrder = null;
                        }

                        currentOrder = new Order { ORC = new ORC(xe) };
                        __last_significant_item = "ORC";
                        break;

                    case "ZAS":
                        Header.ZAS = new ZAS(xe);
                        break;

                    case "ZLB":
                        Header.ZLB = new ZLB(xe);
                        break;

                    case "ZPI":
                        Header.ZPI = new ZPI(xe);
                        break;

                    case "NTE":
                        if (__last_significant_item == "PID")
                        {
                            Patient.NTE.Add(new NTE(xe));
                        }
                        else if (__last_significant_item == "MSH")
                        {
                            Header.NTE.Add(new NTE(xe));
                        }
                        else
                        {
                            currentOrder.NTE.Add(new NTE(xe));
                        }

                        break;

                    case "DG1":
                        Patient.DG1.Add(new DG1(xe));
                        break;

                    case "SFT":
                        Header.SFT.Add(new SFT(xe));
                        break;

                    // ReSharper disable once RedundantEmptySwitchSection
                    default:
                        break;
                }
            }

            Orders.Add(currentOrder);
        }

        public RDS_O13()
        { }
    }
};
