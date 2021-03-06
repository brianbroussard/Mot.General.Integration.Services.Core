﻿// 
// MIT license
//
// Copyright (c) 2018 by Peter H. Jenney and Medicine-On-Time, LLC.
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

using System.Runtime.InteropServices;

namespace Mot.Common.Interface.Lib
{
    public enum PlatformOs
    {
        Unknown,
        Windows,
        Unix,
        // ReSharper disable once InconsistentNaming
        macOS,
        Linux,
        Android,
        // ReSharper disable once InconsistentNaming
        iOS
    }

    public static class GetPlatformOs
    {
        public static PlatformOs Current
        {
            get
            {
                if (RuntimeInformation.OSDescription.Contains("Unix"))
                {
                    return PlatformOs.Unix;
                }
                else if (RuntimeInformation.OSDescription.Contains("Windows"))
                {
                    return PlatformOs.Windows;
                }

                return PlatformOs.Unknown;
            }

            set { }
        }

        static string FullName
        {
            get
            {
                return RuntimeInformation.OSDescription;
            }

            set { }
        }
    }
}
