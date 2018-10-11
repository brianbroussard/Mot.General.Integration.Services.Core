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
            return String().Substring(0, len);
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
            return FullTrim(String().Substring(0,len));
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

        public static double Double(int m)
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).NextDouble() * m;
        }

        public static int Integer()
        {
            return new Random((int)DateTime.Now.Ticks & 0x0000FFFF).Next();
        }
    }
}
