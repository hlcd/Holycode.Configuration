using System.Collections.Generic;
using JsonDiffPatchDotNet.Formatters.JsonPatch;
using Microsoft.AspNetCore.JsonPatch;
using Newtonsoft.Json;

namespace Holycode.Configuration.Converter;

internal class PatchContainer
{
    public IList<Operation?>? Operations { get; set; }

    public JsonPatchDocument ToPatch()
    {
        string patchString = JsonConvert.SerializeObject(Operations);
        return JsonConvert.DeserializeObject<JsonPatchDocument>(patchString)!;
    }
}