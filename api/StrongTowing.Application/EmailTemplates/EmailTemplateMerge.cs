using System.Text.RegularExpressions;

namespace StrongTowing.Application.EmailTemplates;

public static partial class EmailTemplateMerge
{
    public static string Apply(string template, IReadOnlyDictionary<string, string> mergeFields)
    {
        if (string.IsNullOrEmpty(template))
            return string.Empty;

        return MyRegex().Replace(template, m =>
        {
            var key = m.Groups[1].Value;
            if (mergeFields.TryGetValue(key, out var val))
                return val;
            return string.Empty;
        });
    }

    [GeneratedRegex(@"\{\{\s*([a-zA-Z0-9_]+)\s*\}\}")]
    private static partial Regex MyRegex();
}
