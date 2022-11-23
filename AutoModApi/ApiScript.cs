using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public static readonly List<(ApiScript script, Exception exception)> Exceptions = new();
    public Dictionary<string, Script> scripts = new();

    public async Task Execute(string method)
    {
        if (scripts.ContainsKey(method)) await scripts[method].RunAsync(catchException: LogException);
    }

    public async Task Execute(string method, object inputData)
    {
        if (scripts.ContainsKey(method)) await scripts[method].RunAsync(inputData, LogException);
    }

    public async Task<T> ExecuteAndReturn<T>(string method, T def)
    {
        if (!scripts.ContainsKey(method)) return def;
        return (T) (await scripts[method].RunAsync(catchException: LogException)).ReturnValue;
    }
    
    public async Task<T> ExecuteAndReturn<T>(string method, object inputData, T def)
    {
        if (!scripts.ContainsKey(method)) return def;
        return (T) (await scripts[method].RunAsync(inputData, LogException)).ReturnValue;
    }

    public bool LogException(Exception e)
    {
        Exceptions.Add((this, e));
        return true;
    }
}