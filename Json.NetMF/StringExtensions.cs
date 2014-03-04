// Source code is modified from Mike Jones's JSON Serialization and Deserialization library (https://www.ghielectronics.com/community/codeshare/entry/357)

using System;
using Microsoft.SPOT;
using System.Collections;

namespace Json.NETMF
{
	internal static class StringExtensions
	{
        public static bool EndsWith(string s, string value)
        {
            return s.IndexOf(value) == s.Length - value.Length;
        }

        public static bool StartsWith(string s, string value)
        {
            return s.IndexOf(value) == 0;
        }

        public static bool Contains(string s, string value)
        {
            return s.IndexOf(value) >= 0;
        }
	}
}
