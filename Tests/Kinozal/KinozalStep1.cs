using System.Xml.Linq;
using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Kinozal;

public class KinozalStep1
{
    private const string Output = @"C:\temp\TorrentsExplorerData\Extract\kinozal\Step1.json";

    [Fact]
    public async Task Convert()
    {
        await using var fileStream = File.OpenRead(Step0.Output);
        var xml = await XDocument.LoadAsync(
            fileStream, LoadOptions.None, CancellationToken.None);
        await Output.SaveJson(xml.Root!
            .Elements()
            .Select(post => post.ToString().ParseHtml().ParseWall())
            .ToList());
    }
}