using System.Globalization;
using CsvHelper;
using CsvHelper.Configuration;
using FluentAssertions;
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
        var ff = firstNames.Select(x => x.Name).ToHashSet();
        var ll = lastNames.Select(x => x.Surname).ToHashSet();
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
}