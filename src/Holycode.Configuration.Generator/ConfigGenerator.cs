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
        private const string sourceDirName = "source";
        //private string baseDir;
        private Dictionary<string, string> variables = new Dictionary<string, string>();
        public ConfigGenerator()
        {
        }

        public void GenerateConfig(string baseDir, string environment, string outDir = null)
        {
            outDir = outDir ?? baseDir;
            var srcDir = Path.GetFullPath($"{baseDir}/{sourceDirName}/{environment}");
            var files = Directory.GetFiles(srcDir);

            foreach (var file in files)
            {
                if (Path.GetFileName(file).StartsWith("_")) continue;
                var r = ProcessFile(file);
                var relpath = file.Replace(srcDir, "").Trim('\\').Trim('/');
                var outfile = Path.Combine(outDir, relpath);

                File.WriteAllText(outfile, r);
            }            
        }

        internal string ProcessLine(string line)
        {
            return ReplaceVariables(line, this.variables);
        }

        internal static string ReplaceVariables(string line, Dictionary<string, string> variables)
        {
            bool isMatch = false;
            bool replaced = false;
            do
            {
                var matches = Regex.Matches(line, varPattern);
                isMatch = matches.Count > 0;
                replaced = false;

                if (isMatch)
                {
                    foreach (Match match in matches)
                    {
                        var key = match.Groups[1].Value;
                        var originalKey = key;
                        // replace . with new way of hierarchical reference
                        key = key.Replace(".", ":");
                        if (variables.ContainsKey(key))
                        {
                            line = line.Replace("{" + originalKey + "}", variables[key]);
                            replaced = true;
                        }
                    }
                }
            } while (replaced);

            return line;
        }

        internal string ProcessContent(IEnumerable<string> lines, string filepath)
        {
            IConfiguration cfg = null;

            lines = PreProcess(lines, filepath);
            var type = Path.GetExtension(filepath);
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
                default:
                    throw new NotSupportedException($"don't know how to handle file {filepath}");
            }

            AddVariables(cfg.AsDictionaryPlain());

            var result = new StringBuilder();
            foreach (var line in lines)
            {                
                result.AppendLine(ProcessLine(line));
            }

            return result.ToString();
        }

        private IEnumerable<string> PreProcess(IEnumerable<string> lines, string filepath)
        {
            const string varsDirective = @"\[\[\s*vars\s+(?<file>.*)\s*\]\]";
            foreach(var line in lines)
            {
                var m = Regex.Match(line, varsDirective, RegexOptions.IgnoreCase);
                if (m.Success)
                {
                    var incl = m.Groups["file"].Value;
                    incl = Path.Combine(Path.GetDirectoryName(filepath), incl);
                    AddVariables(incl);
                    continue;
                }

                yield return line;
            }
        }

        private void AddVariables(string file)
        {
            IConfiguration cfg = null;
            var ext = Path.GetExtension(file);
            switch (ext) {
                case ".xml":
                    cfg = new ConfigurationBuilder().AddXmlAppSettings(file, optional: false).Build();
                    break;
                case ".json":
                    cfg = new ConfigurationBuilder().AddXmlAppSettings(file, optional: false).Build();
                    break;
                default:
                    throw new NotSupportedException($"file format {ext} is not supported as variables source");
            }

            AddVariables(cfg.AsDictionaryPlain());
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

            var result = ProcessContent(content, file);

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
