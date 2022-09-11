using System.Reflection;
using System.Text;
using AutoModApi.Attributes.Documentation;
using static AutoModApi.Api;

namespace AutoModApi;

public static class Documentary
{
    public static void PrintDocumentation(string path)
    {
        var file = $"{path}/README.md";
        StringBuilder mdBuilder = new();
        mdBuilder.Append($"# Coconut Script (.cns) Documentation for {projectName}\n\n");
        mdBuilder.Append("## Table of Contents\n\n----\n\n");

        var enums = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.IsEnum)
            .Where(t => t.GetCustomAttributes<EnumDocAttribute>().Any()).ToList();

        var names = ReadableTypes.Concat(enums).Select(Api.GetName).ToArray();

        string GetTypeLink(Type t)
        {
            var name = t.GetName();
            if (names.Any() && names.Contains(name)) return $"[{name}](#{name.ToLower()})";
            return name;
        }

        foreach (var name in names) mdBuilder.Append($"- [{name}](#{name.ToLower()})\n");

        foreach (var t in ReadableTypes)
        {
            var name = t.GetName();
            mdBuilder.Append($"\n## {name}\n\n");
            var doc = t.GetDoc();
            if (doc != "") mdBuilder.Append($"{doc}\n\n");
            mdBuilder.Append($"----\n\n[Back to Top](#table-of-contents)\n\n### {name} Fields\n\n");

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
                var useOverride = method.GetCustomAttributes<ArgumentOverrideAttribute>().Any();
                var parameters = method.GetParameters();
                var mName = method.GetName();
                var para = GlobalPool.ContainsKey(name)
                    ? GlobalPool[name].ContainsKey(mName) ? GlobalPool[name][mName] : null
                    : null;

                mdBuilder.Append($"- {GetTypeLink(method.ReturnType)} `{mName}`");
                mdBuilder.Append('(');

                if (parameters.Any() && useOverride)
                {
                    mdBuilder.Append(
                        $"({string.Join(", ", parameters.Select(t => $"{GetTypeLink(t.ParameterType)} `{t.GetName()}`"))})");
                }
                else if (para is not null && !useOverride)
                {
                    var fields = para.GetProperties();
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        mdBuilder.Append($"{GetTypeLink(field.PropertyType)} {field.Name}");
                        if (i < fields.Length - 1) mdBuilder.Append(", ");
                    }
                }

                mdBuilder.Append(')');

                mdBuilder.Append('\n');

                var mDoc = method.GetDoc();
                if (mDoc != "") mdBuilder.Append($"  - {mDoc}\n");

                if (para is null || (useOverride && !parameters.Any())) continue;
                var properties = para.GetConstructors().First().GetParameters();
                if (useOverride)
                {
                    foreach (var paraInfo in parameters)
                    {
                        var pDoc = paraInfo.GetDoc();
                        if (pDoc == "") continue;
                        mdBuilder.Append(
                            $"  - Parameter: {GetTypeLink(paraInfo.ParameterType)} `{paraInfo.GetName()}`\n    - {paraInfo.GetDoc()}\n");
                    }
                }
                else
                {
                    foreach (var prop in properties)
                    {
                        var pDoc = prop.GetDoc();
                        if (pDoc == "") continue;
                        mdBuilder.Append(
                            $"  - Parameter: {GetTypeLink(prop.ParameterType)} `{prop.Name}`\n    - {pDoc}\n");
                    }
                }
            }
        }

        if (enums.Any())
        {
            foreach (var e in enums)
            {
                mdBuilder.Append($"\n## {e.GetName()}");
                var eDoc = e.GetDoc();
                mdBuilder.Append("\n\n(Enum)");
                if (eDoc != "") mdBuilder.Append($": {eDoc}");
                mdBuilder.Append("\n\n----\n\n[Back to Top](#table-of-contents)\n\n");

                var values = e.GetEnumNames();
                var longest = Math.Max(values.Aggregate(0, (i, s) => Math.Max(i, s.Length)), 6) + 2;
                values = values.Select(s => $"|`{s}`{" ".Repeat(longest - s.Length)}|\n").ToArray();

                mdBuilder.Append($"|Values  {" ".Repeat(longest - 6)}|\n");
                mdBuilder.Append($"|:--{"-".Repeat(longest - 1)}|\n");
                foreach (var v in values) mdBuilder.Append(v);
            }
        }

        using var sw = File.CreateText(file);
        sw.Write(mdBuilder);
    }
}