using FluentAssertions;
using Tests.Utilities;

namespace Tests.Rutracker;

public class CherryPickParsing
{
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