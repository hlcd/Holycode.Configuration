using System;
using System.IO;
using System.Text;
using JsonDiffPatchDotNet;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Holycode.Configuration.Converter
{
    class Program
    {
        /// <summary>
        /// Perform transformation from xml to json file format.
        /// </summary>
        /// <param name="input">input file</param>
        /// <param name="output">output file</param>
        /// <param name="inputNode">the node in input file which will be converted, given as XPath. If not given, entire input is converted.</param>
        /// <param name="outputNode">
        /// the node in output file where converted items will be placed, given as JsonPath. If not given, entire output overwrites entire document - must not be be mixed with <c>append</c> option in this case.
        /// </param>
        /// <param name="diffBase">
        /// The file used as diff. Output file with only contain diff between <c>diffBase</c> and generated output.
        /// If <paramref name="outputNode"/> is specified, it used to for diffBase file too.
        /// </param>
        /// <param name="asPatch">
        /// If <c>true</c> and <paramref name="diffBase"/> was used too, generated configuration node will be stored
        /// as json patch. This is required to properly handle scenarios where xml configuration node contains
        /// arrays.
        /// </param>
        static void Main(FileInfo input, FileInfo output,
            string? inputNode = null, string? outputNode = null, FileInfo? diffBase = null, bool asPatch = false)
        {
            string? error = Validate(input, output, inputNode, outputNode, diffBase);
            if (error != null)
            {
                Console.WriteLine($"ERROR: {error}");
            }

            try
            {
                FileMode outputMode = outputNode != null ? FileMode.OpenOrCreate : FileMode.Create;
                string newContent;
                using (Stream inputStream = input.OpenRead())
                using (FileStream outputStream = output.Open(outputMode, FileAccess.ReadWrite, FileShare.Read))
                {
                    newContent = FromXml.Convert(inputStream, outputStream, inputNode, outputNode, diffBase, asPatch);
                }

                using (Stream saveResult = output.Open(FileMode.Create))
                {
                    var writer = new StreamWriter(saveResult, Encoding.UTF8);
                    writer.Write(newContent);
                    writer.Flush();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: {ex}");
            }
        }

        private static string? Validate(FileInfo? input, FileInfo? output, 
            string? inputNode, string? outputNode, FileInfo? diff)
        {
            if (input == null)
            {
                return "no input specified";
            }
            if (output == null)
            {
                return "no output specified";
            }

            if (!input.Exists)
            {
                return $"input file was not found: {input.FullName}";
            }
            if (outputNode != null && !output.Exists)
            {
                return $"cannot append, because output file was not found: {output.FullName}";
            }

            if (diff != null && !diff.Exists)
            {
                return $"diff base file {diff.FullName} was not found";
            }

            return null;
        }
    }
}
