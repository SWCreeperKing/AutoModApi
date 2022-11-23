using System.Reflection;
using AutoModApi.Attributes.Api;
using AutoModApi.Attributes.Documentation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

/// <summary>
/// Main class for the Modding API Extention
/// </summary>
public static class Api
{
    public static readonly List<Type> ReadableTypes = new();
    public static readonly List<Type> DocExcludeTypes = new();
    public static readonly Dictionary<string, Type> TypeDictionary = new();
    public static readonly Dictionary<string, Dictionary<string, Type>> GlobalPool = new();
    public static readonly Dictionary<string, Dictionary<string, Script>> ObjectPool = new();

    public static float CompilationPercent { get; private set; }
    public static string CurrentlyReadingFile { get; private set; }

    public static string projectName = "Untitled App";
    public static bool showErrors = true;
    public static ICompiler interpretCompiler = new DefaultCompiler();
    public static List<(string file, Diagnostic diagnostic)> compileDiagnostics = new();

    private static readonly Action<float> SetPercent = f => CompilationPercent = f;
    private static readonly List<Action<string>> OnFileRegister = new();
    private static readonly List<string> FilesToRegister = new();

    public static event Action<string> OnRegister
    {
        add => OnFileRegister.Add(value);
        remove => OnFileRegister.Remove(value);
    }

    public static void Initialize(string docPath = "", string notes = "")
    {
        foreach (var t in Assembly.GetEntryAssembly()!.GetTypes().Where(IsTypeOfApiScript))
        {
            RegisterType(t);
        }

        Documentary.PrintDocumentation(docPath, notes);
    }

    public static void RegisterType<T>() where T : ApiScript => RegisterType(typeof(T));

    public static void RegisterType(Type t)
    {
        if (ReadableTypes.Contains(t)) return;
        if (!IsTypeOfApiScript(t)) return;
        ReadableTypes.Add(t);

        if (t.GetCustomAttributes<DocIgnoreAttribute>().Any()) DocExcludeTypes.Add(t);

        var tName = t.GetName();
        TypeDictionary.Add(tName, t);

        foreach (var nested in t.GetNestedTypes())
        {
            var cas = nested.GetCustomAttributes<ApiArgumentAttribute>();
            if (!cas.Any()) continue;
            if (!GlobalPool.ContainsKey(tName)) GlobalPool.Add(tName, new Dictionary<string, Type>());

            foreach (var ca in cas.First().methodNames) GlobalPool[tName].Add(ca, nested);
        }
    }

    public static void ReadDir(string directory, bool recurse = true)
    {
        foreach (var file in Directory.GetFiles(directory).Where(s => s.EndsWith(".cns"))) ReadFile(file);
        if (!recurse) return;
        foreach (var dir in Directory.GetDirectories(directory)) ReadDir(dir);
    }

    public static void ReadFile(string file) => FilesToRegister.Add(file.Replace("\\", "/"));

    public static bool DoesTypeExist<T>(string name) where T : ApiScript
    {
        return DoesTypeExist(typeof(T), name);
    }

    public static bool DoesTypeExist(Type type, string name)
    {
        if (!type.IsAssignableTo(typeof(ApiScript))) return false;
        var objName = type.GetObjectName(name);
        return ObjectPool.ContainsKey(objName);
    }

    public static T CreateType<T>(string name, params object[] parameters) where T : ApiScript
    {
        return (T) CreateType(typeof(T), name, parameters)!;
    }

    public static ApiScript? CreateType(Type type, string name, params object[] parameters)
    {
        if (!type.IsAssignableTo(typeof(ApiScript))) return null;
        var objName = type.GetObjectName(name);
        if (!ObjectPool.ContainsKey(objName)) throw new ArgumentException($"Type [{objName}] does not exist");

        var instanced = (ApiScript) (parameters.Any()
            ? Activator.CreateInstance(type, parameters)!
            : Activator.CreateInstance(type)!);

        instanced.scripts = ObjectPool[objName];
        return instanced;
    }

