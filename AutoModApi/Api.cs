using System.Reflection;
using AutoModApi.Attributes.Api;
using AutoModApi.Attributes.Documentation;
using Microsoft.CodeAnalysis.Scripting;

namespace AutoModApi;

public static class Api
{
    public static readonly List<Type> ReadableTypes = new();
    public static readonly List<Type> DocExcludeTypes = new();
    public static readonly Dictionary<string, Type> TypeDictionary = new();
    public static readonly Dictionary<string, Dictionary<string, Type>> GlobalPool = new();
    public static readonly Dictionary<string, Dictionary<string, Script>> ObjectPool = new();

    public static float CompilationPercent { get; private set; }

    public static string projectName = "Untitled App";
    public static ICompiler interpretCompiler = new DefaultCompiler();

    private static readonly Action<float> _setPercent = f => CompilationPercent = f;
    private static readonly List<Action<string>> _onRegister = new();
    private static readonly List<string> _toRegister = new();

    public static event Action<string> OnRegister
    {
        add => _onRegister.Add(value);
        remove => _onRegister.Remove(value);
    }

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
        if (ReadableTypes.Contains(t)) return;
        if (t.BaseType != typeof(ApiScript) || t == typeof(ApiScript)) return;
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

    public static void ReadFile(string file) => _toRegister.Add(file);

    public static T CreateType<T>(string name) where T : ApiScript
    {
        var typeName = typeof(T).GetName();
        var objName = $"{typeName}.{name}";
        if (!ObjectPool.ContainsKey(objName)) throw new ArgumentException($"Type [{objName}] does not exist");
        var instanced = Activator.CreateInstance<T>();
        instanced.scripts = ObjectPool[$"{typeName}.{name}"];
        return instanced;
    }

    public static T CreateType<T>(string name, params object[] parameters) where T : ApiScript
    {
        var typeName = typeof(T).GetName();
        var objName = $"{typeName}.{name}";
        if (!ObjectPool.ContainsKey(objName)) throw new ArgumentException($"Type [{objName}] does not exist");
        var instanced = (T) Activator.CreateInstance(typeof(T), parameters)!;
        instanced.scripts = ObjectPool[$"{typeName}.{name}"];
        return instanced;
    }

    public static void Compile()
    {
        void AddToDict(string[] file)
        {
            var add = interpretCompiler.Compile(file, out var continuation);
            if (add is null) return;
            var addV = add.Value;
            ObjectPool.Add(addV.Key, addV.Value);
            foreach (var o in _onRegister) o.Invoke(addV.Key);
            if (continuation.Any()) AddToDict(continuation);
        }

        Task.Run(async () =>
        {
            CompilationPercent = 0;
            var files = _toRegister.Count;

            for (var i = 0; i < files; i++)
            {
                CompilationPercent = (i + .01f) / files;
                List<string> lines = new();
                using StreamReader sr = new(_toRegister[i]);
                while (!sr.EndOfStream)
                {
                    var line = await sr.ReadLineAsync();
                    if (line is null or "") continue;
                    lines.Add(line.Replace("\r", "").Trim());
                }

                CompilationPercent = (i + .5f) / files;

                AddToDict(lines.ToArray());
            }

            CompilationPercent = 1;
        });
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