using FluentAssertions;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class InpxTests
{
    public const string Flibusta = @"c:\temp\TorrentsExplorerData\Extract\Flibusta.json";
    public const string LibRusEc = @"c:\temp\TorrentsExplorerData\Extract\LibRusEc.json";
    public const string AuthorData = @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json";

    [Fact]
    public async Task ExtractFlibusta()
    {
        var a = InpxFormat
            .ReadInpx(@"C:\Users\marho\Downloads\fb2.Flibusta.Net\flibusta_fb2_local.inpx")
            .ToList();
        a.Count.Should().Be(547939);

        await Flibusta.SaveJson(a.SelectMany(x => x.Authors).Distinct());
    }

    [Fact]
    public async Task ExtractLibRusEc()
    {
        var b = InpxFormat
            .ReadInpx(@"C:\Users\marho\Downloads\_Lib.rus.ec - Официальная\librusec_local_fb2.inpx")
            .ToList();
        b.Count.Should().Be(482865);

        await LibRusEc.SaveJson(
            b.SelectMany(x => x.Authors).Distinct());
    }

    [Fact]
    public async Task Merge()
    {
        await AuthorData.SaveJson(
            new[]
                {
                    await Flibusta.WithFileName(x => x + "Clean").ReadJson<AuthorData[]>(),
                    await LibRusEc.WithFileName(x => x + "Clean").ReadJson<AuthorData[]>()
                }
                .WhereNotNull()
                .SelectMany(x => x)
                .Distinct()
                .GroupBy(a => a.LastName?.Simplify())
                .SelectMany(Collapse)
                .OrderBy(a => a.LastName)
                .ThenBy(a => a.FirstName)
                .ThenBy(a => a.MiddleName));
         
    }
    [Theory]
    [InlineData(Flibusta)]
    [InlineData(LibRusEc)]
    public async Task Clean(string library)
    {
        var knownNames = (await RussianNamesTests.Known.ReadJson<KnownNames>())!;

        var input = await library.ReadJson<AuthorData[]>();
        var clean = input!
            .SelectMany(Split)
            .Select(Purge)
            .GroupBy(a => a.LastName)
            .SelectMany(Collapse)
            .Select(Flip)
            .OrderBy(a => a.LastName)
            .ThenBy(a => a.FirstName)
            .ThenBy(a => a.MiddleName)
            .ToList();

        await library.WithFileName(x => x + "Clean").SaveJson(clean);

        AuthorData Flip(AuthorData a)
        {
            static bool Find(HashSet<string> lib, string? name) =>
                name != null && lib.Contains(name);

            var ff = Find(knownNames.FirstNames, a.FirstName);
            var fl = Find(knownNames.FirstNames, a.LastName);
            var ll = Find(knownNames.LastNames, a.LastName);
            var lf = Find(knownNames.LastNames, a.FirstName);
            // negative -> need to flip
            var f = ff ? 1 : 0 + (lf ? -1 : 0);
            var l = ll ? 1 : 0 + (fl ? -1 : 0);
            var m = a.MiddleName == null ? 0 : 1;
            return f + l + m < 0 ? new(a.LastName, a.MiddleName, a.FirstName) : a;
        }

        static AuthorData Purge(AuthorData a) => new(
            a.FirstName.ToNullIfEmpty(),
            a.MiddleName.ToNullIfEmpty(),
            a.LastName.ToNullIfEmpty());

        static IEnumerable<AuthorData> Split(AuthorData a)
        {
            if (!string.IsNullOrEmpty(a.MiddleName) ||
                a.FirstName?.Contains(" и ") != true ||
                string.IsNullOrEmpty(a.LastName))
            {
                yield return a;
                yield break;
            }

            var parts = a.FirstName.Split(" и ");
            if (parts.Length != 2 || parts.Any(p => p.Contains(' ')))
            {
                yield return a;
                yield break;
            }

            var aLastName = a.LastName.Depluralize();
            yield return new AuthorData(parts[0], null, aLastName);
            yield return new AuthorData(parts[1], null, aLastName);
        }
    }
    
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
        if (string.IsNullOrEmpty(b)) return -1; // Иван/null
        if (a == b.Simplify()) return -1; 
        if (b == a.Simplify()) return 1; 
        if (a.StartsWith(b.TrimEnd('.'))) return -1; // Иван/И.
        if (b.StartsWith(a.TrimEnd('.'))) return 1; // И./Иван
        return 0;
    }

    IEnumerable<AuthorData> Collapse(IGrouping<string?, AuthorData> group)
    {
        var lastName = group.Select(x => x.LastName).Aggregate((x, y) => Subsume(x, y) < 0 ? y : x);
        var result = new List<AuthorData>();
        foreach (var a in group) AddOrReplace(a with { LastName = lastName });
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