using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Formatting = System.Xml.Formatting;

namespace Holycode.Configuration.Converter
{
    internal static class FromXml
    {
        internal static string Convert(Stream xmlStream, Stream jsonStream, string? inputNode, string? outputNode)
        {
            XDocument xml = XDocument.Load(xmlStream);
            XElement targetNode = xml.Root 
                                  ?? throw new ArgumentException($"failed to parse input xml stream");
            if (inputNode != null)
            {
                XElement? selected = xml.XPathSelectElement(inputNode);
                targetNode = selected 
                             ?? throw new ArgumentException($"element {inputNode} was not found in input xml");
            }

            string nodeAsJson = JsonConvert.SerializeXNode(targetNode, Newtonsoft.Json.Formatting.Indented);
            if (outputNode == null)
            {
                return nodeAsJson;
            }
            
            StreamReader outputReader = new StreamReader(jsonStream);
            string currentJson = outputReader.ReadToEnd();
            JObject? current = JsonConvert.DeserializeObject<JObject>(currentJson);
            if (current == null)
            {
                throw new ArgumentException("output file does not contain valid json content");
            }

            JObject? token = current.SelectToken(outputNode) as JObject;
            if (token == null)
            {
                throw new ArgumentException($"element {outputNode} was not found in output json");
            }

            JObject o = JsonConvert.DeserializeObject<JObject>(nodeAsJson)!;
            token.Merge(o);

            return JsonConvert.SerializeObject(current, Newtonsoft.Json.Formatting.Indented);
        }
    }
}
