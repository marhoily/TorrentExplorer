using System.Net;
using System.Text;

namespace Tests;

public class Http
{
    private readonly HtmlCache _cache;
    private readonly HttpClient _client;
    private static readonly Encoding Encoding;

    public Http(HtmlCache cache)
    {
        _cache = cache;
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

        };
        _client = new HttpClient(handler);
    }

    static Http()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        Encoding = Encoding.GetEncoding(1251);
    }

    public async Task<string> Get(string key, string localUri)
    {
        var cachedValue = await _cache.TryGetValue(key);
        if (cachedValue != null) return cachedValue;
        var message = new HttpRequestMessage(HttpMethod.Get, localUri);

        var result = await _client.SendAsync(message);
        if ((int)result.StatusCode == 404)
            throw new Exception("Page is not found");
        if ((int)result.StatusCode >= 400)
            throw new Exception("Bad request");
        if ((int)result.StatusCode >= 500)
            throw new Exception("Server is having troubles. Come later");
        var content = result.IsSuccessStatusCode
            ? Encoding.GetString(await result.Content.ReadAsByteArrayAsync())
            : $"<{result.StatusCode}/>";
        await _cache.SaveValue(key, content);
        return content;
    }
}
