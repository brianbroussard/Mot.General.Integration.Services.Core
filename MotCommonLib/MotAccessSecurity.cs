using System;
using System.Text;

namespace Mot.Common.Interface.Lib
{
    public static class MotAccessSecurity
    {
        public static string _salt { private get; set; } = "EditingInterfaceServices122WithLlamaFidsh";

        public static bool IsEncoded(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException();
            }

            if(DecodeString(str).Contains(_salt))
            {
                return true;
            }

            return false;
        }

        public static string EncodeString(string str)
        {
            if(string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException();
            }

            byte[] b = Encoding.UTF8.GetBytes($"{str.Length}:{str}{_salt}");
            return Convert.ToBase64String(b);
        }

        public static string DecodeString(string str)
        {
            if (string.IsNullOrEmpty(str))
            {
                throw new ArgumentNullException();
            }

            byte[] b = Convert.FromBase64String(str);
            var o = Encoding.UTF8.GetString(b);
            if (o.Contains(":"))
            {
                var n = Convert.ToInt32(o.Substring(0, o.IndexOf(':')));
                return o.Substring(o.IndexOf(':') + 1, n);
            }
            else
            {
                return str;
            }
        }
    }
}
