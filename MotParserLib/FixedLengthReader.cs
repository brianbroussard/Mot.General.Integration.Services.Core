using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace Mot.Parser.InterfaceLib
{
    [AttributeUsage(AttributeTargets.Field)]
    class LayoutAttribute : Attribute
    {
        public int Index { get; }
        public int Length { get; }

        public LayoutAttribute(int index, int length)
        {
            Index = index;
            Length = length;
        }
    }
    internal class FixedLengthReader : IDisposable
    {
        private readonly Stream _stream;
        private byte[] _buffer;

        public FixedLengthReader(byte[] data)
        {
            _stream = new MemoryStream(data);
            _buffer = new byte[4];
        }

        public FixedLengthReader(string data)
        {
            _stream = new MemoryStream(Encoding.UTF8.GetBytes(data));
            _buffer = new byte[4];

        }
        public FixedLengthReader(Stream stream)
        {
            _stream = stream;
            _buffer = new byte[4];
        }

        public void Read<T>(T data)
        {
            foreach (FieldInfo fieldInfo in typeof(T).GetFields())
            {
                foreach (Attribute attribute in fieldInfo.GetCustomAttributes())
                {
                    if (attribute is LayoutAttribute layoutAttribute)
                    {
                        _stream.Seek(layoutAttribute.Index, SeekOrigin.Begin);

                        if (_buffer.Length < layoutAttribute.Length)
                        {
                            _buffer = new byte[layoutAttribute.Length];
                        }

                        _stream.Read(_buffer, 0, layoutAttribute.Length);

                        if (fieldInfo.FieldType == typeof(int))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToInt32(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(bool))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToBoolean(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(string))
                        {
                            // --- If string was written using UTF8 ---
                            byte[] tmp = new byte[layoutAttribute.Length];
                            Array.Copy(_buffer, tmp, tmp.Length);
                            fieldInfo.SetValue(data, Encoding.UTF8.GetString(tmp));

                            // --- ALTERNATIVE: Chars were written to file ---
                            //char[] tmp = new char[la.length - 1];
                            //for (int i = 0; i < la.length; i++)
                            //{
                            //    tmp[i] = BitConverter.ToChar(buffer, i * sizeof(char));
                            //}
                            //fi.SetValue(data, new string(tmp));
                        }
                        else if (fieldInfo.FieldType == typeof(double))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToDouble(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(short))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToInt16(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(long))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToInt64(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(float))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToSingle(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(ushort))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToUInt16(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(uint))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToUInt32(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(ulong))
                        {
                            fieldInfo.SetValue(data, BitConverter.ToUInt64(_buffer, 0));
                        }
                        else if (fieldInfo.FieldType == typeof(DateTime))
                        {
                            byte[] tmp = new byte[layoutAttribute.Length];
                            Array.Copy(_buffer, tmp, tmp.Length);


                            if (layoutAttribute.Length == 6)
                            {
                                var date = DateTime.ParseExact(Encoding.UTF8.GetString(tmp),
                                                               "mmDDYY",
                                                               CultureInfo.InvariantCulture,
                                                               DateTimeStyles.None
                                                                );

                                fieldInfo.SetValue(data, Encoding.UTF8.GetBytes(date.ToString(CultureInfo.InvariantCulture)));
                            }
                            else if (layoutAttribute.Length == 4)
                            {
                                var time = DateTime.ParseExact(Encoding.UTF8.GetString(tmp),
                                                                "HHMM",
                                                                CultureInfo.InvariantCulture,
                                                                DateTimeStyles.None
                                                                );

                                fieldInfo.SetValue(data, Encoding.UTF8.GetBytes(time.ToString(CultureInfo.InvariantCulture)));
                            }
                            else
                            {
                                fieldInfo.SetValue(data, Encoding.UTF8.GetBytes("Unknown Date/Time"));
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //Socket?.Dispose();
                //Socket = null;
            }
        }
        /// <summary>
        /// <c>Dispose</c>
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
