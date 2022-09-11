using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public Dictionary<string, Script> scripts = new();

    public async Task<object> Execute(string method)
    {
        if (!scripts.ContainsKey(method)) return null;
        return (await scripts[method].RunAsync()).ReturnValue;
    }

    public async Task<object> Execute(string method, object inputData)
    {
        if (!scripts.ContainsKey(method)) return null;
        return (await scripts[method].RunAsync(inputData)).ReturnValue;
    }

    public async Task<T> Execute<T>(string method, object inputData)
    {
        if (!scripts.ContainsKey(method)) throw new ArgumentException($"Method [{method}] does not exist");
        return (T) (await scripts[method].RunAsync(inputData)).ReturnValue;
    }
}