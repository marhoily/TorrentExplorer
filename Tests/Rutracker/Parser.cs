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
    string? AuthorSecondName,
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

    public static Topic ParseRussianFantasyTopic(this HtmlNode node)
    {
        var attributeValue = node.FirstChild.GetAttributeValue("data-ext_link_data", null);
        var jObject = JObject.Parse(attributeValue);
        var topicId = jObject["t"]!.Value<int>();

        var post = node.SelectSingleNode("//div[@class='post_body']");
        if (post.FindTags("Год выпуска").Count() > 1)
            return new Series();
        var title =
            post.SelectSingleNode("//span[@style='font-size: 24px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 23px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 28px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 27px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 26px; line-height: normal;']");

        var year = post.FindTag("Год выпуска")?.TagValue().TrimEnd('.', 'г');

        var author = post.FindTag("Фамилия автора") ??
                     post.FindTag("Фамилии авторов") ??
                     post.FindTag("Автор") ??
                     post.FindTag("Автора") ??
                     post.FindTag("Авторы");
        var htmlNode = post.FindTag("Исполнитель") ??
                       post.FindTag("Исполнители") ??
                       post.FindTag("Исполнитель и звукорежиссёр");

        var performer = htmlNode?.TagValue();
        var series = (post.FindTag("Цикл") ??
                      post.FindTag("Цикл/серия"))?.TagValue() ??
                     Mmm(post);

        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();
        return new Story(topicId, title?.InnerText, author?.TagValue(),
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