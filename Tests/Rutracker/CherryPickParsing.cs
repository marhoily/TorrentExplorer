using FluentAssertions;
using Tests.Utilities;

namespace Tests.Rutracker;

public class CherryPickParsing
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\cherry-pick.json";

    [Fact]
    public async Task Test1()
    {
        var htmlList = await Step0.Output.ReadJson<string[]>();
        var result = new List<Topic>();
        foreach (var htmlNode in htmlList!)
        {
            var russianFantasyTopic = htmlNode.ParseHtml().ParseRussianFantasyTopic();
            if (russianFantasyTopic != null)
                result.Add(russianFantasyTopic);
        }

        await Output.SaveJson(result);
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