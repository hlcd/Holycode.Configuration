using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Holycode.Configuration.Generator
{
    public class ConfigGenerator
    {
        private const string varPattern = @"\{(\?{0,1}[a-zA-Z0-9_.:]+?)\}";
        //private const string sourceDirName = "source";
        //private string baseDir;
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        public ConfigGenerator()
        {
        }

        public string GenerateConfig(string inputFile, string environment)
        {
            //var files = Directory.GetFiles($"{baseDir}/{sourceDirName}/{environment}");

            //foreach(var file in files)
            //{
            //    ProcessFile(file);
            //}
            var r = ProcessFile(inputFile);

            return r;
        }

        internal string ProcessLine(string line)
        {
            return ReplaceVariables(line, this.variables);
        }

        internal static string ReplaceVariables(string line, Dictionary<string, string> variables)
        {
            bool isMatch = false;
            do
            {
                var matches = Regex.Matches(line, varPattern);
                isMatch = matches.Count > 0;
                if (isMatch)
                {
                    foreach (Match match in matches)
                    {
                        var key = match.Groups[1].Value;
                        if (variables.ContainsKey(key))
                        {
                            line = line.Replace("{" + key + "}", variables[key]);
                        }
                    }
                }
            } while (isMatch);

            return line;
        }

        internal string ProcessContent(IEnumerable<string> lines, string type)
        {
            IConfiguration cfg = null; 
            switch(type)
            {
                case "xml":
                case ".xml":
                case ".config":
                    cfg = Helpers.LoadXMLConfigString(lines);                    
                    break;
                case "json":
                case ".json":
                    cfg = Helpers.LoadJsonConfigString(lines);
                    break;
            }

            if (cfg != null) AddVariables(cfg.AsDictionaryPlain());

            var result = new StringBuilder();
            foreach (var line in lines)
            {                
                result.AppendLine(ProcessLine(line));
            }

            return result.ToString();
        }

        private void AddVariables(Dictionary<string, string> dictionary)
        {
            foreach(var kvp in dictionary)
            {
                variables[kvp.Key] = kvp.Value;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="file"></param>
        /// <returns>processed file content</returns>
        internal string ProcessFile(string file)
        {            
            var content = File.ReadAllLines(file);

            var result = ProcessContent(content, Path.GetExtension(file));

            return result;
        }
    }

    public class Helpers
    {
        public static IConfiguration LoadXMLConfigLines(string text)
        {
            return LoadXMLConfigString("<appSettings>" + text + "</appSettings>");
        }
        public static IConfiguration LoadXMLConfigString(IEnumerable<string> lines) => LoadXMLConfigString(string.Join("\r\n", lines));
        
        public static IConfiguration LoadXMLConfigString(string text)
        {
            if (!text.StartsWith("<appSettings"))
            {
                text = "<appSettings>" + text + "</appSettings>";
            }
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, text);
            var cfg = new ConfigurationBuilder().AddXmlAppSettings(tmp).Build();

            return cfg;
        }

        internal static IConfiguration LoadJsonConfigString(IEnumerable<string> lines) => LoadJsonConfigString(string.Join("\r\n", lines));
        
        private static IConfiguration LoadJsonConfigString(string text)
        {
            var tmp = Path.GetTempFileName();
            File.WriteAllText(tmp, text);
            var cfg = new ConfigurationBuilder().AddJsonFile(tmp).Build();

            return cfg;
        }        
    }
}
