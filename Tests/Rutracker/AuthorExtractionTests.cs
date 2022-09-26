using FluentAssertions;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class AuthorExtractionTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-extracted.json";

    [Fact]
    public async Task ClassifyAuthors()
    {
        var authors = await AuthorClassificationTests
            .Output.ReadTypedJson<ClassifiedAuthor[]>();
        await Output.SaveJson(authors!
            .Where(a => a is not Empty)
            .Select(a => a.Extract()));
    }

    [Fact]
    public void SingleMix() =>
        new SingleMix("Суржиков Роман")
            .Extract().Should().Equal(
                new FirstLast("Роман", "Суржиков"));

    [Fact]
    public void SingleMixOnlyLast() =>
        new SingleMix("SadSlim")
            .Extract().Should().Equal(
                new Only("SadSlim"));

    [Fact]
    public void SingleMixWithAnd() =>
        new SingleMix("Дяченко Марина и Сергей")
            .Extract().Should().Equal(
                new FirstLast("Марина", "Дяченко"),
                new FirstLast("Сергей", "Дяченко"));

    [Fact]
    public void SingleMixWith3Words() =>
        new SingleMix("Гай Юлий Орловский")
            .Extract().Should().Equal(
                new Only("Гай Юлий Орловский"));

    [Fact]
    public void Single() =>
        new Single("Роман", "Суржиков")
            .Extract().Should().Equal(
                new FirstLast("Роман", "Суржиков"));

    [Fact]
    public void PluralMix() =>
        new PluralMix("Круз Андрей, Царев Андрей")
            .Extract().Should().Equal(
                new FirstLast("Андрей", "Круз"),
                new FirstLast("Андрей", "Царев"));

    [Fact]
    public void Plural() =>
        new Plural("Ерофей, Андрей", "Трофимов, Земляной")
            .Extract().Should().Equal(
                new FirstLast("Ерофей", "Трофимов"),
                new FirstLast("Андрей", "Земляной"));
}