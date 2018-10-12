using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;

namespace Mot.Common.Interface.Lib
{
    public static class RandomData
    {
        static List<string> TextCollection = new List<string>();

        public static string String()
        {
            if (!TextCollection.Any())
            {
                TextCollection = GetLorum(50).ToList();
            }

            var rng = new Random();
            var randomElement = TextCollection[rng.Next(TextCollection.Count)];

            return randomElement;
        }

        public static string String(int len)
        {
            if (len > TextCollection[0].Length)
            {
                var s = string.Empty;

                while(s.Length < len)
                {
                    s += String();
                }

                return s.Substring(0,len);
            }

            return String();
        }

        private static string[] GetLorum(int lineCount)
        {
            // build the JSON request URL
            var requestUrl = $"http://loripsum.net/api/{lineCount}/long/plaintext";

            try
            {
                using (var client = new HttpClient())
                {
                    using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(requestUrl)))
                    {
                        request.Headers.Add("Accept", "application/json");

                        using (var response = client.SendAsync(request).Result)
                        {
                            response.EnsureSuccessStatusCode();
                            var text = response.Content.ReadAsStringAsync().Result;
                            return text.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        }
                    }
                }
            }
            catch (HttpRequestException hex)
            {
                Console.WriteLine(hex.Message);
                throw;
                //return default(T);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public static string TrimString()
        {
            return FullTrim(String());
        }

        public static string TrimString(int len)
        {
            return FullTrim(String().Substring(0, len));
        }

        // ------- Format Utilities
        private static string FullTrim(string val)
        {
            if (string.IsNullOrEmpty(val))
            {
                return string.Empty;
            }

            char[] junk = { ' ', '-', '.', ',', ' ', ';', ':', '(', ')', '?', '*', '!' };

            while (val?.IndexOfAny(junk) > -1)
            {
                val = val.Remove(val.IndexOfAny(junk), 1);
            }

            return val;
        }

        public static double Double(int multiplier)
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble() * multiplier;
        }

        public static int Integer(int minVal = 1, int maxVal = int.MaxValue)
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next(minVal, maxVal);
        }

        public static DateTime Date(int minYear = 1900)
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            var year = rnd.Next(minYear, DateTime.Now.Year);
            var month = rnd.Next(1, 13);
            var days = rnd.Next(1, DateTime.DaysInMonth(year, month) + 1);

            return new DateTime(year, month, days, rnd.Next(0, 24), rnd.Next(0, 60), rnd.Next(0, 60), rnd.Next(0, 1000));
        }

        public static int Bit()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return rnd.Next(0, 2);
        }

        public static string USPhoneNumber()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return $"({rnd.Next(1, 1000).ToString("D3")}){rnd.Next(1, 1000).ToString("D3")}-{rnd.Next(0000, 10000).ToString("D4wr")}";
        }

        public static string SSN()
        {
            var rnd = new Random(Guid.NewGuid().GetHashCode());
            return $"{rnd.Next(1, 1000).ToString("D3")}-{rnd.Next(1, 100).ToString("D2")}-{rnd.Next(1000, 10000).ToString("D4")}";
        }

        public static string DoseTimes(int numTimes = 1)
        {
            var doseTime = $"{RandomData.Integer(0, 25).ToString("D2")}{RandomData.Integer(0, 61).ToString("D2")}" +
                           $"{RandomData.Integer(0, 13).ToString("D2")}{RandomData.Integer(0, 100).ToString("D2")}";

            if(numTimes > 1)
            {
                for(var i = 1; i < numTimes; i++)
                {
                    doseTime += $"{RandomData.Integer(0, 25).ToString("D2")}{RandomData.Integer(0, 61).ToString("D2")}" +
                                $"{RandomData.Integer(0, 13).ToString("D2")}{RandomData.Integer(0, 100).ToString("D2")}";
                }
            }

            return doseTime;
        }
    }
}
