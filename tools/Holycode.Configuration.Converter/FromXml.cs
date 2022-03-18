using System;
using System.Collections.Generic;
using System.IO;
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
        private static readonly JsonSerializerSettings JsonOptions = new JsonSerializerSettings()
        {
            NullValueHandling = NullValueHandling.Ignore,
            Formatting = Formatting.Indented
        };
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

            string json = JsonConvert.SerializeObject(current, JsonOptions);
            //not sure why NullValueHandling.Ignore does not kick in
            //workaround manually
            return json
                .Replace(@"""from"": null,", string.Empty)
                .Replace(@"""value"": null,", string.Empty);
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
            var diffMaker = new JsonDiffPatch(new Options()
            {
                ArrayDiff = ArrayDiffMode.Efficient,
                //disable text diff, since json patch standard does not support it
                MinEfficientTextDiffLength = int.MaxValue - 1,
            });
            using (FileStream diffStream = diffBase.OpenRead())
            {
                JObject diffBaseJson = ReadJson(diffStream, outputNode);
                //special handling for loggers
                //since we want add only behaviour for loggers
                if (diffBaseJson.ContainsKey(LoggerNode))
                {
                    diffBaseJson[LoggerNode] = null;
                }

                JObject obj = JObject.Parse(nodeAsJson);
                JToken delta = diffMaker.Diff(diffBaseJson, obj);

                PatchContainer patchOperations = AsPatch(delta);
                patchOperations = HandleAdditiveLoggers(patchOperations);
                
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

        private const string LoggerNode = "logger";

        private static PatchContainer HandleAdditiveLoggers(PatchContainer patch)
        {
            var toRemove = new List<Operation>();
            var newOperations = new List<Operation>();
            foreach (var operation in patch.Operations!)
            {
                if (operation?.Path == "/" + LoggerNode
                    && operation.Op == OperationTypes.Replace)
                {
                    if (operation.Value is JArray newLoggersArray)
                    {
                        //we need to convert operation of replacing array
                        //to set of add item operations
                        foreach (JToken logger in newLoggersArray)
                        {
                            newOperations.Add(new Operation(OperationTypes.Add, operation.Path + "/-", null, logger));
                        }

                        toRemove.Add(operation);
                    }
                    else
                    {
                        //indicates insert at the end of the table
                        operation.Path += "/-";
                        operation.Op = OperationTypes.Add;
                    }
                }
            }

            foreach (var operation in toRemove)
            {
                patch.Operations.Remove(operation);
            }
            foreach (var operation in newOperations)
            {
                patch.Operations.Add(operation);
            }

            return patch;
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
