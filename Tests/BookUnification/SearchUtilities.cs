using System.Text.RegularExpressions;

namespace Tests.BookUnification;

public static class SearchUtilities
{
    public static string GetTitle(string title, string? series)
    {
        if (series == null)
            return title;

        return Regex.Replace(title,
            series + "\\s*(:?|-)\\s*\\d+(\\.|:)\\s*", "",
            RegexOptions.IgnoreCase);
    }
}