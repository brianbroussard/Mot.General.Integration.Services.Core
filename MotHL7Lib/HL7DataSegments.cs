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

#pragma warning disable CS1570 // XML comment has badly formed XML
    /// <summary>
    /// HL7 - Grammer:
    /// 
    ///         <CR> - Segment Terminator
    ///         |    - Field Separator
    ///         ^    - Component Separator
    ///         &    - Subcompuonent Separator
    ///         ~    - Repetition Seperator
    ///         \    - Escape Character
    ///         
    ///         []   - Optional  )
    ///         {}   - 1 or more repetitions
    ///         
    ///         OPT (Optionality Keys)
    ///             R - Required
    ///             O - Optional
    ///             C - Conditional on Trigger
    ///             X - Not used with this trigger
    ///             B - Left in for backward compatability
    ///             W - Withdrawn
    ///             
    /// </summary>

    /// <summary>
    /// Messsages to support for FrameworkLTE
    /// 
    ///     ADT^A04, ADT_A01
    ///     ADT^A08, ADT_A01
    ///     
    ///     OMP^O09, OMP_O09
    ///     ADT^A23, ADT_A21
    ///     
    ///     RDE^011, RDE_O11
    ///     
    ///         v2.7.1 Spec
    ///         Header:     MSH, [{SFT}], [UAC], [{NTE}], 
    ///         Patient:        [PID, [PD1], [{PRT}],[{NTE}], 
    ///         Visit:              [PV1, [PV2], [{PRT}]],
    ///         Insurance:          [IN1, [IN2], [IN3]],
    ///         Misc:               [GT1], [{AL1}]],
    ///         Order:          [{ORC,
    ///         Timing:             [{TQ1, [{TQ2}]}],
    ///         Detail:             [RXO, [{NTE}], {RXR},
    ///         Component:              [{RXC, [{NTE}]}],
    ///                             [{PRT}], RXE, [{PRT}],[{NTE}],
    ///         Timing Encoded:         [{TQ1, [{TQ2}]}],
    ///                             {RXR}, [{RXC}],
    ///         Observation:            [{OBX, [{PRT}],[{NTE}]}],
    ///                             [{FT1}], [BLG], [{CTI}] }]
    ///         
    ///     FrameworkLTC Spec
    ///     -----------------
    ///     RDE^O11^RDE_O11  - DO(Drug Order)
    ///         Header:     MSH, 
    ///         Patient:        [PID,[PV1]],
    ///         Order:          {ORC,[RXO,{RXR}],RXE,[{NTE},{TQ1},{RXR},[{RXC]},
    ///         Custom:         [ZPI]
    ///
    ///     RDE^O11^RDE_O11  - LO(Literal Order)
    ///         Header:     MSH, 
    ///         Patient:        PID, [PV1],
    ///         Order:          ORC,[TQ1],[RXE],
    ///         Custom:         [ZAS]
    ///        
    ///     -- RDE Special Interpretations
    ///         PID-2:          3rd Party Patient ID [CX]
    ///         PID-3:          FW Patient ID - FacilityID\F\PatientID
    ///         PID-4:          Alternate Patient ID
    /// 
    ///     EPIC Spec
    ///     ----------
    ///     RDE^O11
    ///         Header:     MSH,PID,CON,
    ///         Order:          {ORC,[RXE,RXD,ZLB,PRT,OBX]}
    ///                    
    ///     Response:       ORR,MSH,MSA,[ERR]
    ///     
    ///     --- RDE Special Interpretations
    ///         PID-2:       CX :: Backward Compatability Only
    ///         PID-3:       ID^^^SA^assigning athority-ID2^^^SA2^Assigning Athourity
    ///            
    ///     RDS^O13, RDS_O13
    ///     
    /// 
    /// </summary>

    /// <summary>
    /// Messages to support for EPIC
    /// </summary>
