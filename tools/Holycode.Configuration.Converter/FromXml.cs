using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Holycode.Configuration.Converter
{
    internal static class FromXml
    {
        internal static string Convert(Stream xmlStream, FileStream jsonStream, string? inputNode, string? outputNode,
            FileInfo? diffBase)
        {
            XElement xmlNode = ReadInputElement(xmlStream, inputNode);
            JObject current = ReadJson(jsonStream, null);
            JObject targetJsonNode = outputNode == null
                ? current
                : SelectJsonNode(current, outputNode!);

            string nodeAsJson = JsonConvert.SerializeXNode(xmlNode, Formatting.Indented);
            if (diffBase != null)
            {
                JObject updated = CalculateDiff(diffBase!, nodeAsJson, outputNode);
                targetJsonNode.Replace(updated);
            }
            else
            {
                JObject o = JsonConvert.DeserializeObject<JObject>(nodeAsJson)!;
                targetJsonNode.Merge(o);
            }

            return JsonConvert.SerializeObject(current, Formatting.Indented);
        }

        private static XElement ReadInputElement(Stream xmlStream, string? inputNode)
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

            return targetNode;
        }

        private static JObject ReadJson(FileStream jsonStream, string? outputNode)
        {
            StreamReader outputReader = new StreamReader(jsonStream);
            string currentJson = outputReader.ReadToEnd();
            JObject read = JsonConvert.DeserializeObject<JObject>(currentJson) ?? new JObject();

            if (outputNode == null)
            {
                return read;
            }

            return SelectJsonNode(read, outputNode!);
        }

        private static JObject SelectJsonNode(JObject json, string outputNode)
        {
            JObject? token = json.SelectToken(outputNode) as JObject;
            if (token == null)
            {
                throw new ArgumentException($"element {outputNode} was not found in output json");
            }

            return token;
        }

        private static JObject CalculateDiff(FileInfo diffBase, string nodeAsJson, string? outputNode)
        {
            var diffMaker = new JsonDiffPatch();
            using (FileStream diffStream = diffBase.OpenRead())
            {
                JObject diffBaseJson = ReadJson(diffStream, outputNode);

                JObject obj = JObject.Parse(nodeAsJson);
                JToken delta = diffMaker.Diff(diffBaseJson, obj);

                var formatter = new JsonDeltaFormatter();
                IList<Operation>? operations = formatter.Format(delta);
                string patchString = JsonConvert.SerializeObject(operations);

                JsonPatchDocument patch = JsonConvert.DeserializeObject<JsonPatchDocument>(patchString)!;
                dynamic result = new JObject();
                patch.ApplyTo(result);

                return result;
            }
        }
    }
}
