using FluentAssertions;
using Tests.Utilities;

namespace Tests.Rutracker;

public class CherryPickParsing
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\cherry-pick.json";

    [Fact]
    public async Task Do()
    {
        var htmlList = await Step0.Output.ReadJson<string[]>();
        await Output.SaveJson(htmlList!
            .Select(n => n.ParseHtml().ParseRussianFantasyTopic())
            .Where(topic => topic != null)
            .ToList());
    }

    [Theory]
    [InlineData("<s>p:</s>v")]
    [InlineData("p<s>:</s>v")]
    [InlineData("<s>p</s>: v")]
    [InlineData("p:<s>v</s>")]
    [InlineData("<s>p:</s><s>v</s>")]
    [InlineData("<s>p</s>:<s>v</s>")]
    [InlineData("<s>p</s><s>:v</s>")]
    [InlineData("<s>p</s><s>:</s><s>v</s>")]
    public void TagValue(string html)
    {
        html.ParseHtml().Descendants()
            .Last(d =>d.InnerText.StartsWith("p"))
            .TagValue().Should().Be("v");
    }
}