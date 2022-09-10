namespace AutoModApi;

public static class Replacer
{
    public static Dictionary<string, Func<string, string>> replacer = new()
    {
        {
            "print", s =>
            {
                var ss = s[6..];
                return $"Console.WriteLine({(ss.IsSurroundedByQuote() ? ss : $"\"{ss}\"")});";
            }
        }
    };

    public static bool IsSurroundedByQuote(this string s) =>
        (s.StartsWith("\"") || s.StartsWith("$\"")) && s.EndsWith("\"");
}