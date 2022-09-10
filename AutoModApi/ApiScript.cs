using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public class ApiScript
{
    public Dictionary<string, Script> scripts = new();

    public object Execute(string method) => scripts[method].RunAsync().GetAwaiter().GetResult().ReturnValue;

    public object Execute(string method, object inputData)
    {
        try
        {
            return scripts[method].RunAsync(inputData).GetAwaiter().GetResult().ReturnValue;
        }
        catch (CompilationErrorException e)
        {
            var before = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Red;
            // Console.WriteLine($"Error in script: \nON ({e.})\n{scripts[method].Code}");
            Console.WriteLine($"Diagnostics:\n{string.Join("\n", e.Diagnostics)}");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine($"Code:\n{scripts[method].Code.Replace("\r", "")}");
            Console.ForegroundColor = before;
            return null;
        }
    }
}