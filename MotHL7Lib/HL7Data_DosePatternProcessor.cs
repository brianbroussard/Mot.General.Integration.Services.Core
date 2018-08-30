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
using Mot.Common.Interface.Lib;

namespace Mot.HL7.Interface.Lib
{
    class MotHl7DosePatternProcessor
    {
        public MotHl7DosePatternProcessor()
        { }

        public delegate bool BoolDoseScheduleDelegate(string data, int count, TQ1 tq1);
        private Dictionary<string, BoolDoseScheduleDelegate> ProcessDoseSchedule;
        private RecordBundle RecBundle;
        MotHl7DosePatternProcessor(RecordBundle recBundle)
        {
            this.RecBundle = recBundle;

            try
            {
                InitializeDoseScheduleProccessor();
            }
            catch
            {
                throw;
            }
        }
        public void InitializeDoseScheduleProccessor()
        {
            ProcessDoseSchedule = new Dictionary<string, BoolDoseScheduleDelegate>()
            {
                { "QnD", ProcessQnD },
                { "QJn", ProcessQJn },
                { "QnH", ProcessQnH },
                { "QnJn", ProcessQnJn},
                { "QnL", ProcessQnL },
                { "QnM", ProcessQnM },
                { "QnS", ProcessQnS },
                { "QnW", ProcessQnW },
                { "xID", ProcessxID },
                { "BID", ProcessBID },
                { "TID", ProcessTID },
                { "QID", ProcessQID },
                { "PRN", ProcessPRN },
                { "PRNx", ProcessPRNx }
            };
        }

        // Usage  --  ProcessDoseSchedule["QJn"]("data pattern", n);
        public bool ProcessQJn(string Data, int Count, TQ1 tq1)
        { return true; }

        // Usage -- ProcessDoseSchedule["QnD"]("data pattern", n);
        public bool ProcessQnD(string Data, int Count, TQ1 tq1)
        {
            try
            {
                string tq121;
                string tq14;

                string[] month = new string[31];
                int day = Convert.ToInt32(Data.Substring(1, 1));

                int i = 0;

                while (i < month.Length)
                {
                    month[i++] = "00.00";
                }

                for (i = 0; i < month.Length; i += day)
                {
                    tq121 = tq1.Get("TQ1.2.1");
                    month[i] = $"{(Convert.ToDouble(string.IsNullOrEmpty(tq121) ? "0" : tq121)):00.00}";
                }

                i = 1;
                while (i < month.Length)
                {
                    month[0] += month[i++];
                }

                RecBundle.Scrip.RxType = 18;
                RecBundle.Scrip.MDOMStart = Data.Substring(1, 1);  // Extract the number 
                RecBundle.Scrip.DoseScheduleName = tq1.Get("TQ1.3.1");  // => This is the key part.  The DosScheduleName has to exist on MOTALL 
                RecBundle.Scrip.SpecialDoses = month[0];

                tq14 = tq1.Get("TQ1.4");
                tq121 = tq1.Get("TQ1.2.1");

                if (string.IsNullOrEmpty(tq14) || string.IsNullOrEmpty(tq121))
                {
                    return false;
                }

                RecBundle.Scrip.DoseTimesQtys += $"{tq14}{Convert.ToDouble(tq121):00.00}";

                return true;
            }
            catch
            {
                return false;
            }

        }
        public bool ProcessQnH(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQnJn(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQnL(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQnM(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQnS(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQnW(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessxID(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessBID(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessTID(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessQID(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessPRN(string data, int count, TQ1 tq1)
        { return true; }
        public bool ProcessPRNx(string data, int count, TQ1 tq1)  // PRN with frequency code
        { return true; }

        // Messages

    }
}
