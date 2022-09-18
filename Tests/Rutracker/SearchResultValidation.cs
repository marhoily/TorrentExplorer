using Tests.Utilities;

namespace Tests.Rutracker;

public static class SearchResultValidation
{
    public static bool ValidateSearchResultMatches(this SearchResult result, Story topic)
    {
        if (topic.Author != null)
        {
            if (!CompareAuthors(
                    SanitizeAuthor(result.Author), 
                    SanitizeAuthor(topic.Author)))
            {
                Console.WriteLine(result.Author + " != " + topic.Author);
                return false;
            }
        }

        var resultTitle = ScrapeTopic(result.Title);
        var title = SearchUtilities.GetTitle(topic.Title!, 
            topic.Series ?? result.SeriesName);
        var topicTitle = ScrapeTopic(title);

        if (resultTitle.Contains(topicTitle) ||
            topicTitle.Contains(resultTitle))
            return true;
        
        Console.WriteLine(result.Title + " != " + topic.Title);
        return false;
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
        .Replace(",", ", ")
        .Replace("  ", " ")
        .Split(' ', '.', '-')
        .Where(c => !string.IsNullOrWhiteSpace(c))
        .StrJoin(" ");

    private static string SanitizeAuthor(string s) => s
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
            if (x == y) return true;
            if (y.Length < x.Length) return Eq(y, x);
            // Змагаевы -> Змагаев     
            if (x == y.TrimEnd('ы', 'и')) return true;
            // Стругацкий -> Стругацкие     
            if (x.Length > 3 && x[..^1] == y[..^1]) return true;
            if (x.Length == 1 && x[0] == y[0]) return true;
            return false;
        }

        bool IsSubset(IEnumerable<string> a, ICollection<string> b) =>
            a.All(word => b.Any(x => Eq(x, word)));

        if (IsSubset(ff, mm) || IsSubset(mm, ff))
            return true;
        return false;
    }

}