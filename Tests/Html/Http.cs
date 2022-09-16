using System.Net;
using System.Text;

namespace Tests.Html;

public class Http
{
    private readonly HtmlCache _cache;
    private readonly HttpClient _client;
    private readonly Encoding _encoding;

    public Http(HtmlCache cache, Encoding encoding)
    {
        _cache = cache;
        _encoding = encoding;
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

        };
        _client = new HttpClient(handler);
    }

    public async Task<string> Get(string key, string localUri)
    {
        var cachedValue = await _cache.TryGetValue(key);
        if (cachedValue != null) return cachedValue;
        var message = new HttpRequestMessage(HttpMethod.Get, localUri)
        {
            Headers =
            {
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0" },
                { "Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8" },
                { "Accept-Language", "en-US,en;q=0.5" },
                { "Referer", "https://readli.net/lyudi-i-bogi-tom-1/" },
                { "Connection", "keep-alive" },
                { "Cookie", "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D" },
                { "Upgrade-Insecure-Requests", "1" },
                { "Sec-Fetch-Dest", "document" },
                { "Sec-Fetch-Mode", "navigate" },
                { "Sec-Fetch-Site", "same-origin" },
            },
        };

        var result = await _client.SendAsync(message);
        if ((int)result.StatusCode == 404)
            throw new Exception("Page is not found");
        if ((int)result.StatusCode >= 400)
            throw new Exception("Bad request");
        if ((int)result.StatusCode >= 500)
            throw new Exception("Server is having troubles. Come later");
        var content = result.IsSuccessStatusCode
            ? _encoding.GetString(await result.Content.ReadAsByteArrayAsync())
            : $"<{result.StatusCode}/>";
        await _cache.SaveValue(key, content);
        return content;
    }
}
