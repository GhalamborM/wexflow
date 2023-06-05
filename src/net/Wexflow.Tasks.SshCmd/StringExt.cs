﻿using System;
using System.Text.RegularExpressions;

namespace Wexflow.Tasks.SshCmd
{
    public static class StringExt
    {
        public static string StringAfter(this string str, string substring)
        {
            int index = str.IndexOf(substring, StringComparison.Ordinal);

            return index >= 0 ? str.Substring(index + substring.Length) : string.Empty;
        }

        public static string[] GetLines(this string str) => Regex.Split(str, "\r\n|\r|\n");
    }
}
