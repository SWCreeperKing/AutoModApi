using System.Reflection;
using System.Text;
using AutoModApi.Attributes.Documentation;
using static AutoModApi.Api;

namespace AutoModApi;

public sealed class Documentary
{
    public static void PrintDocumentation(string path, string notes = "")
    {
        var bindAtt = BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly;
        var file = $"{path}/README.md";
        StringBuilder mdBuilder = new();
        mdBuilder.Append($"# Coconut Script (.cns) Documentation for {projectName}\n\n");
        mdBuilder.Append("## Table of Contents\n\n----\n\n");

        var enums = Assembly.GetEntryAssembly()!.GetTypes().Where(t => t.IsEnum)
            .Where(t => t.GetCustomAttributes<EnumDocAttribute>().Any()).ToList();

        var names = ReadableTypes.Where(t => !DocExcludeTypes.Contains(t)).Concat(enums).Select(Api.GetName).ToArray();

        string GetTypeLink(Type t)
        {
            var name = t.GetName();
            if (names.Any() && names.Contains(name)) return $"[{name}](#{name.ToLower()})";
            foreach (var rawName in names.Where(s => name.Contains(s, StringComparison.CurrentCultureIgnoreCase)))
            {
                name = name.Replace(rawName, $"[{rawName}](#{rawName.ToLower()})",
                    StringComparison.CurrentCultureIgnoreCase);
            }

            return name;
        }

        foreach (var name in names) mdBuilder.Append($"- [{name}](#{name.ToLower()})\n");
        if (notes != "")
        {
            mdBuilder.Append("- [Notes](#notes)\n\n## Notes\n\n");
            mdBuilder.Append("----\n\n[Back to Top](#table-of-contents)\n");
            mdBuilder.Append($"\n```\n{notes}\n```\n");
        }

        foreach (var t in ReadableTypes)
        {
            if (DocExcludeTypes.Contains(t)) continue;
            var name = t.GetName();
            mdBuilder.Append($"\n## {name}\n\n");
            var doc = t.GetDoc();
            if (doc != "") mdBuilder.Append($"{doc}\n\n");
            mdBuilder.Append("----\n\n[Back to Top](#table-of-contents)\n");

            var tProperties = t.GetProperties(bindAtt);
            if (tProperties.Any())
            {
                mdBuilder.Append($"\n### {name} Properties\n\n");
                foreach (var property in tProperties)
                {
                    if (property.GetCustomAttributes<DocIgnoreAttribute>().Any()) continue;
                    mdBuilder.Append($"- {GetTypeLink(property.PropertyType)} `{property.GetName()}`");
                    if (property.SetMethod is null || property.SetMethod.IsPrivate) mdBuilder.Append(" **unsettable**");
                    if (property.GetMethod is null || property.GetMethod.IsPrivate) mdBuilder.Append(" **unreadable**");
                    mdBuilder.Append('\n');

                    var fDoc = property.GetDoc();
                    if (fDoc != "") mdBuilder.Append($"  > {fDoc}\n");
                }
            }

            var tFields = t.GetFields(bindAtt);
            if (tFields.Any())
            {
                mdBuilder.Append($"\n### {name} Fields\n\n");
                foreach (var field in tFields)
                {
                    if (field.GetCustomAttributes<DocIgnoreAttribute>().Any()) continue;
                    mdBuilder.Append($"- {GetTypeLink(field.FieldType)} `{field.GetName()}`\n");
                    var fDoc = field.GetDoc();
                    if (fDoc != "") mdBuilder.Append($"  > {fDoc}\n");
                }
            }

            var tMethods = t.GetMethods(bindAtt).Where(mi => !mi.IsSpecialName);
            if (!tMethods.Any()) continue;
            mdBuilder.Append($"\n### {name} Methods\n\n");
            foreach (var method in tMethods)
            {
                if (method.GetCustomAttributes<DocIgnoreAttribute>().Any()) continue;
                var parameters = method.GetParameters();
                var mName = method.GetName();
                var para = GlobalPool.ContainsKey(name)
                    ? GlobalPool[name].ContainsKey(mName) ? GlobalPool[name][mName] : null
                    : null;

                mdBuilder.Append($"- {GetTypeLink(method.ReturnType)} `{mName}`");

                if (para is not null)
                {
                    mdBuilder.Append('(');
                    var fields = para.GetProperties();
                    for (var i = 0; i < fields.Length; i++)
                    {
                        var field = fields[i];
                        mdBuilder.Append($"{GetTypeLink(field.PropertyType)} {field.Name}");
                        if (i < fields.Length - 1) mdBuilder.Append(", ");
                    }

                    mdBuilder.Append(")\n");
                }
                else if (parameters.Any())
                {
                    mdBuilder.Append(
                        $"({string.Join(", ", parameters.Select(t => $"{GetTypeLink(t.ParameterType)} `{t.GetName()}`"))})\n");
                }
                else mdBuilder.Append("()\n");

                var mDoc = method.GetDoc();
                if (mDoc != "") mdBuilder.Append($"  > {mDoc}\n");

                if (para is null && !parameters.Any()) continue;
                foreach (var paraInfo in parameters)
                {
                    var pDoc = paraInfo.GetDoc();
                    if (pDoc == "") continue;
                    mdBuilder.Append(
                        $"  - Parameter: {GetTypeLink(paraInfo.ParameterType)} `{paraInfo.GetName()}`\n    > {paraInfo.GetDoc()}\n");
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