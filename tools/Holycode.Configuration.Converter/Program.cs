using System;
using System.IO;
using System.Text;

namespace Holycode.Configuration.Converter
{
    class Program
    {
        /// <summary>
        /// Perform transformation between xml and json file format.
        /// </summary>
        /// <param name="input">input file</param>
        /// <param name="output">output file</param>
        /// <param name="from">input file format</param>
        /// <param name="to">output file format</param>
        /// <param name="append">
        /// should we append existing <paramref name="output"/> file. If not, file is overwritten.
        /// </param>
        /// <param name="inputNode">the node in input file which will be converted. Supported formats are xpath and json path. If not given, entire input is converted.</param>
        /// <param name="outputNode">
        /// the node in output file where converted items will be placed. Supported formats are xpath and json path. If not given, entire output overwrites entire document - must not be be mixed with <c>append</c> option in this case.
        /// </param>
        /// 
        static void Main(FileInfo input, FileInfo output, Format from = Format.Xml, Format to = Format.Json,
            string? inputNode = null, string? outputNode = null)
        {
            string? error = Validate(input, output, from, to, inputNode, outputNode);
            if (error != null)
            {
                Console.WriteLine($"ERROR: {error}");
            }

            try
            {
                FileMode outputMode = outputNode != null ? FileMode.OpenOrCreate : FileMode.Create;
                string newContent;
                using (Stream inputStream = input.OpenRead())
                using (Stream outputStream = output.Open(outputMode, FileAccess.ReadWrite, FileShare.Read))
                {
                    if (from == Format.Xml)
                    {
                        newContent = FromXml.Convert(inputStream, outputStream, inputNode, outputNode);
                    }
                    else
                    {
                        throw new NotSupportedException($"converting from {from} input is not supported");
                    }
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

        private static string? Validate(FileInfo? input, FileInfo? output, Format from, Format to,
            string? inputNode, string? outputNode)
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

            if (from == to)
            {
                return "input are output format are the same";
            }

            return null;
        }
    }
}
