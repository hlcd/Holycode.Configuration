using System;
using System.ComponentModel;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;
using JsonDiffPatchDotNet;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.AspNetCore.JsonPatch.Adapters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace Holycode.Configuration.Converter
{
    internal static class FromXml
    {
        internal static string Convert(Stream xmlStream, FileStream jsonStream, string? inputNode, string? outputNode,
            FileInfo? diffBase, bool asPatch)
        {
            XElement xmlNode = ReadInputElement(xmlStream, inputNode);

            JObject current = ReadJson(jsonStream, null);
            JObject targetJsonNode = outputNode == null
                ? current
                : SelectJsonNode(current, outputNode!, true);

            string nodeAsJson = JsonConvert.SerializeXNode(xmlNode, Formatting.Indented, omitRootObject: true);
            if (diffBase != null)
            {
                JToken updated = CalculateDiff(diffBase!, nodeAsJson, outputNode, asPatch);
                targetJsonNode.Replace(updated);
            }
            else
            {
                JObject o = JsonConvert.DeserializeObject<JObject>(nodeAsJson)!;
                targetJsonNode.Merge(o);
            }

            return JsonConvert.SerializeObject(current, Formatting.Indented);
        }
        /*
        private static XElement ConvertArraysToDictionaries(XElement root)
        {
            XElement rootCopy = new XElement(root.Name);
            foreach (XAttribute xAttribute in root.Attributes())
            {
                rootCopy.SetAttributeValue(xAttribute.Name, xAttribute.Value);
            }

            var childGroups = root.Elements()
                .GroupBy(e => e.Name)
                .Select(g => new { g.Key, Count = g.Count() });
            foreach (var childGroup in childGroups)
            {
                if (childGroup.Count == 1)
                {
                    XElement child = root.Element(childGroup.Key)!;
                    rootCopy.Add(ConvertArraysToDictionaries(child));
                }
                else
                {
                    XName? keyAttribute = SelectGroupKey(childGroup.Key, root, childGroup.Count);
                    if (keyAttribute == null)
                    {
                        throw new InvalidOperationException(
                            $"unable to select key attribute for elements {childGroup.Key} " +
                            $"of node {root.Name}");
                    }

                    foreach (XElement xElement in root.Elements(childGroup.Key))
                    {
                        XAttribute key = xElement.Attribute(keyAttribute!)!;
                        XElement converted = ConvertArraysToDictionaries(xElement);
                        XElement newChild = new XElement(key.Value, converted, new XAttribute("fromArray", "true"));
                        rootCopy.Add(newChild);
                    }
                }
            }


            return rootCopy;
        }

        private static XName? SelectGroupKey(XName elementName, XElement root, int elementCount)
        {
            return root.Elements(elementName)
                //select an attribute
                .SelectMany(e => e.Attributes())
                //which is in preffered keys set
                .Where(a => PreferredDictionaryKeys.Contains(a.Name))
                .GroupBy(a => a.Name)
                //and is set for each element with unique value
                .Where(g => g.Count() == elementCount && g.GroupBy(a => a.Value).Count() == elementCount)
                .FirstOrDefault()
                ?.Key;
        }

        private static readonly XName[] PreferredDictionaryKeys = new[] { (XName)"name", (XName)"key" };
        */
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

            return SelectJsonNode(read, outputNode!, false);
        }

        private static JObject SelectJsonNode(JObject json, string outputNode, bool allowCreation)
        {
            JObject? token = json.SelectToken(outputNode) as JObject;
            if (allowCreation)
            {
                if (token == null)
                {
                    AddJsonObject(json, outputNode);
                    token = json.SelectToken(outputNode) as JObject;
                }
            }

            if (token == null)
            {
                throw new ArgumentException($"element {outputNode} was not found in output json");
            }

            return token;
        }

        private static void AddJsonObject(JObject root, string path)
        {
            JObject current = root;
            string[] segments = path.Split(new [] {'.', '$'}, StringSplitOptions.RemoveEmptyEntries);
            foreach (var segment in segments)
            {
                if (current.TryGetValue(segment, out JToken? existing))
                {
                    current = (JObject)existing;
                }
                else
                {
                    JObject newObject = new JObject();
                    current.Add(segment, newObject);
                    current = newObject;
                }
            }
        }

        private static readonly IObjectAdapter adapter = new EmptyObjectAdapter(
            new ObjectAdapter(new DefaultContractResolver(), null));

        private static JToken CalculateDiff(FileInfo diffBase, string nodeAsJson, string? outputNode, bool asPatch)
        {
            var diffMaker = new JsonDiffPatch();
            using (FileStream diffStream = diffBase.OpenRead())
            {
                JObject diffBaseJson = ReadJson(diffStream, outputNode);

                JObject obj = JObject.Parse(nodeAsJson);
                JToken delta = diffMaker.Diff(diffBaseJson, obj);

                
                PatchContainer patchOperations = AsPatch(delta);
                if (asPatch)
                {
                    return JToken.Parse(JsonConvert.SerializeObject((patchOperations)));
                }

                JsonPatchDocument patch = patchOperations.ToPatch();
                var result = new JObject();
                patch.ApplyTo(result, adapter);

                return result;
            }
        }

        private static PatchContainer AsPatch(JToken delta)
        {
            var formatter = new JsonDeltaFormatter();
            return new PatchContainer()
            {
                Operations = formatter.Format(delta)
            };
        }
    }
}
