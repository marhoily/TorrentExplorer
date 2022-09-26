using FluentAssertions;
using Tests.Utilities;

namespace Tests.Flibusta;

public class InpxTests
{
    [Fact]
    public void FactMethodName()
    {
        var bookRecords = InpxFormat
            .ReadInpx(@"C:\Users\marho\Downloads\fb2.Flibusta.Net\flibusta_fb2_local.inpx")
            .ToList();
        bookRecords.Count.Should().Be(547939);
    }

    [Fact]
    public async Task ExtractAuthors()
    {
        var bookRecords = InpxFormat
            .ReadInpx(@"C:\Users\marho\Downloads\fb2.Flibusta.Net\flibusta_fb2_local.inpx")
            .ToList();
        await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .SaveJson(bookRecords.SelectMany(b => b.Authors).Distinct());
    }

    [Fact]
    public async Task LocalAuthorsToAtoms()
    {
        var authors = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        await @"c:\temp\TorrentsExplorerData\Extract\AuthorAtoms.json".SaveJson(
            authors!.SelectMany(a => new[] { a.LastName, a.FirstName, a.MiddleName })
                .WhereNotNull()
                .Distinct());
    }

    [Fact]
    public async Task PurgeLocalAuthors()
    {
        var authors = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        await @"c:\temp\TorrentsExplorerData\Extract\ContainsSpace.json"
            .SaveJson(authors!.Where(a => (a.FirstName+ a.MiddleName + a.LastName).Contains(' ')));
    }
}