#pragma warning restore CS1570 // XML comment has badly formed XML


    public class HL7MessageParser

    {
        private bool SubParse(string tagName, string message, char delimiter, Dictionary<string, string> messageData, int minorId)
        {
            char[] localDelimiters = { '\\', '&', '~', '^' };
            string[] fieldNames = message.Split(delimiter);

            minorId = 1;

            foreach (var field in fieldNames)
            {
                int gotOne = field.IndexOfAny(localDelimiters);

                if (gotOne > 0)
                {
                    string __tmp_tagname = tagName;

                    tagName = tagName + "." + minorId.ToString();
                    SubParse(tagName, field, field[gotOne], messageData, minorId); // recurse

                    tagName = __tmp_tagname;

                    continue;
                }

                messageData.Add(tagName + "." + minorId.ToString(), field.TrimStart(' ').TrimEnd(' '));
                minorId++;
            }

            return true;
        }
        public void Parse(string message, Dictionary<string, string> messageData)
        {
            char[] delimiters = { '|' };
            char[] localDelimiters = { '\\', '&', '~', '^' };

            int major = 0;
            int minor = 1;
            int gotOne;

            string[] fieldNames = message.Split(delimiters);
            string tagName = fieldNames[0].TrimStart(' ').TrimEnd(' ');
            int i = 0;

            while (i < fieldNames.Length)
            {
                // Trim out all the whitespace
                fieldNames[i] = fieldNames[i].TrimStart(' ').TrimEnd(' ');
                string fieldName = fieldNames[i++];

                // Catch the Root Name  {RXE,RXE}
                if (major == 0)
                {
                    messageData.Add(tagName, fieldName);
                    major++;
                    continue;
                }

                // Doah!  MSH requires something special
                if (tagName == "MSH" && major < 3)
                {
                    if (major < 3)
                    {
                        messageData.Add("MSH.1", "|");
                        messageData.Add("MSH.2", @"^~\&");
                        major = 3;
                        continue;
                    }
                }

                messageData.Add(tagName + "." + major.ToString(), fieldName);

                if (fieldName != @"^~\&")
                {
                    gotOne = fieldName.IndexOfAny(localDelimiters);    // Subfield ADT01^RDE^RDE_011
                    if (gotOne != -1)
                    {
                        if (fieldName.IndexOf('~') > 0)
                        {
                            char[] __tilde = { '~' };
                            string[] tildeSplit = fieldName.Split(__tilde);
                            var tmpFieldName = fieldNames[0];

                            foreach (var split in tildeSplit)
                            {
                                tagName = tagName + "." + major.ToString();
                                SubParse(tagName, split, split[fieldName.IndexOfAny(localDelimiters)], messageData, minor);
                                tagName = "~" + tmpFieldName;
                                tmpFieldName = tagName;  // Add '~', 1 for each iteration.  We'll decode them later
                            }
                        }
                        else
                        {
                            tagName = tagName + "." + major.ToString();

                            // Wait for it so we don't loose the overall seque
                            bool __success = SubParse(tagName, fieldName, fieldName[gotOne], messageData, minor);

                            // Reset the tagname to the root level
                            tagName = fieldNames[0];
                        }
                    }
                }

                major++;
            }
        }
        public HL7MessageParser() { }
    }
    /*
        RDE^O11^RDE_O11  - DO(Drug Order)
            MSH, [PID,[PV1],{ORC,[RXO, RXR], RXE, [{NTE}], {TQ1}, RXR, [{RXC]} ]}

        RDE^O11^RDE_O11  - LO(Literal Order)
            MSH, PID, [PV1],ORC,[TQ1],[RXE]
    */

    // RDE_O11 Order - {ORC,[RXO,{RXR}],RXE,[{NTE}],{TQ1},{RXR},[{RXC]}]}
    public class hl7Patient
    {
        public PID PID;
        public PD1 PD1;
        public List<PRT> PRT;
        public List<NTE> NTE;
        public List<AL1> AL1;
        public List<DG1> DG1;
        public List<CX1> CX1;
        public List<OBX> OBX;
        public PV1 PV1;
        public PV2 PV2;
        public IN1 IN1;
        public IN2 IN2;

        public hl7Patient()
        {
            try
            {
                PRT = new List<PRT>();
                NTE = new List<NTE>();
                AL1 = new List<AL1>();
                DG1 = new List<DG1>();
                CX1 = new List<CX1>();
                OBX = new List<OBX>();
            }
            catch
            { throw; }
        }

        public bool Empty()
        {
            /*
            if (__pid.__msg_data.Count == 0 &&
                __pd1.__msg_data.Count == 0 &&
                __pv1.__msg_data.Count == 0 &&
                __pv2.__msg_data.Count == 0 &&
                __in1.__msg_data.Count == 0 &&
                __in2.__msg_data.Count == 0 &&
                __dg1.Count == 0 &&
                __prt.Count == 0 &&
                NTE.Count == 0 &&
                __al1.Count == 0)
            {
                return true;
            }
            */
            return false;

        }
    }
    public class Header
    {
        public MSH MSH;
        public List<SFT> SFT;
        public UAC UAC;
        public List<NTE> NTE;

        // Hangers on
        public ZAS ZAS;
        public ZLB ZLB;
        public ZPI ZPI;

        public Header()
        {
            try
            {
                SFT = new List<SFT>();
                NTE = new List<NTE>();
            }
            catch
            { throw; }
        }
    }

    //
    //  Segments - These all parse as SEG-#,-#... and stored as a Dictioary<string,string>
    //
    public class FT1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        public void __load()
        {
        }

        public FT1(XElement xElement) : base(xElement)
        {

        }

        public FT1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class AL1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        public void __load()
        {
        }
        public AL1(XElement xElement) : base(xElement)
        {
        }
        public AL1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class ERR : Hl7ElementBase
    {
        // McKesson Pharmaserve classifications
        //ERR  	1	Error Code and Location
        //ERR	2	Error Location
        //ERR	3	HL7 Error Code
        //ERR	4	Severity
        //ERR	5	Application Error Code
        //ERR	6	Application Error Parameter
        //ERR	7	Diagnostic Information
        //ERR	8	User Message
        //ERR	9	Inform Person Indicator
        //ERR	10	Override Type
        //ERR	11	Override Reason Code
        //ERR 	12	Help Desk Contact Point

        public string __error_code_and_location { get; set; }
        public string __error_location { get; set; }
        public string __hl7_error_code { get; set; }
        public string __severity { get; set; }
        public string __application_error_code { get; set; }
        public string __application_error_param { get; set; }
        public string __user_mesage { get; set; }
        public string __inform_person_id { get; set; }
        public string __override_tape { get; set; }
        public string __override_reason { get; set; }
        public string ____help_desk_contact { get; set; }


        private void __load()
        {
        }
        public ERR(XElement xElement) : base(xElement)
        {

        }
        public ERR() : base()
        {
            __load();
        }
    }
    public class EVN : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public EVN(XElement xElement) : base(xElement)
        {

        }
        public EVN() : base()
        {
            __load();
        }

        public EVN(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class DB1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public DB1() : base()
        {
            __load();
        }
        public DB1(XElement xElement) : base(xElement)
        {
        }
    }
    public class DG1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public DG1() : base()
        {
            __load();
        }
        public DG1(XElement xElement) : base(xElement)
        {
        }
        public DG1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class CX1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public CX1() : base()
        {
            __load();
        }
        public CX1(XElement xElement) : base(xElement)
        {
        }
        public CX1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class GT1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public GT1() : base()
        {
            __load();
        }
        public GT1(XElement xElement) : base(xElement)
        {

        }
        public GT1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class IIM : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public IIM() : base()
        {
            __load();
        }
        public IIM(XElement xElement) : base(xElement)
        {
        }
        public IIM(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class IN1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public IN1() : base()
        {
            __load();
        }
        public IN1(XElement xElement) : base(xElement)
        {

        }
        public IN1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class IN2 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public IN2() : base()
        {
            __load();
        }
        public IN2(XElement xElement) : base(xElement)
        {
        }
        public IN2(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class MSA : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public MSA() : base()
        {
            __load();
        }
        public MSA(XElement xElement) : base(xElement)
        {
        }

        public MSA(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class MSH : Hl7ElementBase
    {
        HL7MessageParser Parser = new HL7MessageParser();
        bool listParsed = false;

        public MSH() : base()
        { }
        public MSH(XElement xElement) : base(xElement)
        {
            listParsed = false;
            //__data = new XDocument(xElement);
        }
        public MSH(string message)
        {
            Parser.Parse(message, MessageData);
            listParsed = true;

            //var HL7xml = new HL7toXDocumentParser.Parser();
            //var xDoc = HL7xml.Parse(message);

            //
            // Wierdness - In MSH-9 there can be 2 or 3 items for example |ADT^AO1| or |RDE^O11^RDE_O11|. It looks like if there
            //             is no MSH-9-3, 9-1 and 9-2 need to be commbined to make 9-3. So, the strategy should be to combine 1 & 2 into 3 
            //             and if there's a 3, it will overwrite it.
            var tmp = string.Empty;

            if (!MessageData.TryGetValue("MSH.9.3", out tmp))
            {
                MessageData.Add("MSH.9.3", MessageData["MSH.9.1"] + "_" + MessageData["MSH.9.2"]);
            }

            //else
            //{
            //    MessageData.Add("MSH.9.3", "MALFORMED MESSAGE");
            //}
        }

        public string Get(string key)
        {
            if (listParsed)
            {
                string tmp = string.Empty;
                MessageData.TryGetValue(key, out tmp);
                return tmp;
            }

            return base.Get(key);
        }

        public string FullMessage()
        {
            //string __out = @"MSH|^~\&||MOT_HL7Gateway|MOT_HL7Gateway|";
            string wholeThing = string.Empty;

            if (MessageData != null)
            {
                var enumerator = MessageData.GetEnumerator();

                while (true)
                {
                    try
                    {
                        enumerator.MoveNext();
                        wholeThing += enumerator.Current.Value + "|";
                    }
                    catch
                    { break; }
                }
            }

            return wholeThing;
        }

        public string AckString()
        {
            return null;
        }

        public string NakString()
        {
            return null;
        }
    }
    public class NK1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public NK1() : base()
        {
            __load();
        }
        public NK1(XElement xElement) : base(xElement)
        {

        }
        public NK1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class NTE : Hl7ElementBase
    {
        // public Dictionary<string, DataPair> __field_names = new Dictionary<string, DataPair>()
        //public Dictionary<string, string> __msg_data = new Dictionary<string, string>();
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public NTE(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
        public NTE(XElement xElement) : base(xElement)
        {

        }
        public NTE() : base()
        {
            __load();
        }
    }
    public class OBX : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public OBX() : base()
        {
            __load();
        }
        public OBX(XElement xElement) : base(xElement)
        {
        }
        public OBX(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class ORC : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public ORC() : base()
        {
            __load();
        }
        public ORC(XElement xElement) : base(xElement)
        {
        }
        public ORC(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PID : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public PID()
        {
            __load();
        }

        public PID(XElement xElement) : base(xElement)
        {
        }
        public PID(string __message)
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PR1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public PR1() : base()
        {
            __load();
        }
        public PR1(XElement xElement) : base(xElement)
        {
        }
        public PR1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PV1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {

        }
        public PV1() : base()
        {
            __load();
        }
        public PV1(XElement xElement) : base(xElement)
        {
        }
        public PV1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PV2 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()  // TODO:  Finish Patient Visit Fields
        {

        }
        public PV2() : base()
        {
            __load();
        }
        public PV2(XElement xElement) : base(xElement)
        {
        }
        public PV2(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PRT : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()  // TODO:  Finish Patient Visit Fields
        {

        }
        public PRT() : base()
        {
            __load();
        }
        public PRT(XElement xElement) : base(xElement)
        {
        }
        public PRT(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class SFT : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        // TODO:  Finish Field Names
        private void __load()  // TODO:  Finish Patient Visit Fields
        {

        }
        public SFT() : base()
        {
            __load();
        }
        public SFT(XElement xElement) : base(xElement)
        {
        }
        public SFT(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class UAC : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        // TODO:  Finish Field Names
        private void __load()  // TODO:  Finish Patient Visit Fields
        {

        }
        public UAC() : base()
        {
            __load();
        }
        public UAC(XElement xElement) : base(xElement)
        {
        }
        public UAC(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class ROL : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public ROL() : base()
        {
            __load();
        }
        public ROL(XElement xElement) : base(xElement)
        {
        }
        public ROL(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class PD1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        { }
        public PD1() : base()
        {
            __load();
        }
        public PD1(XElement xElement) : base(xElement)
        {
        }
        public PD1(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class RXC : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {

        }
        public RXC() : base()
        {
            __load();
        }
        public RXC(XElement xElement) : base(xElement)
        {
        }
        public RXC(string __message) : base()
        {
            __load();
            __parser.Parse(__message, MessageData);
        }
    }
    public class RXD : Hl7ElementBase
    {

        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }

        public RXD() : base()
        {
        }
        public RXD(XElement xElement) : base(xElement)
        {
        }
        public RXD(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class RXE : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }

        public RXE() : base()
        {
        }
        public RXE(XElement xElement) : base(xElement)
        {
        }
        public RXE(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class RXO : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }

        public RXO() : base()
        {
        }
        public RXO(XElement xElement) : base(xElement)
        {
        }
        public RXO(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class RXR : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();


        private void __load()
        {
        }
        public RXR(XElement xElement) : base(xElement)
        {
        }
        public RXR() : base()
        {
        }

        public RXR(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class TQ1 : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }

        public TQ1() : base()
        {
        }
        public TQ1(XElement xElement) : base(xElement)
        {
        }
        public TQ1(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class ZAS : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }

        public ZAS() : base()
        {
        }
        public ZAS(XElement xElement) : base(xElement)
        {
        }
        public ZAS(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class ZLB : Hl7ElementBase   // Epic Specific
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public ZLB(XElement xElement) : base(xElement)
        {
        }
        public ZLB() : base()
        {
        }

        public ZLB(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class ZPI : Hl7ElementBase  // FrameworksLTC  Specific
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public ZPI(XElement xElement) : base(xElement)
        {
        }
        public ZPI() : base()
        {
        }

        public ZPI(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class ZFI : Hl7ElementBase
    {
        HL7MessageParser __parser = new HL7MessageParser();

        private void __load()
        {
        }
        public ZFI(XElement xElement) : base(xElement)
        {
        }
        public ZFI() : base()
        {
        }

        public ZFI(string __message) : base()
        {
            __parser.Parse(__message, MessageData);
        }
    }
    public class HL7Return
    {
        protected Dictionary<int, string> Error_Values;

        public HL7Return()
        {
            Error_Values = new Dictionary<int, string>()
            {
                { 100, @"Segment Sequence Error. The message segments were not in the proper order, or required segments are missing." },
                { 101, @"Required Field Missing. A mandatory field is missing from a segment." },
                { 102, @"Data type error. The field contained data of the wrong data type" },
                { 103, @"Table value. A field of data type ID or IS was compared against the corresponding table, and no match was found." },
                { 200, @"Unsupported message type. The Message Type is not supported." },
                { 201, @"Unsupported event code. The Event Code is not supported." },
                { 202, @"Unsupported processing id. The Processing ID is not supported." },
                { 203, @"Unsupported version id. The Version ID is not supported."},
                { 205, @"Duplicate key identifier. The ID of the patient, order, etc., already exists. Used in response to addition transactions (Admit, New Order, etc.)." },
                { 206, @"Application record locked. The transaction could not be performed at the application storage level, e.g., database locked." },
                { 207, @"Application internal error. A catch-all for internal errors not explicitly covered by other codes." }
            };
        }
        public bool __include_SFT { get; set; } = false;
        public bool __include_ERR { get; set; } = false;
        public bool __include_MSA { get; set; } = true;
    }
    public class ACK : HL7Return
    {
        public string AckString { get; set; } = string.Empty;
        public string CleanAckString { get; set; } = string.Empty;
        // XDocument xmlDoc;

        public void Build(MSH __msh, string __org, string __proc)
        {
            if (string.IsNullOrEmpty(__org))
            {
                __org = "Unknown";
            }

            if (string.IsNullOrEmpty(__proc))
            {
                __proc = "Unknown";
            }

            MSH tmpMsh = __msh;

            string __time_stamp = DateTime.Now.ToString("yyyyMMddhh");

            tmpMsh.MessageData["MSH.5"] = tmpMsh.MessageData["MSH.3"];
            tmpMsh.MessageData["MSH.6"] = tmpMsh.MessageData["MSH.4"];

            tmpMsh.MessageData["MSH.3"] = __proc;
            tmpMsh.MessageData["MSH.4"] = __org;

            AckString = '\x0B' +
                           @"MSH|^~\&|" +
                           tmpMsh.MessageData["MSH.3"] + "|" +
                           tmpMsh.MessageData["MSH.4"] + "|" +
                           tmpMsh.MessageData["MSH.5"] + "|" +
                           tmpMsh.MessageData["MSH.6"] + "|" +
                           __time_stamp + "||ACK^" +
                           tmpMsh.MessageData["MSH.9.2"] + "|" +
                           tmpMsh.MessageData["MSH.10"] + "|" +
                           tmpMsh.MessageData["MSH.11"] + "|" +
                           tmpMsh.MessageData["MSH.12"] + "|" +
                           "\r" +
                           @"MSA|AA|" +
                           tmpMsh.MessageData["MSH.10"] + "|" +
                           '\x1C' +
                           '\x0D';

            CleanAckString = @"MSH | ^~\& | " +
                           tmpMsh.MessageData["MSH.3"] + " | " +
                           tmpMsh.MessageData["MSH.4"] + " | " +
                           tmpMsh.MessageData["MSH.5"] + " | " +
                           tmpMsh.MessageData["MSH.6"] + " | " +
                           __time_stamp + "|| ACK^" +
                           tmpMsh.MessageData["MSH.9.2"] + " | " +
                           tmpMsh.MessageData["MSH.10"] + " | " +
                           tmpMsh.MessageData["MSH.11"] + " | " +
                           tmpMsh.MessageData["MSH.12"] + " | " +
                           "\n\tMSA | AA | " +
                           tmpMsh.MessageData["MSH.10"] + "|\n";
        }

        public ACK(MSH msh)
        {
            Build(msh, "Medicine-On-Time HL7 Interface", "Medicine-On-Time");
        }
        public ACK(MSH msh, string org)
        {
            Build(msh, org, "Medicine-On-Time HL7 Interface");
        }
        public ACK(MSH msh, string org, string proc)
        {
            Build(msh, org, proc);
        }
    }
    public class NAK : HL7Return
    {
        public string NakString { get; set; } = string.Empty;
        public string CleanNakString { get; set; } = string.Empty;

        public void Build(MSH msh, string errorCode, string org, string proc, string errorMsg = null)
        {
            if (msh == null) throw new ArgumentNullException(nameof(msh));
            

            if (string.IsNullOrEmpty(org))
            {
                org = "Unknown";
            }

            if (string.IsNullOrEmpty(proc))
            {
                proc = "Unknown";
            }

            if (string.IsNullOrEmpty(errorCode))
            {
                errorCode = "AF";
            }

            MSH tmpMsh = msh;
            var timeStamp = DateTime.Now.ToString("yyyyMMddhh");
            var errorString = !string.IsNullOrEmpty(errorMsg) ? errorMsg : tmpMsh.MessageData["MSH.10"];

            tmpMsh.MessageData["MSH.5"] = tmpMsh.MessageData["MSH.3"];
            tmpMsh.MessageData["MSH.6"] = tmpMsh.MessageData["MSH.4"];
            tmpMsh.MessageData["MSH.3"] = proc;
            tmpMsh.MessageData["MSH.4"] = org;

            NakString = '\x0B' +
                           @"MSH|^~\&|" +
                           tmpMsh.MessageData["MSH.3"] + "|" +
                           tmpMsh.MessageData["MSH.4"] + "|" +
                           tmpMsh.MessageData["MSH.5"] + "|" +
                           tmpMsh.MessageData["MSH.6"] + "|" +
                           timeStamp + "||NAK^" +
                           tmpMsh.MessageData["MSH.9.3"] + "|" +
                           tmpMsh.MessageData["MSH.10"] + "|" +
                           tmpMsh.MessageData["MSH.11"] + "|" +
                           tmpMsh.MessageData["MSH.12"] + "|" +
                           "\r" +
                           @"MSA|" +
                           errorCode + "|" +                 // MSA.1
                           tmpMsh.MessageData["MSH.10"] + "|" +     // MSA.2
                           "PROC ERROR|" +                      // MSA.3
                           "0|" +                               // MSA.4
                           "0|" +                               // MSA.5
                           errorString +                     // MSA.6
                           '\x1C' +
                           '\x0D';

            CleanNakString = @"MSH | ^~\& |" +
                           tmpMsh.MessageData["MSH.3"] + " | " +
                           tmpMsh.MessageData["MSH.4"] + " | " +
                           tmpMsh.MessageData["MSH.5"] + " | " +
                           tmpMsh.MessageData["MSH.6"] + " | " +
                           timeStamp + "| | NAK^" +
                           tmpMsh.MessageData["MSH.9.3"] + " | " +
                           tmpMsh.MessageData["MSH.10"] + " | " +
                           tmpMsh.MessageData["MSH.11"] + " | " +
                           tmpMsh.MessageData["MSH.12"] + " | " +
                           "\n\tMSA | " +
                           errorCode + "|" +
                           tmpMsh.MessageData["MSH.10"] + "|" +
                           "PROC ERROR|" +
                           "0|" +
                           "0|" +
                           errorString + "\n";
        }

        public NAK(MSH msh, string errorCode)
        {
            Build(msh, errorCode, "Medicine-On-Time", "Medicine-On-Time HL7 Interface");
        }
        public NAK(MSH msh, string errorCode, string org)
        {
            Build(msh, errorCode, org, "Medicine-On-Time HL7 Interface");
        }
        public NAK(MSH msh, string errorCode, string org, string proc, string errorMsg = null)
        {
            Build(msh, errorCode, org, proc, errorMsg);
        }
    }
};
