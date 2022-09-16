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
    public static int ParseInt(this string s)
    {
        return !int.TryParse(s, out var result)
            ? throw new Exception($"Words count '{s}' is not a valid int")
            : result;
    }
    public static int? ParseIntOrNull(this string s)
    {
        return int.TryParse(s, out var result) ? result : null;
    }
    public static string? RemovePostfix(this string str, string postfix)
    {
        return str.EndsWith(postfix) ? str[..^postfix.Length] : null;
    }
}