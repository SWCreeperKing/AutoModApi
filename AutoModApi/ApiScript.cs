using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public Dictionary<string, Script> scripts = new();

    public async Task<object> Execute(string method, object def = null)
    {
        return !scripts.ContainsKey(method) ? def : (await scripts[method].RunAsync()).ReturnValue;
    }

    public async Task<object> Execute(string method, object inputData, object def = null)
    {
        return !scripts.ContainsKey(method) ? def : (await scripts[method].RunAsync(inputData)).ReturnValue;
    }

    public async Task<T> Execute<T>(string method, object inputData, T def)
    {
        if (!scripts.ContainsKey(method)) return def;
        return (T) (await scripts[method].RunAsync(inputData)).ReturnValue;
    }
}