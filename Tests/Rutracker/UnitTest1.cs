using HtmlAgilityPack;
using RegExtract;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Rutracker;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        var http = new Http(htmlCache);
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await http.DownloadRussianFantasyHeaders(i);
                return page.ParseRussianFantasyHeaders();
            }));

        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await http.DownloadRussianFantasyTopic(header.Id);
                return topic.GetForumPost();
            });
        var htmlNodes = await Task.WhenAll(headers);
        await @"c:\temp\bulk.json".SaveJson(htmlNodes.Select(x => x.OuterHtml));
    }
}

public record Header(int Id);

public abstract record Topic;
public sealed record Story(string? Title,
    string? AuthorSecondName,
    string Performer,
    string Year,
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
        var post = node.SelectSingleNode("//div[@class='post_body']");
        if (post.FindTags("Год выпуска").Count() > 1)
            return new Series();
        var title =
            post.SelectSingleNode("//span[@style='font-size: 24px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 23px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 28px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 27px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 26px; line-height: normal;']");

        var year = post.FindTagB("Год выпуска").TagValue().TrimEnd('.', 'г');

        var author = post.FindTag("Фамилия автора") ??
                           post.FindTag("Фамилии авторов") ??
                           post.FindTag("Автор") ??
                           post.FindTag("Автора") ??
                           post.FindTag("Авторы");
        var htmlNode = post.FindTag("Исполнитель") ??
                       post.FindTag("Исполнители") ??
                       post.FindTagB("Исполнитель и звукорежиссёр");

        var performer = htmlNode.TagValue();
        var series = (post.FindTag("Цикл") ??
                      post.FindTag("Цикл/серия"))?.TagValue() ??
                     Mmm(post);

        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();
        return new Story(title?.InnerText, author?.TagValue(),
            performer, year, series, genre, playTime);
    }

    private static string? Mmm(HtmlNode post)
    {
        var replace = post.GetSpoilers()
            .SingleOrDefault(s => s.Header.Contains("Цикл"))?
            .Header
            .Replace('<', '«')
            .Replace('>', '»');
        return replace?.Extract<string>(@"Цикл «(.*)»");
    }
}
public static class KinozalParser
{
    public static int[] ParseKinozalFantasyHeaders(this HtmlNode node)
    {
        var rows = node.SelectNodes("//table[@class='t_peer w100p']/tr/td[2]/a").Skip(1);
        return rows
            .Select(n => n.Href(skipPrefix: "/details.php?id=")?.ParseIntOrNull())
            .OfType<int>()
            .ToArray();
    }

    public static HtmlNode GetKinozalForumPost(this HtmlNode node)
    {
        return node.SelectSingleNode("//div[@class='bx1 justify']");

    }
    public static Topic ParseRussianFantasyTopic(this HtmlNode node)
    {
        var post = node.SelectSingleNode("//div[@class='post_body']");
        if (post.FindTags("Год выпуска").Count() > 1)
            return new Series();
        var title =
            post.SelectSingleNode("//span[@style='font-size: 24px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 23px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 28px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 27px; line-height: normal;']") ??
            post.SelectSingleNode("//span[@style='font-size: 26px; line-height: normal;']");

        var year = post.FindTagB("Год выпуска").TagValue().TrimEnd('.', 'г');

        var author = post.FindTag("Фамилия автора") ??
                           post.FindTag("Фамилии авторов") ??
                           post.FindTag("Автор") ??
                           post.FindTag("Автора") ??
                           post.FindTag("Авторы");
        var htmlNode = post.FindTag("Исполнитель") ??
                       post.FindTag("Исполнители") ??
                       post.FindTagB("Исполнитель и звукорежиссёр");

        var performer = htmlNode.TagValue();
        var series = (post.FindTag("Цикл") ??
                      post.FindTag("Цикл/серия"))?.TagValue() ??
                     Mmm(post);

        var genre = post.FindTag("Жанр")?.TagValue();
        var playTime = post.FindTag("Время звучания")?.TagValue();
        return new Story(title?.InnerText, author?.TagValue(),
            performer, year, series, genre, playTime);
    }

    private static string? Mmm(HtmlNode post)
    {
        var replace = post.GetSpoilers()
            .SingleOrDefault(s => s.Header.Contains("Цикл"))?
            .Header
            .Replace('<', '«')
            .Replace('>', '»');
        return replace?.Extract<string>(@"Цикл «(.*)»");
    }
}