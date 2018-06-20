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

        public RDS_O13(XDocument __xdoc) : base()
        {
            string __last_significant_item = "NONE";

            Orders = new List<Order>();
            Header = new Header();
            Patient = new hl7Patient();

            Order __current_order = new Order();

            foreach (XElement __xe in __xdoc.Root.Elements())
            {
                switch (__xe.Name.ToString())
                {
                    case "AL1":
                        Patient.AL1.Add(new AL1(__xe));
                        break;

                    case "MSH":
                        Header.MSH = new MSH(__xe);
                        __last_significant_item = "MSH";
                        break;

                    case "OBX":
                        __current_order.OBX.Add(new OBX(__xe));
                        break;

                    case "PID":
                        Patient.PID = new PID(__xe);
                        __last_significant_item = "PID";
                        break;

                    case "PV1":
                        Patient.PV1 = new PV1(__xe);
                        __last_significant_item = "PV1";
                        break;

                    case "PV2":
                        Patient.PV2 = new PV2(__xe);
                        __last_significant_item = "PV2";
                        break;

                    case "PD1":
                        Patient.PD1 = new PD1(__xe);
                        break;

                    case "PRT":

                        if (__last_significant_item == "PID" || __last_significant_item.Contains("PV"))
                        {
                            Patient.PRT.Add(new PRT(__xe));
                        }
                        else
                        {
                            __current_order.PRT.Add(new PRT(__xe));
                        }

                        break;

                    case "IN1":
                        Patient.IN1 = new IN1(__xe);
                        break;

                    case "IN2":
                        Patient.IN2 = new IN2(__xe);
                        break;

                    case "TQ1":
                        __current_order.TQ1.Add(new TQ1(__xe));
                        break;

                    case "RXC":
                        __current_order.RXC.Add(new RXC(__xe));
                        break;

                    case "RXD":
                        __current_order.RXD = new RXD(__xe);
                        break;

                    case "RXE":
                        __current_order.RXE = new RXE(__xe);
                        __last_significant_item = "RXE";
                        break;

                    case "RXO":
                        __current_order.RXO = new RXO(__xe);
                        break;

                    case "RXR":
                        __current_order.RXR.Add(new RXR(__xe));
                        break;

                    case "ORC":  // Need to parse the order       
                        if (!__current_order.Empty()) // Is this a new order
                        {
                            Orders.Add(__current_order);
                            __current_order = null;
                        }

                        __current_order = new Order
                        {
                            ORC = new ORC(__xe)
                        };
                        __last_significant_item = "ORC";
                        break;

                    case "ZAS":
                        Header.ZAS = new ZAS(__xe);
                        break;

                    case "ZLB":
                        Header.ZLB = new ZLB(__xe);
                        break;

                    case "ZPI":
                        Header.ZPI = new ZPI(__xe);
                        break;

                    case "NTE":
                        if (__last_significant_item == "PID")
                        {
                            Patient.NTE.Add(new NTE(__xe));
                        }
                        else if (__last_significant_item == "MSH")
                        {
                            Header.NTE.Add(new NTE(__xe));
                        }
                        else
                        {
                            __current_order.NTE.Add(new NTE(__xe));
                        }
                        break;

                    case "DG1":
                        Patient.DG1.Add(new DG1(__xe));
                        break;

                    case "SFT":
                        Header.SFT.Add(new SFT(__xe));
                        break;

                    default:
                        break;
                }
            }

            if (__current_order != null)
            {
                Orders.Add(__current_order);
            }
        }

        public RDS_O13() : base()
        { }
    }
};
