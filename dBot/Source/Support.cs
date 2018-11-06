using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace DBot.Source
{
    public static class Support
    {
        public static string DbConnectionString = @"DataSource=.\core.db;Version=3;";

        public static JObject readJsonFile(string file)
        {
            return JObject.Parse(File.ReadAllText(file));
        }

        public static void Log(string msg)
        {
            Console.WriteLine(msg + Environment.NewLine);
        }

        public static long UnixTimeStamp()
        {
            return ((DateTimeOffset)DateTime.Now).ToUnixTimeSeconds();
        }
    }
}
