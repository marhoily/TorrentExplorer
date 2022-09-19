using System.Text.RegularExpressions;

namespace Tests.Utilities;

public static class StringExt
{
    public static string StrJoin<T>(this IEnumerable<T> values, string separator = ", ")
    {
        return string.Join(separator, values);
    }

    public static string StrJoin<T>(this IEnumerable<T> values,
        Func<T, string> selector, string separator = ", ")
    {
        return string.Join(separator, values.Select(selector));
    }

    public static string? NullifyWhenEmpty(this string x) => x == "" ? null : x;

    public static int ParseHtmlInt(this string s)
    {
        var m = s.Replace("&nbsp;", "").Replace(",", "");
        return !int.TryParse(m, out var result)
            ? throw new Exception($"Words count '{m}' is not a valid int")
            : result;
    }

    public static string Quote(this string s) => $"'{s}'";

    public static bool StartsOrEndsWith(this string s, char c) =>
        s.StartsWith(c) || s.EndsWith(c);

    public static string HtmlTrim(this string s) =>
        s.Replace("&nbsp;", " ").Trim();

    public static string Unquote(this string s, char quote = '\"')
    {
        return s.StartsWith(quote) && s.EndsWith(quote) ? s.Trim('\"') : s;
    }

    public static string Unbrace(this string input, char open, char close)
    {
        return input.StartsWith(open) && input.EndsWith(close)
            ? input.TrimStart(open).TrimEnd(close)
            : input;
    }

    public static string RemoveRegexIfItIsNotTheWholeString(this string input, string regex)
    {
        var result = Regex.Replace(input, regex, "").Trim();
        return string.IsNullOrWhiteSpace(result) ? input : result;
    }

    public static int ParseInt(this string s)
    {
        return !int.TryParse(s, out var result)
            ? throw new Exception($"Words count '{s}' is not a valid int")
            : result;
    }

    public static string CompressIfPossible(this string s)
    {
        var strings = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (strings.Length * 2 - 1 == s.Length)
            return string.Concat(strings);
        return s;
    }

    public static int ParseIntOrWord(this string s)
    {
        return int.TryParse(s, out var result)
            ? result
            : s switch
            {
                // книга..
                "первая" => 1,
                "вторая" => 2,
                "третья" => 3,
                _ => throw new Exception($"Words count '{s}' is not a valid int")
            };
    }

    public static int? TryParseIntOrWord(this string s)
    {
        return int.TryParse(s, out var result)
            ? result
            : s switch
            {
                // книга..
                "первая" => 1,
                "вторая" => 2,
                "третья" => 3,
                _ => null
            };
    }

    public static int? ParseIntOrNull(this string input)
    {
        return int.TryParse(input, out var result) ? result : null;
    }

    public static string TrimPostfix(this string input, string postfix)
    {
        return input.EndsWith(postfix) ? input[..^postfix.Length] : input;
    }

    public static string TrimPrefix(this string input, string prefix)
    {
        return input.StartsWith(prefix) ? input[prefix.Length..] : input;
    }
}