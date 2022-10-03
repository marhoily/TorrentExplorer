using FluentAssertions;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class InpxTests
{
    public const string AuthorData = @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json";
    public const string AuthorDataCollapsed = @"c:\temp\TorrentsExplorerData\Extract\AuthorDataCollapsed.json";
    
    [Fact]
    public async Task ExtractAuthors()
    {
        var bookRecords = InpxFormat
            .ReadInpx(@"C:\Users\marho\Downloads\fb2.Flibusta.Net\flibusta_fb2_local.inpx")
            .ToList();
        bookRecords.Count.Should().Be(547939);
        await AuthorData.SaveJson(
            bookRecords.SelectMany(b => b.Authors).Distinct());
    }

    [Fact]
    public async Task CollapseAuthors()
    {

        var input = await AuthorData.ReadJson<AuthorData[]>();
            
        await AuthorDataCollapsed.SaveJson(input!
            .GroupBy(a => a.LastName)
            .SelectMany(Collapse)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.MiddleName));
        AuthorData? Match(AuthorData a, AuthorData b)
        {
            var f = Subsume(a.FirstName, b.FirstName);
            var m = Subsume(a.MiddleName, b.MiddleName);
            return (f, m) switch
            {
                (-1, -1) => a,
                (1, 1) => b,
                _ => null
            };
        }

        int Subsume(string? a, string? b)
        {
            if (string.IsNullOrEmpty(a)) return 1; // null/Иван
            if (string.IsNullOrEmpty(b)) return -1;  // Иван/null
            if (a.StartsWith(b.TrimEnd('.'))) return -1; // Иван/И.
            if (b.StartsWith(a.TrimEnd('.'))) return 1; // И./Иван
            return 0;
        }
        IEnumerable<AuthorData> Collapse(IGrouping<string?, AuthorData> group)
        {
            var result = new List<AuthorData>();
            foreach (var a in group) AddOrReplace(a);
            return result;
            
            void AddOrReplace(AuthorData a)
            {
                for (var i = 0; i < result.Count; i++)
                    if (Match(result[i], a) is { } m)
                    {
                        result[i] = m;
                        return;
                    }
                result.Add(a);
            }
        }
    }


    [Fact]
    public async Task PurgeLocalAuthors()
    {
        var authors = await AuthorData
            .ReadJson<AuthorData[]>();
        await @"c:\temp\TorrentsExplorerData\Extract\ContainsSpace.json"
            .SaveJson(authors!.Where(a => (a.FirstName+ a.MiddleName + a.LastName).Contains(' ')));
    }
}