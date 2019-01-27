﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;

namespace PikaFetcher
{
    internal enum DataBaseType
    {
        Unknown,
        MySql,
        Sqlite
    }

    internal class Options
    {
        public int? Top { get; set; }

        public TimeSpan Period { get; set; }

        public string Proxy { get; set; }

        public DataBaseType DataBaseType { get; set; }

        public string DataBase { get; set; }

        public TimeSpan? Delay { get; set; }

        public int? Skip { get; set; }

        public static Options Parse(string[] args)
        {
            var dict = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                var idx = arg.IndexOf('=');
                if (idx < 0)
                {
                    dict[arg] = null;
                }
                else
                {
                    dict[arg.Substring(0, idx)] = arg.Substring(idx + 1);
                }
            }

            if (!dict.ContainsKey("period") || !dict.ContainsKey("databasetype") || !dict.ContainsKey("database"))
            {
                throw new InvalidOperationException();
            }

            var result = new Options();

            if (dict.TryGetValue("top", out var topStr) && int.TryParse(topStr, out var top))
            {
                result.Top = top;
            }

            if (dict.TryGetValue("period", out var periodStr) && TryParseTimeSpan(periodStr, out var period))
            {
                result.Period = period;
            }

            if (dict.TryGetValue("proxy", out var proxy))
            {
                result.Proxy = proxy;
            }

            if (dict.TryGetValue("databasetype", out var databasetype))
            {
                switch (databasetype)
                {
                    case "mysql":
                        result.DataBaseType = DataBaseType.MySql;
                        break;
                    case "sqlite":
                        result.DataBaseType = DataBaseType.Sqlite;
                        break;
                    default:
                        result.DataBaseType = DataBaseType.Unknown;
                        break;
                }
            }

            if (dict.TryGetValue("database", out var database))
            {
                result.DataBase = database;
            }

            if (dict.TryGetValue("delay", out var delayStr) && TryParseTimeSpan(delayStr, out var delay))
            {
                result.Delay = delay;
            }

            if (dict.TryGetValue("skip", out var skipStr) && int.TryParse(skipStr, out var skip))
            {
                result.Skip = skip;
            }

            return result;
        }

        private static bool TryParseTimeSpan(string str, out TimeSpan value)
        {
            if (TimeSpan.TryParse(str, out value))
            {
                return true;
            }

            var match = Regex.Match(str, @"^([\d\.]+)s$");
            double val;
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                value = TimeSpan.FromSeconds(val);
                return true;
            }

            match = Regex.Match(str, @"^([\d\.]+)m$");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                value = TimeSpan.FromMinutes(val);
                return true;
            }

            match = Regex.Match(str, @"^([\d\.]+)h$");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                value = TimeSpan.FromHours(val);
                return true;
            }

            match = Regex.Match(str, @"^([\d\.]+)d$");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                value = TimeSpan.FromDays(val);
                return true;
            }

            match = Regex.Match(str, @"^([\d\.]+)w$");
            if (match.Success && double.TryParse(match.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out val))
            {
                value = TimeSpan.FromDays(val * 7);
                return true;
            }

            return false;
        }
    }
}