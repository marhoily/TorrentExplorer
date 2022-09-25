using System.Xml.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step1
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\step-1.xml";

    [Fact]
    public async Task LanguageMixFix()
    {
        using var file = File.OpenText(Step0.Output);
        var doc = await XDocument.LoadAsync(file,
            LoadOptions.PreserveWhitespace, CancellationToken.None);
        foreach (var xText in doc.DescendantNodes().OfType<XText>()) 
            xText.Value = xText.Value.CleanUp().LanguageMixFix();
        await using var output = MyFile.CreateText(Output);
        await doc.SaveAsync(output, 
            SaveOptions.DisableFormatting, 
            CancellationToken.None);
    }
}