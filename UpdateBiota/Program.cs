using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

using ACE.Entity.Enum.Properties;

namespace UpdateBiota
{
    public class Program
    {
        public static void Main(string[] args)
        {
            if (args.Length == 0)
            {
                ShowUsage();
                return;
            }

            var scriptFile = args[0];

            if (!File.Exists(scriptFile))
            {
                Console.WriteLine($"Couldn't find {scriptFile}");
                return;
            }

            var lines = File.ReadAllLines(scriptFile);

            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i];

                if (line.StartsWith("//"))
                    continue;

                ParseLine(line, i + 1);
            }
        }

        public static void ParseLine(string line, int line_num)
        {
            // PropertyFloat.CriticalFrequency - 46215 Enhanced Shimmering Isparian Wand

            var origLine = line;

            var propType = ParsePropertyType(ref line);

            if (propType == null)
            {
                Console.WriteLine($"Couldn't find property type on line {line_num}:");
                Console.WriteLine(line);
                return;
            }

            var propNum = ParseProperty(ref line, propType.Value);

            if (propNum == -1)
            {
                Console.WriteLine($"Couldn't find property name on line {line_num}:");
                Console.WriteLine(line);
                return;
            }

            var wcid = ParseWCID(line);

            if (wcid == 0)
            {
                Console.WriteLine($"Couldn't find wcid on line {line_num}:");
                Console.WriteLine(line);
                return;
            }

            /*Console.WriteLine($"PropertyType: {propType}");
            Console.WriteLine($"PropNum: {propNum}");
            Console.WriteLine($"WCID: {wcid}");*/

            OutputSQL(propType.Value, propNum, wcid, origLine);
        }

        public static Dictionary<PropertyType, string> TableName = new Dictionary<PropertyType, string>()
        {
            { PropertyType.PropertyBool,       "bool" },
            { PropertyType.PropertyDataId,     "d_i_d" },
            { PropertyType.PropertyFloat,      "float" },
            { PropertyType.PropertyInstanceId, "i_i_d" },
            { PropertyType.PropertyInt,        "int" },
            { PropertyType.PropertyInt64,      "int64" },
            { PropertyType.PropertyString,     "string" },
        };


        public static void OutputSQL(PropertyType propType, int propNum, uint wcid, string line)
        {
            var t = TableName[propType];

            var sql = $"UPDATE biota_properties_{t} b{t}\n" +
                $"INNER JOIN biota ON b{t}.object_Id=biota.Id\n" +
                $"INNER JOIN ace_world.weenie_properties_{t} w{t} ON w{t}.object_Id=biota.weenie_Class_Id\n" +
                $"SET b{t}.value=w{t}.value\n" +
                $"WHERE biota.weenie_Class_Id={wcid} and b{t}.`type`={propNum} and w{t}.`type`={propNum};";

            Console.WriteLine($"/* {line} */\n");

            Console.WriteLine(sql);
            Console.WriteLine();
        }

        public static PropertyType? ParsePropertyType(ref string line)
        {
            var idx = line.IndexOf('.');
            if (idx == -1)
                return null;

            var ptype = line.Substring(0, idx);

            if (!Enum.TryParse(ptype, true, out PropertyType propertyType))
                return null;

            line = line.Substring(idx + 1);

            return propertyType;
        }

        public static int ParseProperty(ref string line, PropertyType propType)
        {
            var idx = line.IndexOf(' ');
            if (idx == -1)
                return -1;

            var prop = line.Substring(0, idx);

            line = line.Substring(idx + 1);

            switch (propType)
            {
                case PropertyType.PropertyBool:
                    return Enum.TryParse(prop, true, out PropertyBool bVal) ? (int)bVal : -1;
                case PropertyType.PropertyDataId:
                    return Enum.TryParse(prop, true, out PropertyDataId didVal) ? (int)didVal : -1;
                case PropertyType.PropertyFloat:
                    return Enum.TryParse(prop, true, out PropertyFloat fVal) ? (int)fVal : -1;
                case PropertyType.PropertyInstanceId:
                    return Enum.TryParse(prop, true, out PropertyInstanceId iidVal) ? (int)iidVal : -1;
                case PropertyType.PropertyInt:
                    return Enum.TryParse(prop, true, out PropertyInt iVal) ? (int)iVal : -1;
                case PropertyType.PropertyInt64:
                    return Enum.TryParse(prop, true, out PropertyInt64 lVal) ? (int)lVal : -1;
                case PropertyType.PropertyString:
                    return Enum.TryParse(prop, true, out PropertyString sVal) ? (int)sVal : -1;
                default:
                    return -1;
            }
        }

        public static uint ParseWCID(string line)
        {
            var match = Regex.Match(line, @"(\d+)");

            return match.Success && uint.TryParse(match.Groups[1].Value, out var wcid) ? wcid : 0;
        }

        public static void ShowUsage()
        {
            Console.WriteLine($"\nUsage: UpdateBiota <script>\n");
            Console.WriteLine($"Creates a SQL update script for syncing existing items on server\n");
            Console.WriteLine($"Example script:\n");
            Console.WriteLine($"PropertyFloat.CriticalFrequency - 46215 Enhanced Shimmering Isparian Wand\n");
        }
    }
}
