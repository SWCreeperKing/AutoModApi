using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using AutoModApi.Attributes.Api;
using AutoModApi.Attributes.Documentation;
using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public static class Api
{
    public static readonly List<Type> readableTypes = new();
    public static string projectName = "Untitled App";
    public static Dictionary<string, Type> typeDictionary = new();
    public static Dictionary<string, Dictionary<string, Type>> globalPool = new();
    public static Dictionary<string, Dictionary<string, Script>> objectPool = new();
    public static string[] scriptImports = { "System", "System.Math" };

    public static Regex GetScrpitTypeAndName = new(@"type (.*?) called (.*)", RegexOptions.Compiled);

    public static void Initialize(string docPath = "")
    {
        foreach (var t in Assembly.GetEntryAssembly()!.GetTypes()
                     .Where(t => t.BaseType == typeof(ApiScript) && t != typeof(ApiScript)))
        {
            RegisterType(t);
        }
    
        PrintDocumentation(docPath);
    }

    public static void RegisterType<T>() where T : ApiScript => RegisterType(typeof(T));

    public static void RegisterType(Type t)
    {
        if (readableTypes.Contains(t)) return;
        if (t.BaseType != typeof(ApiScript) || t == typeof(ApiScript)) return;
        readableTypes.Add(t);
        var tName = t.GetName();
        typeDictionary.Add(tName, t);

        foreach (var nested in t.GetNestedTypes())
        {
            var cas = nested.GetCustomAttributes<ApiArgumentAttribute>();
            if (!cas.Any()) continue;
            if (!globalPool.ContainsKey(tName)) globalPool.Add(tName, new Dictionary<string, Type>());
            globalPool[tName].Add(cas.First().methodName, nested);
        }
    }

    public static void PrintDocumentation(string path)
    {
        var file = $"{path}/README.md";
        StringBuilder mdBuilder = new();
        mdBuilder.Append($"# Coconut (.cns) Documentation for {projectName}\n\n");
        mdBuilder.Append("## Table of Contents\n\n----\n\n");

        var names = readableTypes.Select(GetName).ToArray();

        string GetTypeLink(Type t)
        {
            var name = t.GetName();
            if (names.Any() && names.Contains(name)) return $"[{name}](#{name.ToLower()})";
            return name;
        }

        foreach (var name in names) mdBuilder.Append($"- [{name}](#{name.ToLower()})\n");

        foreach (var t in readableTypes)
        {
            var name = t.GetName();
            mdBuilder.Append($"\n## {name}\n\n");
            var doc = t.GetDoc();
            if (doc != "") mdBuilder.Append($"{doc}\n\n");
            mdBuilder.Append($"----\n\n### {name} Fields\n\n");

            foreach (var field in t.GetFields(BindingFlags.Instance | BindingFlags.Public |
                                              BindingFlags.DeclaredOnly))
            {
                mdBuilder.Append($"- {GetTypeLink(field.FieldType)} `{field.GetName()}`\n");
                var fDoc = field.GetDoc();
                if (fDoc != "") mdBuilder.Append($"  - {fDoc}\n");
            }

            mdBuilder.Append($"\n### {name} Methods\n\n");
            foreach (var method in t.GetMethods(BindingFlags.Instance | BindingFlags.Public |
                                                BindingFlags.DeclaredOnly))
            {
                var parameters = method.GetParameters();

                mdBuilder.Append($"- {GetTypeLink(method.ReturnType)} `{method.GetName()}`");
                if (parameters.Any())
                {
                    mdBuilder.Append(
                        $"({string.Join(", ", parameters.Select(t => $"{GetTypeLink(t.ParameterType)} `{t.GetName()}`"))})");
                }
                else mdBuilder.Append("()");

                mdBuilder.Append('\n');

                var mDoc = method.GetDoc();
                if (mDoc != "") mdBuilder.Append($"  - {mDoc}\n");

                if (!parameters.Any()) continue;
                foreach (var para in parameters)
                {
                    var pDoc = para.GetDoc();
                    if (pDoc == "") continue;
                    mdBuilder.Append(
                        $"  - Parameter: {GetTypeLink(para.ParameterType)} `{para.GetName()}`\n    - {para.GetDoc()}\n");
                }
            }
        }

        using var sw = File.CreateText(file);
        sw.Write(mdBuilder);
    }

    public static void ReadDir(string directory, bool recurse = true)
    {
        foreach (var file in Directory.GetFiles(directory).Where(s => s.EndsWith(".cns"))) ReadFile(file);
        if (!recurse) return;
        foreach (var dir in Directory.GetDirectories(directory)) ReadDir(dir);
    }

    public static void ReadFile(string file)
    {
        List<string> lines = new();
        using StreamReader sr = new(file);
        while (!sr.EndOfStream)
        {
            var line = sr.ReadLine();
            if (line is null or "") continue;
            lines.Add(line.Replace("\r", "").Trim());
        }

        Interpret(lines);
    }

    public static T CreateType<T>(string name) where T : ApiScript
    {
        var typeName = typeof(T).GetName();
        var objName = $"{typeName}.{name}";
        if (!objectPool.ContainsKey(objName)) throw new ArgumentException($"Type [{objName}] does not exist");
        var instanced = Activator.CreateInstance<T>();
        instanced.scripts = objectPool[$"{typeName}.{name}"];
        return instanced;
    }

    public static void Interpret(List<string> fileData)
    {
        var nameMatch = GetScrpitTypeAndName.Match(fileData[0]).Groups;
        var tName = nameMatch[1].Value;
        var name = $"{tName}.{nameMatch[2].Value}";
        Dictionary<string, Script> methods = new();
        var methodName = "";
        var isInterlop = false;
        StringBuilder methodBuilder = new();

        var i = 1;
        for (; i < fileData.Count; i++)
        {
            var s = fileData[i];
            if (s.StartsWith("method")) methodName = s[7..];
            else if (s == "interlop start") isInterlop = true;
            else if (s == "end")
            {
                if (isInterlop) isInterlop = false;
                else if (methodName != "")
                {
                    Type globals = null;

                    if (globalPool.ContainsKey(tName) && globalPool[tName].ContainsKey(methodName))
                    {
                        globals = globalPool[tName][methodName];
                    }

                    var options = ScriptOptions.Default.WithImports(scriptImports);
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
                if (isInterlop) methodBuilder.AppendLine(s);
                else
                {
                    var rs = Replacer(s);
                    if (rs == "") continue;
                    methodBuilder.AppendLine(rs);
                }
            }
        }

        objectPool.Add(name, methods);

        if (i + 1 < fileData.Count) Interpret(fileData.GetRange(i, i - fileData.Count));
    }

    public static string Replacer(string toReplace)
    {
        if (toReplace.StartsWith("print")) return $"Console.WriteLine({toReplace[6..]});";
        return toReplace.EndsWith(";") ? toReplace : toReplace + ';';
    }

    #region GetName/GetDoc stuff

    public static string GetDoc(this Type t)
    {
        var cas = t.GetCustomAttributes<DocumentAttribute>().ToList();
        return !cas.Any() ? "" : cas[0].documentation;
    }

    public static string GetDoc(this FieldInfo t)
    {
        var cas = t.GetCustomAttributes<DocumentAttribute>().ToList();
        return !cas.Any() ? "" : cas[0].documentation;
    }

    public static string GetDoc(this MethodInfo t)
    {
        var cas = t.GetCustomAttributes<DocumentAttribute>().ToList();
        return !cas.Any() ? "" : cas[0].documentation;
    }

    public static string GetDoc(this ParameterInfo t)
    {
        var cas = t.GetCustomAttributes<DocumentAttribute>().ToList();
        return !cas.Any() ? "" : cas[0].documentation;
    }

    public static string GetName(this Type t)
    {
        var cas = t.GetCustomAttributes<ApiAttribute>().ToList();
        return !cas.Any() ? t.Name : cas[0].overriderName;
    }

    public static string GetName(this FieldInfo t)
    {
        var cas = t.GetCustomAttributes<ApiAttribute>().ToList();
        return !cas.Any() ? t.Name : cas[0].overriderName;
    }

    public static string GetName(this MethodInfo t)
    {
        var cas = t.GetCustomAttributes<ApiAttribute>().ToList();
        return !cas.Any() ? t.Name : cas[0].overriderName;
    }

    public static string GetName(this ParameterInfo t)
    {
        var cas = t.GetCustomAttributes<ApiAttribute>().ToList();
        return !cas.Any() ? t.Name : cas[0].overriderName;
    }

    #endregion
}