using System.Xml.Linq;
using Tests.Utilities;

namespace Tests.Rutracker;

public class Step1
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker-En\step-1.xml";

    [Fact]
    public async Task LanguageMixFix()
    {
        void Fix(XElement element)
        {
            element.Name = "span";
            var value = element.Attributes().Select(a => a.Name + "=" + a.Value).StrJoin();
            element.Value = value == "class=postImg, title=https://static.t-ru.org/ranks/rg_poliglot.png" 
                ? "Полиглот" 
                : value;
            foreach (var xAttribute in element.Attributes().ToList())
            {
                xAttribute.Remove();
            }
        }

        using var file = File.OpenText(Step0.Output);
        var doc = await XDocument.LoadAsync(file,
            LoadOptions.PreserveWhitespace, CancellationToken.None);
        foreach (var xText in doc.DescendantNodes().OfType<XText>())
            xText.Value = xText.Value.CleanUp().LanguageMixFix();
        foreach (var element in doc.DescendantNodes().OfType<XElement>())
            if (element.Name == "img")
            {
                Fix(element);
            }
            else if (element.Name == "var")
            {
                if (element.Attribute("class")?.Value == "postImg") 
                    Fix(element);
            }

        await using var output = MyFile.CreateText(Output);
        await doc.SaveAsync(output,
            SaveOptions.DisableFormatting,
            CancellationToken.None);
    }
}