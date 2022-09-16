using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using RegExtract;
using Tests.Utilities;

namespace Tests.Rutracker;


public record Header(int Id);

public abstract record Topic;

public sealed record Story(
    int TopicId,
    string? Title,
    string? Author,
    string? Performer,
    string? Year,
    string? Series,
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
        var author = (s + " " + f).Trim();
        if (string.IsNullOrWhiteSpace(author))
            author = null;
        var htmlNode = post.FindTag("Исполнитель") ??
                       post.FindTag("Исполнители") ??
                       post.FindTag("Исполнитель и звукорежиссёр");

        var performer = htmlNode?.TagValue();
        var rawSeries = (post.FindTag("Цикл") ??
                        post.FindTag("Цикл/серия"))?.TagValue();
        if (rawSeries != null && string.IsNullOrWhiteSpace(rawSeries))
            rawSeries = "<YES>";
        var series = rawSeries ?? Mmm(post);

        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();

        if (title == null)
            return null;

        if (title == "Рассказы")
            return null;
        
        if (title.Contains('['))
        {
            var trim = Regex.Replace(title, "\\[.*\\]", "").Trim();
            title = trim != "" ? trim : title.TrimStart('[').TrimEnd(']');
        }

        if (title.Contains('('))
            title = Regex.Replace(title, "\\(.*\\)", "").Trim();
        if (title.StartsWith("Рассказ"))
            title = title["Рассказ".Length..].Trim(' ', '\"');
        if (series != null && title.Contains(series) && title != series && title != series+".")
        {
            title = Regex.Replace(title, series + " (\\d)*\\.", "").Trim();
            title = title.Replace(series + ".", "").Trim();
        }

        if (string.IsNullOrWhiteSpace(title))
            throw new Exception();

        if (author != null)
        {
            if (title.Contains(f + " " + s))
                title = title.Replace(f + " " + s, "").Trim('-', '–', ' ');
            else if (title.Contains(s + " " + f))
                title = title.Replace(s + " " + f, "").Trim('-', '–',  ' ');
        }

        return new Story(topicId, title, author,
            performer, year, series, genre, playTime);
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