using Microsoft.AspNetCore.JsonPatch.Adapters;
using Microsoft.AspNetCore.JsonPatch.Internal;
using Newtonsoft.Json.Linq;

namespace Holycode.Configuration.Converter;

internal class EmptyObjectAdapter : IObjectAdapter
{
    private readonly IObjectAdapter adapter;

    public EmptyObjectAdapter(IObjectAdapter adapter)
    {
        this.adapter = adapter;
    }

    private void EnsurePathExists(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object obj)
    {
        ParsedPath path = new ParsedPath(operation.path);
        foreach (var pathSegment in path.Segments)
        {
            var map = (JObject)obj!;
            if (!map.ContainsKey(pathSegment))
            {
                obj = map[pathSegment] = new JObject();
            }
            else
            {
                obj = map[pathSegment]!;
            }

        }
    }

    void IObjectAdapter.Add(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo)
    {
        EnsurePathExists(operation, objectToApplyTo);
        adapter.Add(operation, objectToApplyTo);
    }

    void IObjectAdapter.Copy(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo)
    {
        EnsurePathExists(operation, objectToApplyTo);
        adapter.Copy(operation, objectToApplyTo);
    }

    void IObjectAdapter.Move(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo)
    {
        EnsurePathExists(operation, objectToApplyTo);
        adapter.Move(operation, objectToApplyTo);
    }

    void IObjectAdapter.Remove(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo)
    {
        EnsurePathExists(operation, objectToApplyTo);
        adapter.Remove(operation, objectToApplyTo);
    }

    void IObjectAdapter.Replace(Microsoft.AspNetCore.JsonPatch.Operations.Operation operation, object objectToApplyTo)
    {
        EnsurePathExists(operation, objectToApplyTo);
        adapter.Replace(operation, objectToApplyTo);
    }
}