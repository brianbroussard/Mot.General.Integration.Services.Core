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
using System.Linq;
using System.Xml.Linq;


namespace MotHL7Lib
{

    public enum HL7SendingApplication
    {
        AutoDiscover = 0,
        FrameworkLTC,
        Epic,
        QS1,
        RX30,
        RNA,
        Pharmaserve,
        Enterprise,
        Unknown
    };

    public enum DataInputType
    {
        File,
        Socket,
        WebService,
        Unknown
    }

    public class HL7Event7MessageArgs : EventArgs
    {
        public DateTime Timestamp { get; set; }
        public string Data { get; set; }
        public HL7SendingApplication Hl7SendingApp { get; set; } = HL7SendingApplication.AutoDiscover;
        public DataInputType DataInputType { get; set; } = DataInputType.Unknown;
    }

#pragma warning disable 1591
    public class Hl7ElementBase
    {
        public Dictionary<string, string> FieldNames;
        public Dictionary<string, string> MessageData;
#pragma warning restore 1591

        /// <summary>
        ///The collected set of key/value pairs in an HL message
        /// </summary>
        protected XDocument Hl7XmLset;

        int NumOf(char c, string s)
        {
            var n = s.Split(c);
            return n.Length;
        }
        /// <summary>
        /// <c>Get</c>
        /// Gets the value of a sp
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            string retVal = string.Empty;

            if (string.IsNullOrEmpty(key))
            {
                return retVal;
            }


            // The parser is a little flakey when t comes to the root ID and a .1
            // Sometimes if the request comes in with a .1, which is correct in most cases, the parser may have just
            // put it in the root, so try what was passwed and if it fails and there's at least 1 '.', strip off the last 2 chars and try again


            retVal = (from elem in Hl7XmLset.Descendants(key) select elem.Value).FirstOrDefault();


            if (string.IsNullOrEmpty(retVal))
            {
                var Tag = Hl7XmLset.Descendants().SingleOrDefault(p => p.Name.LocalName == key);

                if (Tag == null && NumOf('.', key) > 1 && key.Last() == '1')
                {

                    retVal = (from elem in Hl7XmLset.Descendants(key.Substring(0, key.LastIndexOf('.'))) select elem.Value).FirstOrDefault();
                    if (!string.IsNullOrEmpty(retVal))
                    {
                        return retVal;
                    }
                }
            }

            return retVal ?? string.Empty;
        }
        /// <summary>
        /// <c>GetList</c>
        /// Gets the list of Key/Value pairs associated with a list type tag
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public List<string> GetList(string key)
        {
            if (string.IsNullOrEmpty(key))
            {
                return null;
            }

            var ResultList = (from elem in Hl7XmLset.Descendants(key) select elem.Value).ToList();
            /*
            if((ResultList == null || ResultList.Count == 0) && Key.Contains(".1"))
            {
                var SecondTry = (from elem in hl7XMLset.Descendants(Key.Substring(0, Key.Length - 2)) select elem.Value).ToList();
                ResultList = SecondTry;
            }
            */

            return ResultList ?? new List<string>();

        }
        /// <summary>
        /// <c>ClearNewLines</c>
        /// Cleans a value of unwanted newlines
        /// </summary>
        /// <param name="stringList"></param>
        /// <returns></returns>
        protected string[] ClearNewlines(string[] stringList)
        {
            for (int i = 0; i < stringList.Length; i++)
            {
                if (stringList[i].IndexOf("\n", StringComparison.Ordinal) != -1)
                {
                    stringList[i] = stringList[i].Substring(1);
                }
            }

            return stringList;
        }
        /// <summary>
        /// Base for HL7 Parser
        /// </summary>
        /// <param name="xElement"></param>
        public Hl7ElementBase(XElement xElement)
        {
            Hl7XmLset = new XDocument(xElement);
        }
        /// <summary>
        /// Ditto
        /// </summary>
        public Hl7ElementBase()
        {
            FieldNames = new Dictionary<string, string>();
            MessageData = new Dictionary<string, string>();
        }
    }
}


