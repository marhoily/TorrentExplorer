using System.Net;
using System.Text;

namespace Tests.Html;

public class Http
{
    private readonly IHtmlCache _cache;
    private readonly HttpClient _client;
    private readonly Encoding _encoding;

    public Http(IHtmlCache cache, Encoding encoding)
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

    public async Task<string> Get(string localUri) =>
        await Get(localUri, localUri);
    public async Task<string> Get(Uri uri, HashSet<int>? errorsAsNull = default) =>
        await Get(uri.ToString(), uri.ToString(), errorsAsNull);

    public async Task<string> Get(string key, string localUri, HashSet<int>? errorsAsNull = default)
    {
        var cachedValue = await _cache.TryGetValue(key);
        if (cachedValue != null) return cachedValue;
        var message = new HttpRequestMessage(HttpMethod.Get, localUri);
        var result = await _client.SendAsync(message);
        if (errorsAsNull?.Contains((int)result.StatusCode) == true)
        {
            var r = $"<Error>{result.StatusCode}<Error>";
            await _cache.SaveValue(key, r);
            return r;
        }
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

    public async Task<string> Get(HttpRequestMessage message) => 
        await Get(message.RequestUri!.ToString(), message);


    public async Task<string> Get(string key, HttpRequestMessage message)
    {
        var cachedValue = await _cache.TryGetValue(key);
        if (cachedValue != null) return cachedValue;

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