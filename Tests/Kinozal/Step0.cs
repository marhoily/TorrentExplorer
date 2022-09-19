using System.Text;
using Tests.Html;
using Tests.Utilities;

namespace Tests.Kinozal;

public sealed record KinozalBook(KinozalForumPost Post, string? Series);

public class Step0
{
    private readonly Http _http;
    private readonly Http _httpUtf8;
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\kinozal\step0.xml";

    static Step0()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public Step0()
    {
        var htmlCache = new HtmlCache(CacheLocation.Temp, CachingStrategy.Normal);
        _http = new Http(htmlCache, Encoding.GetEncoding(1251));
        _httpUtf8 = new Http(new SqliteCache(CachingStrategy.Normal), Encoding.Default);
    }

    [Fact]
    public async Task DownloadRawHtml()
    {
        var htmlNodes = await GetKinozalForumPosts();
        await Output.SaveJson(htmlNodes);
    }

    private async Task<KinozalBook[]> GetKinozalForumPosts()
    {
        var headerPages = await Task.WhenAll(Enumerable.Range(0, 60)
            .Select(async i =>
            {
                var page = await _http.DownloadKinozalFantasyHeaders(i);
                return page.ParseKinozalFantasyHeaders();
            }));


        var headers = headerPages.SelectMany(p => p)
            .Select(async header =>
            {
                var topic = await _http.DownloadKinozalFantasyTopic(header);
                var post = topic.GetKinozalForumPost();
                if (post.SeriesId == null) return new KinozalBook(post, null);
                var html = await _httpUtf8.Get(
                    "http://kinozal.tv/get_srv_details.php?" +
                    $"id={post.Id}&pagesd={post.SeriesId}");
                return new KinozalBook(post, html);
            });

        return await Task.WhenAll(headers);
    }
}