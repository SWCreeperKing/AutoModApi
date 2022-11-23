using System.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;
using static AutoModApi.Api;

namespace AutoModApi;

public interface ICompiler
{
    public KeyValuePair<string, Dictionary<string, Script>>? Compile(string fileName, string[] fileData,
        out string[] continuation, out List<(string file, Diagnostic diagnostic)> diagnostics);
}

public sealed class DefaultCompiler : ICompiler
{
    public static bool enableDiagnostics = true;

    public static string[] scriptImports = { "System", "System.Math" };
    public static string[] referrences = Array.Empty<string>();

    // regex
    public static Regex GetTypeAndName = new(@"type (.*?) called (.*)", RegexOptions.Compiled);
    public static Regex GetPrint = new(@"print (.*)", RegexOptions.Compiled);

    public KeyValuePair<string, Dictionary<string, Script>>? Compile(string fileName, string[] fileData,
        out string[] continuation, out List<(string file, Diagnostic diagnostic)> diagnostics)
    {
        diagnostics = new List<(string file, Diagnostic diagnostic)>();
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

                    if (GlobalPool.ContainsKey(tName) && GlobalPool[tName].ContainsKey(methodName))
                    {
                        globals = GlobalPool[tName][methodName];
                    }

                    var options = ScriptOptions.Default.WithImports(scriptImports);
                    if (enableDiagnostics)
                    {
                        options.WithEmitDebugInformation(true).WithFilePath(fileName)
                            .WithFileEncoding(Encoding.UTF8);
                    }

                    if (referrences.Any()) options.WithReferences(referrences);

                    var script = CSharpScript.Create(methodBuilder.ToString(), options, globalsType: globals);
                    script.Compile();
                    var scriptDiagnostics = script.GetCompilation().GetDiagnostics();

                    if (scriptDiagnostics.Any())
                    {
                        var extendFileName = $"{fileName}.{name}.{methodName}";
                        diagnostics.AddRange(scriptDiagnostics.Select(diagnostic => (extendFileName, diagnostic)));
                    }

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
        if (s.StartsWith(".")) return $"This{s};";
        return s.EndsWith(";") ? s : $"{s};";
    }
}