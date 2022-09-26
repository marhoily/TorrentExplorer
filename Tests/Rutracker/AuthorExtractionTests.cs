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
        await Output.SaveTypedJson(authors!
            .Where(a => a is not Empty)
            .SelectMany(a => a.Extract()));
    }

    [Fact]
    public void SingleMix() =>
        new SingleMix(0, "Суржиков Роман")
            .Extract().Should().Equal(
                new FirstLast(0, "Роман", "Суржиков"));

    [Fact]
    public void SingleMixOnlyLast() =>
        new SingleMix(0, "SadSlim")
            .Extract().Should().Equal(
                new Only(0, "SadSlim"));

    [Fact]
    public void SingleMixWithAndIn3RdPosition() =>
        new SingleMix(0, "Дяченко Марина и Сергей")
            .Extract().Should().Equal(
                new FirstLast(0, "Марина", "Дяченко"),
                new FirstLast(0, "Сергей", "Дяченко"));
    
    [Fact]
    public void SingleMixWithAndIn2RdPosition() =>
        new SingleMix(0, "Марина и Сергей Дяченко")
            .Extract().Should().Equal(
                new FirstLast(0, "Марина", "Дяченко"),
                new FirstLast(0, "Сергей", "Дяченко"));
    
    [Fact]
    public void SingleMixWithAndSeparatedNames() =>
        new SingleMix(6208654, "Фалий Светлана и Сандрацкая Алина")
            .Extract().Should().Equal(
                new FirstLast(6208654, "Светлана", "Фалий"),
                new FirstLast(6208654, "Алина", "Сандрацкая"));

    [Fact]
    public void SingleMixWith3Words() =>
        new SingleMix(0, "Гай Юлий Орловский")
            .Extract().Should().Equal(
                new Only(0, "Гай Юлий Орловский"));

    [Fact]
    public void Single() =>
        new Single(0, "Роман", "Суржиков")
            .Extract().Should().Equal(
                new FirstLast(0, "Роман", "Суржиков"));

    [Fact]
    public void PluralMix() =>
        new PluralMix(0, "Круз Андрей, Царев Андрей")
            .Extract().Should().Equal(
                new FirstLast(0, "Андрей", "Круз"),
                new FirstLast(0, "Андрей", "Царев"));

    [Fact]
    public void Plural() =>
        new Plural(0, "Ерофей, Андрей", "Трофимов, Земляной")
            .Extract().Should().Equal(
                new FirstLast(0, "Ерофей", "Трофимов"),
                new FirstLast(0, "Андрей", "Земляной"));

    [Fact]
    public void CommonLastMix() =>
        new CommonLastMix(5651029, "Аркадий Натанович, Борис Натанович", "Стругацкие") 
            .Extract().Should().Equal(
                new FirstLast(5651029, "Аркадий Натанович", "Стругацкие"),
                new FirstLast(5651029, "Борис Натанович", "Стругацкие"));
}