using System.Runtime.CompilerServices;
using Tests.Utilities;

namespace Tests.Rutracker;

public static class SearchResultValidation
{
    public static bool ValidateSearchResultMatches(this SearchResultItem result, Story topic)
    {
        if (topic.Author != null)
        {
            if (!CompareAuthors(
                    SanitizeAuthor(result.Author), 
                    SanitizeAuthor(topic.Author)))
            {
                return false;
            }
        }

        if (topic.Series != null && result.SeriesName != null)
        {
            if (!Compare(topic.Series, result.SeriesName))
                return false;
        }

        var validateSearchResultMatches = CompareTitles(result, topic);
        return validateSearchResultMatches;
    }

    private static bool CompareTitles(SearchResultItem result, Story topic)
    {
        var title = SearchUtilities.GetTitle(topic.Title!,
            topic.Series ?? result.SeriesName);
        var s = result.Title;
        return Compare(s, title);
    }

    private static bool Compare(string x, string y)
    {
        var xx = ScrapeTopic(x);
        var yy = ScrapeTopic(y);
        return xx.Contains(yy) || yy.Contains(xx);
    }

    private static string ScrapeTopic(string s) => s
        .ToLower()
        .Replace('ё', 'e')
        .Replace('й', 'и')
        .Replace('й', 'и')
        .Replace('е', 'e')
        .Replace('c', 'с') //русский..topic
        .Replace('а', 'a') //русский..topic
        .Replace('о', 'o') //русский..topic
        .Replace('х', 'x') //русский..topic
        .Replace('–', '-') //русский..topic
        .Replace("«", "")
        .Replace("»", "")
        .Replace("\"", "")
        .Replace(",", " ")
        .Replace(":", " ")
        .Replace("!", " ")
        .Replace("?", " ")
        .Replace("-", " ")
        .Replace("  ", " ")
        .Replace("  ", " ")
        .Split(' ', '.', '-')
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .StrJoin(" ");

    private static string SanitizeAuthor(string s) => s
        .Replace("M", "М")
        .Replace("P", "Р")
        .Replace("T", "Т")
        .Replace("H", "Н")
        .Replace("B", "В")
        .ToLower()
        .Replace("&", ",")
        .Replace("_", " ")
        .Replace('ё', 'e')
        .Replace('е', 'e')
        .Replace('c', 'с') //русский..topic
        .Replace('а', 'a') //русский..topic
        .Replace('о', 'o') //русский..topic
        .Replace('х', 'x') //русский..topic
        .Replace("(", " ")
        .Replace(")", " ")
        .Replace("[", " ")
        .Replace("]", " ");


    private static bool CompareAuthors(string formal, string manual)
    {
        if (formal.Contains(',') || manual.Contains(','))
        {
            foreach (var f in formal.Split(',', StringSplitOptions.RemoveEmptyEntries))
            foreach (var m in manual.Split(',', StringSplitOptions.RemoveEmptyEntries))
                if (CompareAuthors(f, m))
                    return true;
        }

        var ff = formal.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToHashSet();
        var mm = manual
            // for the case like "Змагаевы Алекс и Ангелина"
            .Replace(" и ", " ")
            .Split(new[] { ' ', '.' }, StringSplitOptions.RemoveEmptyEntries)
            .ToList();

        bool Eq(string x, string y)
        {
            if (y.StartsWith(x)) return true;
            if (y.Length < x.Length) return Eq(y, x);
            // Змагаевы -> Змагаев     
            if (x == y.TrimEnd('ы', 'и')) return true;
            // Стругацкий -> Стругацкие     
            if (x.Length > 3 && x[..^1] == y[..^1]) return true;
            return false;
        }

        bool IsSubset(IEnumerable<string> a, ICollection<string> b) =>
            a.All(word => b.Any(x => Eq(x, word)));

        if (IsSubset(ff, mm) || IsSubset(mm, ff))
            return true;
        return false;
    }

}