using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public interface ICompiler
{
    public KeyValuePair<string, Dictionary<string, Script>>? Compile(string[] fileData, out string[] continuation);
}

public class DefaultCompiler : ICompiler
{
    public static string[] scriptImports = { "System", "System.Math" };
    public static string[] referrences = Array.Empty<string>();
    public static Regex GetTypeAndName = new(@"type (.*?) called (.*)", RegexOptions.Compiled);
    public static Regex GetPrint = new(@"print (.*)", RegexOptions.Compiled);

    public KeyValuePair<string, Dictionary<string, Script>>? Compile(string[] fileData, out string[] continuation)
    {
        var nameMatch = GetTypeAndName.Match(fileData[0]).Groups;
        var tName = nameMatch[1].Value;
        var name = $"{tName}.{nameMatch[2].Value}";
        Dictionary<string, Script> methods = new();
        var methodName = "";
        var isInterop = false;
        StringBuilder methodBuilder = new();

        var i = 1;
        for (; i < fileData.Length; i++)
        {
            var s = fileData[i];
            if (s.StartsWith("method")) methodName = s[7..];
            else if (s == "interop start") isInterop = true;
            else if (s == "end")
            {
                if (isInterop) isInterop = false;
                else if (methodName != "")
                {
                    Type? globals = null;

                    if (Api.GlobalPool.ContainsKey(tName) && Api.GlobalPool[tName].ContainsKey(methodName))
                    {
                        globals = Api.GlobalPool[tName][methodName];
                    }

                    var options = ScriptOptions.Default.WithImports(scriptImports);
                    if (referrences.Any()) options.WithReferences(referrences);

                    var script = CSharpScript.Create(methodBuilder.ToString(), options, globalsType: globals);
                    script.Compile();
                    methods.Add(methodName, script);
                    methodName = "";
                    methodBuilder.Clear();
                }
                else break;
            }
            else
            {
                if (isInterop) methodBuilder.AppendLine(s);
                else
                {
                    var rs = Interpreter(s);
                    if (rs == "") continue;
                    methodBuilder.AppendLine(rs);
                }
            }
        }

        i++;

        continuation = i + 1 < fileData.Length ? fileData[i..] : Array.Empty<string>();
        return new KeyValuePair<string, Dictionary<string, Script>>(name, methods);
    }

    public string Interpreter(string s)
    {
        if (GetPrint.IsMatch(s)) return $"Console.WriteLine({GetPrint.Match(s).Groups[1].Value});";
        if (s.StartsWith(".")) return "This" + s + ';';
        return s.EndsWith(";") ? s : s + ';';
    }
}