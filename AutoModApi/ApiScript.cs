using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public Dictionary<string, Script> scripts = new();

    public object Execute(string method) => scripts[method].RunAsync().GetAwaiter().GetResult().ReturnValue;
    public object Execute(string method, object inputData)
    {
        return scripts[method].RunAsync(inputData).GetAwaiter().GetResult().ReturnValue;
    }
}