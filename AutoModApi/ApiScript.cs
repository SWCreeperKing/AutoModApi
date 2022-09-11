using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public Dictionary<string, Script> scripts = new();

    public async Task Execute(string method)
    {
        if (scripts.ContainsKey(method)) await scripts[method].RunAsync();
    }

    public async Task Execute(string method, object inputData)
    {
        if (scripts.ContainsKey(method)) await scripts[method].RunAsync(inputData);
    }

    public async Task<T> Execute<T>(string method, T def)
    {
        if (!scripts.ContainsKey(method)) return def;
        return (T) (await scripts[method].RunAsync()).ReturnValue;
    }
    
    public async Task<T> Execute<T>(string method, object inputData, T def)
    {
        if (!scripts.ContainsKey(method)) return def;
        return (T) (await scripts[method].RunAsync(inputData)).ReturnValue;
    }
}