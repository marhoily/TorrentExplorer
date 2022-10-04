using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FluentAssertions;
using Newtonsoft.Json;
using ServiceStack;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed record KnownNames(HashSet<string> FirstNames, HashSet<string> LastNames);

public sealed class RussianNamesTests
{
    public const string Known = @"C:\temp\TorrentsExplorerData\Extract\KnownNames.json";
    public sealed record FirstName(
        int Id,
        string Name, string Sex,
        int PeoplesCount,
        string WhenPeoplesCount, string Source);
    public sealed record LastName(
        int Id,
        string Surname, string Sex,
        int PeoplesCount,
        string WhenPeoplesCount, string Source);
    public sealed record JFirstName(string Text, int Count, string[] Ethnic, char Gender);
    public sealed record JLastName(
        string Text, string FName,
        [JsonProperty("f_form")]
        string FForm,
        int Count, char Gender);

    [Fact]
    public async Task Save()
    {
        var firstNames = Read<FirstName>(@"C:\Users\marho\OneDrive\Books\NamesDb\russian_names.csv");
        var lastNames = Read<LastName>(@"C:\Users\marho\OneDrive\Books\NamesDb\russian_surnames.csv");
        firstNames.Count.Should().Be(51529);
        lastNames.Count.Should().Be(318474);
        firstNames
            .OrderByDescending(x => x.PeoplesCount)
            .Select(x => x.Name)
            .Take(5).StrJoin().Should().Be("Россия, Санкт, Бразилия, Индия, Калифорния");
        lastNames
            .OrderByDescending(x => x.PeoplesCount)
            .Select(x => x.Surname)
            .Take(5).StrJoin().Should().Be("Школа, Москва, Московская, Петербург, Ленинградская");
        var ff = Enumerable.ToHashSet(firstNames.Select(x => x.Name));
        var ll = Enumerable.ToHashSet(lastNames.Select(x => x.Surname));
        ff.Count(f => ll.Contains(f)).Should().Be(3765);
        ff.ExceptWith(lastNames.Select(x => x.Surname));
        ll.ExceptWith(firstNames.Select(x => x.Name));
        await Known.SaveJson(new KnownNames(ff, ll));
    }

    private static List<T> Read<T>(string path)
    {
        using var reader = new StreamReader(path);
        using var csv = new CsvReader(reader,
            new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                PrepareHeaderForMatch = args => args.Header.ToLower(),
                Delimiter = ";"
            });
        return csv.GetRecords<T>().ToList();
    }

    [Fact]
    public async Task ReadJFirstNames()
    {
        var firstNames = JRead<JFirstName>(@"C:\temp\TorrentsExplorerData\names.jsonl");
        firstNames.Count.Should().Be(32134);
        firstNames.Select(n => n.Text).Distinct().Count().Should().Be(32134);
        firstNames.OrderByDescending(n => n.Count).Take(5)
            .Select(n => n.Text).StrJoin().Should()
            .Be("Александр, Николай, Иван, Сергей, Владимир");

        var lastNames = JRead<JLastName>(@"C:\temp\TorrentsExplorerData\surnames.jsonl");
        lastNames.Count.Should().Be(375449);
        lastNames.Select(n => n.Text).Distinct().Count().Should().Be(375449);
        lastNames.OrderByDescending(n => n.Count).Take(5)
            .Select(n => n.Text).StrJoin().Should()
            .Be("Иванов, Иванова, Кузнецов, Смирнов, Попов");

        var ff = firstNames
                .GroupBy(x => x.Text.ToLowerInvariant().Simplify())
                .Select(g => new
                {
                    g.Key,
                    Value = g.MaxBy(n => n.Count)!.Text.ToLowerInvariant().ToPascalCase(),
                    Count = g.Sum(x => x.Count),
                    T = "F"
                });
        var ll = lastNames
                .GroupBy(x => x.Text.ToLowerInvariant().Simplify())
                .Select(g => new
                {
                    g.Key,
                    Value = g.MaxBy(n => n.Count)!.Text.ToLowerInvariant().ToPascalCase(),
                    Count = g.Sum(x => x.Count),
                    T = "L"
                });
        var lookup = ff.Concat(ll)
            .GroupBy(x => x.Key)
            .Select(g =>
            {
                var l = g.ToList();
                if (l.Count == 1) return l[0];
                if (l.Count == 2)
                {
                    if (l[0].Count * 3 < l[1].Count) return l[1];
                    if (l[1].Count * 3 < l[0].Count) return l[0];
                    return null;
                }

                throw new Exception(l.StrJoin());
            })
            .WhereNotNull()
            .ToLookup(x => x.T, x => x.Value);


        await Known.SaveJson(new KnownNames(
            new HashSet<string>(lookup["F"]),
            new HashSet<string>(lookup["L"])));

    }

    private static List<T> JRead<T>(string path)
    {
        return path
            .ReadAllText()
            .ReadLines()
            .Select(l => l.FromJson<T>())
            .ToList();
    }

}