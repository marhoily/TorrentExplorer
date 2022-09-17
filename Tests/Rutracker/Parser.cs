using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RegExtract;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Rutracker;

public record Header(int Id);

public abstract record Topic;

public sealed record Story(
    int TopicId,
    string Url,
    string? Title,
    string? Author,
    string? Performer,
    string? Year,
    string? Series,
    string? NumberInSeries,
    string? Genre,
    string? PlayTime) : Topic;

public sealed record Series : Topic;

public static class Parser
{
    public static Header[] ParseRussianFantasyHeaders(this HtmlNode node)
    {
        return node.SelectNodes("//tr[@class='hl-tr']/td[1]")
            .Select(n => new Header(n.ParseIntAttribute("id")))
            .ToArray();
    }

    public static HtmlNode GetForumPost(this HtmlNode node)
    {
        return node.SelectSingleNode("//div[@class='post_body']");
    }

    public static Topic? ParseRussianFantasyTopic(this HtmlNode node)
    {
        var attributeValue = node.FirstChild.GetAttributeValue("data-ext_link_data", null);
        var jObject = JObject.Parse(attributeValue);
        var topicId = jObject["t"]!.Value<int>();

        var post = node.SelectSingleNode("//div[@class='post_body']");
        if (post.FindTags("Год выпуска").Count() > 1)
            return new Series();
        var title =
            (post.SelectSingleNode("//span[@style='font-size: 24px; line-height: normal;']") ??
             post.SelectSingleNode("//span[@style='font-size: 23px; line-height: normal;']") ??
             post.SelectSingleNode("//span[@style='font-size: 28px; line-height: normal;']") ??
             post.SelectSingleNode("//span[@style='font-size: 27px; line-height: normal;']") ??
             post.SelectSingleNode("//span[@style='font-size: 26px; line-height: normal;']"))?.InnerText;

        var year = post.FindTag("Год выпуска")?.TagValue().TrimEnd('.', 'г');

        var s = (post.FindTag("Фамилия автора") ??
                 post.FindTag("Фамилии авторов") ??
                 post.FindTag("Aвтор") ?? // different "A"?
                 post.FindTag("Автор") ??
                 post.FindTag("Автора") ??
                 post.FindTag("Авторы"))?.TagValue().Trim();
        var f = (post.FindTag("Имя автора") ??
                 post.FindTag("Имена авторов"))?.TagValue().Trim();
        var htmlNode = post.FindTag("Исполнитель") ??
                       post.FindTag("Исполнители") ??
                       post.FindTag("Исполнитель и звукорежиссёр");

        var performer = htmlNode?.TagValue();
        var rawSeries = (post.FindTag("Цикл") ??
                         post.FindTag("Цикл/серия"))?.TagValue();
        if (rawSeries != null && string.IsNullOrWhiteSpace(rawSeries))
            rawSeries = "<YES>";
        var series = rawSeries ?? Mmm(post);

        var numberInSeries = post.FindTag("Номер книги")?.TagValue(); 

        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();

        if (title == null)
            return null;

        title = title.Trim(' ', '•');
        
        if (title == "Рассказы" || title.TrimEnd('.').EndsWith(". Рассказы"))
            return null;

        if (title.StartsWith("\"") && title.EndsWith("\""))
            title = title.Trim('\"');

        if (title.Contains('['))
        {
            var trim = Regex.Replace(title, "\\[.*\\]", "").Trim();
            title = trim != "" ? trim : title.TrimStart('[').TrimEnd(']');
        }

        if (title.Contains('('))
            title = Regex.Replace(title, "\\(.*\\)", "").Trim();
        if (title.StartsWith("Рассказ"))
            title = title["Рассказ".Length..].Trim(' ', '\"');


        if (topicId == 6239060)
            1.ToString();
        title = RemoveSeriesPrefixFromTitle(title, series);
        title = RemoveAuthorPrefixFromTitle(title, f, s);

        var author = CombineAuthors(s, f);
        return new Story(topicId,
            $"https://rutracker.org/forum/viewtopic.php?t={topicId}",
            title, author, performer, year, series, numberInSeries,
            genre, playTime);
    }

    private static string? CombineAuthors(string? s, string? f)
    {
        if (string.IsNullOrWhiteSpace(s+f))
            return null;
        var a = s?.Trim().Replace(";", ",");
        var b = f?.Trim().Replace(";", ",");
        if (a == null) return b;
        if (b == null) return a;
        if (a.Contains(',') && b.Contains(','))
            return a.Split(',', RemoveEmptyEntries).Zip(b.Split(',', RemoveEmptyEntries))
                .Select(x => x.First.Trim('_').Trim() + " " + x.Second.Trim('_').Trim())
                .StrJoin();
        return a + " " + b;
    }

    private static string RemoveAuthorPrefixFromTitle(string title, string? f, string? s)
    {
        var o1 = f + " " + s;
        if (string.IsNullOrWhiteSpace(o1)) return title;
        
        if (title.Contains(o1))
            return title.Replace(o1, "").Trim('-', '–', ' ');
        
        var o2 = s + " " + f;
        if (title.Contains(o2))
            return title.Replace(o2, "").Trim('-', '–', ' ');

        return title;
    }

    private static string RemoveSeriesPrefixFromTitle(string title, string? series)
    {
        if (series != null)
        {
            if (title.Contains(series) && title != series && title != series + ".")
            {
                title = Regex.Replace(title, series + " (\\d)*(\\.|,)", "").Trim();
                title = Regex.Replace(title, series + "-(\\d)*(\\.|,)", "").Trim();
                title = title.Replace(series + ".", "").Trim();
            }
        }
        else
        {
            var idx = title.IndexOf(" серия ", StringComparison.InvariantCulture);
            if (idx != -1)
                title = title[..idx].Trim('.', '-').Trim();
        }

        if (string.IsNullOrWhiteSpace(title))
            throw new Exception();
        return title;
    }

    private static string? Mmm(HtmlNode post)
    {
        var singleOrDefault = post.GetSpoilers()
            .SingleOrDefault(s => s.Header.Contains("Цикл"));
        if (singleOrDefault == null) return null;
        var replace = singleOrDefault
            .Header
            .Replace("Цикл/серия", "Цикл")
            .Replace("Цикл книг", "Цикл")
            .Replace("&#34;", "ξ")
            .Replace('<', 'ξ')
            .Replace('>', 'ξ')
            .Replace('«', 'ξ')
            .Replace('»', 'ξ');
        if (replace.Trim() == "Цикл")
            return "<PRESENT>";
        if (!replace.Contains("ξ"))
        {
            var s = replace["Цикл".Length..];
            return string.IsNullOrWhiteSpace(s) ? null : s;
        }

        return replace.Extract<string>(@"Цикл ξ(.*)ξ");
    }
}