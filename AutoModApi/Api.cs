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
    public static string[] referrences = Array.Empty<string>();

    public static Regex GetScriptTypeAndName = new(@"type (.*?) called (.*)", RegexOptions.Compiled);

    public static void Initialize(string docPath = "")
    {
        foreach (var t in Assembly.GetEntryAssembly()!.GetTypes()
                     .Where(t => t.BaseType == typeof(ApiScript) && t != typeof(ApiScript)))
        {
            RegisterType(t);
        }

        Documentary.PrintDocumentation(docPath);
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

        Interpret(lines.ToArray());
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

    public static void Interpret(string[] fileData)
    {
        var nameMatch = GetScriptTypeAndName.Match(fileData[0]).Groups;
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

                    if (globalPool.ContainsKey(tName) && globalPool[tName].ContainsKey(methodName))
                    {
                        globals = globalPool[tName][methodName];
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
                    var rs = InterpretReplacer(s);
                    if (rs == "") continue;
                    methodBuilder.AppendLine(rs);
                }
            }
        }

        i++;

        objectPool.Add(name, methods);

        if (i + 1 < fileData.Length) Interpret(fileData[i..]);
    }

    public static string InterpretReplacer(string toReplace)
    {
        if (!toReplace.Contains(' ')) return toReplace.EndsWith(";") ? toReplace : toReplace + ';';
        var keyWord = toReplace[..toReplace.IndexOf(' ')];
        if (Replacer.replacer.ContainsKey(keyWord)) return Replacer.replacer[keyWord].Invoke(toReplace);
        return toReplace.EndsWith(";") ? toReplace : toReplace + ';';
    }

    #region GetName/GetDoc stuff

    public static string Repeat(this string repeater, int amount)
    {
        return string.Join("", Enumerable.Repeat(repeater, amount));
    }

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