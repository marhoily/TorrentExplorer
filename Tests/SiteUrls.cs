using HtmlAgilityPack;
using Tests.Html;
using Tests.Utilities;

namespace Tests;

public static class SiteUrls
{
    private static readonly HashSet<int> ErrorsAsNull = new(){400};
    private const string Firefox = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:104.0) Gecko/20100101 Firefox/104.0";

    public static async Task<HtmlNode> DownloadKinozalFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get(
            $"kinozal/headers/{page:D3}",
            $"http://kinozal.tv/browse.php?c=2&page={page}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadRussianFantasyHeaders(this Http http, int page)
    {
        var html = await http.Get(
            $"rutracker/headers-en/{page:D3}",
            $"https://rutracker.org/forum/viewforum.php?f=1501&start={page * 50}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadKinozalFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get(
            $"kinozal/topics/{topicId:D8}",
            $"http://kinozal.tv/details.php?id={topicId}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> DownloadRussianFantasyTopic(this Http http, int topicId)
    {
        var html = await http.Get(
            $"rutracker/topics/{topicId:D8}",
            $"https://rutracker.org/forum/viewtopic.php?t={topicId}");
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> VseAudioknigiCom(this Http http, string localUrl)
    {
        var html = await http.Get(new Uri(
            new Uri("https://vse-audioknigi.com"),
            localUrl));
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> AudioknigaComUa(this Http http, string localUrl)
    {
        var uri = new Uri(new Uri("https://audiokniga.com.ua"), localUrl);
        var html = await http.Get(uri, ErrorsAsNull);
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> Knigorai(this Http http, string localUrl)
    {
        var uri = new Uri(new Uri("https://knigorai.com"), localUrl);
        var html = await http.Get(uri);
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> AbooksInfo(this Http http, string localUrl)
    {
        var uri = new Uri(new Uri("https://abooks.info"), localUrl);
        var html = await http.Get(uri);
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> MyBookRu(this Http http, string localUrl)
    {
        var uri = new Uri(new Uri("https://mybook.ru"), localUrl);
        var html = await http.Get(uri);
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> ReadliNet(this Http http, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://readli.net"), localUrl);
        const string cookie = "_ga=GA1.2.37066940.1663270926; _gid=GA1.2.330031539.1663270926; advanced-frontend=84uqtqjj4g54f915fkc8qu56v9; _csrf-frontend=33e0b2dbf8bf3fd887ebaa108b4fdbcead07599c3091d46862ebb5e5bcfa9b94a%3A2%3A%7Bi%3A0%3Bs%3A14%3A%22_csrf-frontend%22%3Bi%3A1%3Bs%3A32%3A%22TDtxxN2rcQlSLmpR4krXD2KkqW4zLe-L%22%3B%7D";
        var html = await http.Get(
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "User-Agent", Firefox },
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> FanlabRu(this Http http, string key, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://fantlab.ru"), localUrl);
        const string cookie = "_ym_uid=166312055796018485; _ym_d=1663120557; _ym_isad=1";
        var html = await http.Get(key,
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
    }

    public static async Task<HtmlNode> AuthorToday(this Http http, string key, string localUrl)
    {
        var requestUri = new Uri(new Uri("https://author.today"), localUrl);
        const string cookie = "cf_clearance=PtNKqnivIZM5ntn1Xt5imjYT_Z1oIgargOEMvhBXhGE-1663796156-0-150; mobile-mode=false; _ga=GA1.2.1851698772.1662849743; _ym_uid=1662849743100916213; _ym_d=1662849743; LoginCookie=ARgXhi0Dz5NI7O-yAiZxPWeB0Vg1s9E0_RR-IGkEW6CEsh1bUJN_FkIPxgHZt6tTkrykcRnQRrql3296TYon9FMknHO1z_ksAx_MbLmGJAgW3KNsb854WQqv-VpHwxEP9uWpOp2j0L367gn5n0z0w4Sc0K5rF-abbtv9GUMRHgydGs0BVom9PmngFbtWDyYLYJqaoE4V-eP4MR4niWcorie9YjU7C0ZJDm5GFTsa9QOSaHCXQhRJa9Ux8x90lbIhRAD7O7PIQ06JirXt5A64ZhzWiHjGketv33o8jPQfEmd3vqFxZQDWVpRaE5Ye-_MX86eOyItHwSzXoSDaUES0DZvnkpNjA-jD42Ed22CMbiVRdc3E0GDuMF8wuYADmTu85lRtju-JGiMYd3r6FSS4tAhtHVaRiHstRsTC5qFu83CUiuSJgvrjJ7NJx0h5Ul0O1BjN8WfPvswhZmiCAOaHX7_S2lTYkACJZlOm1iy_mO_mDRpZQfCzbCqG1_rqACciQ0G2Iqtu3pQpY6W1oxMDAByUwCoTy42xf9LH1OYTNg8M78BBbrFk_1mhTXKeqpmg2lAPRMc5S4TWTYajj3Nh4OED8ovqFMLaNcx3c1lT7xMehI9I; ngLoginCookie=dba552a71244044cfb7391d71a8ef62d; cf_chl_2=54c1df25e188d29; cf_chl_prog=x13; backend_route=web10; CSRFToken=4Q2KQiPRl72V7qMG0aTtNPEgsaaierke5obfvRXsMzIdtpFYpNVq-o7CD0DYMYS7NDGER7PpTzus5DaZjv8Vio6mTsk1; ngSessnCookie=AAAAAL2DK2Ngf9uNAZM2Bg==; _gid=GA1.2.592254428.1663796158; _ym_isad=1; _ym_visorc=b";
        var html = await http.Get(key,
            new HttpRequestMessage(HttpMethod.Get, requestUri)
            {
                Headers =
                {
                    { "User-Agent", Firefox },
                    { "Cookie", cookie }
                }
            });
        return html.ParseHtml();
     
    }
}