    public static void Compile(bool clear = true)
    {
        if (clear)
        {
            CompilationPercent = 0;
            ObjectPool.Clear();
            compileDiagnostics.Clear();
            ApiScript.Exceptions.Clear();
        }

        void AddToObjectPool(string fileName, string[] file)
        {
            var add = interpretCompiler.Compile(fileName, file, out var continuation, out var diagnostics);
            compileDiagnostics.AddRange(diagnostics);
            if (add is null) return;
            var addV = add.Value;
            ObjectPool.Add(addV.Key, addV.Value);
            foreach (var o in OnFileRegister) o(addV.Key);
            if (continuation.Any() && continuation.Sum(s => s.Length) > 0) AddToObjectPool(fileName, continuation);
        }

        Task.Run(async () =>
        {
            try
            {
                CompilationPercent = 0;
                var files = FilesToRegister.Count;

                for (var i = 0; i < files; i++)
                {
                    CompilationPercent = (i + .01f) / files;
                    var file = FilesToRegister[i];
                    CurrentlyReadingFile = file.Split("/")[^1];

                    if (!File.ReadLines(file).Any() && File.ReadLines(file).Sum(s => s.Length) < 1) continue;

                    List<string> lines = new();
                    using StreamReader sr = new(file);
                    while (!sr.EndOfStream)
                    {
                        var line = await sr.ReadLineAsync();
                        if (line is null or "") continue;
                        lines.Add(line.Replace("\r", "").Trim());
                    }

                    CompilationPercent = (i + .5f) / files;

                    AddToObjectPool(file, lines.ToArray());
                }

                CompilationPercent = 1;
                CurrentlyReadingFile = "";
            }
            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"\n\n\n\n\n{e}");
            }
        });
    }

    public static void CompileWithLoading()
    {
        var i = 0;
        var lastRead = "";
        Compile();

        while (CompilationPercent != 1)
        {
            var read =$"Compiling File: [{CurrentlyReadingFile}]";
            i++;
            
            Task.Delay(150).GetAwaiter().GetResult();
            Console.SetCursorPosition(0, 0);
            Console.WriteLine($"Compilation Progress: [{CompilationPercent:##0.##%}]         ");

            if (lastRead != read)
            {
                Console.WriteLine(string.Join("", Enumerable.Repeat(" ", lastRead.Length + 3)));
                Console.SetCursorPosition(0, 1);
                lastRead = read;
            }

            Console.WriteLine(read);
            Console.WriteLine($"Compiling Please Wait [{".".Repeat(i)}{" ".Repeat(3 - i)}]      ");
            
            i %= 3;
        }

        
        Console.SetCursorPosition(0, 1);
        Console.WriteLine(string.Join("", Enumerable.Repeat(" ", lastRead.Length + 3)));
        Console.WriteLine("                                                   ");
        Console.SetCursorPosition(0, 1);

        if (!compileDiagnostics.Any(d => (int) d.diagnostic.Severity >= 2)) return;

        var before = Console.ForegroundColor;
        Console.ForegroundColor = ConsoleColor.Red;

        if (showErrors)
        {
            foreach (var (file, diag) in compileDiagnostics.Where(d => (int) d.diagnostic.Severity >= 2))
            {
                Console.WriteLine($"[{diag.Severity}] in file [{file}] |> [{diag.GetMessage()}]");
            }
        }
        else
        {
            Console.WriteLine(
                $"Compiled with [{compileDiagnostics.Count(d => (int) d.diagnostic.Severity >= 2)}] Compiler Warnings/Errors");
        }

        Console.ForegroundColor = before;
    }

    public static bool IsTypeOfApiScript(Type t)
    {
        return t.IsAssignableTo(typeof(ApiScript)) && !t.IsAbstract && !t.IsInterface && t != typeof(ApiScript);
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

    public static string GetDoc(this PropertyInfo t)
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

    public static string GetObjectName(this Type type, string name) => $"{type.GetName()}.{name}";

    public static string GetName(this Type t)
    {
        var cas = t.GetCustomAttributes<ApiAttribute>().ToList();
        if (cas.Any()) return cas[0].overriderName;

        var tName = t.Name;
        if (!t.IsGenericType)
        {
            return tName switch
            {
                "Single" => "Float",
                "Int32" => "Int",
                "Int64" => "Long",
                _ => tName
            };
        }

        if (tName.Contains('`')) tName = tName[..tName.IndexOf('`')];

        return $"{tName}<{string.Join(", ", t.GenericTypeArguments.Select(t => t.GetName()))}>";
    }

    public static string GetName(this PropertyInfo t)
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