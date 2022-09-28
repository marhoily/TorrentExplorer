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
                Flh(0, "Роман", "Суржиков"));

    private static WithHeader<PurifiedAuthor> Flh(
        int topicId, string firstName, string lastName) => 
        new(topicId, new FirstLast(firstName, lastName));
    private static WithHeader<PurifiedAuthor> O(
        int topicId, string name) => 
        new(topicId, new Only(name));

    [Fact]
    public void SingleMixOnlyLast() =>
        new SingleMix(0, "SadSlim").Extract()
            .Should().Equal(O(0, "SadSlim"));

    [Fact]
    public void SingleMixWithAndIn3RdPosition() =>
        new SingleMix(0, "Дяченко Марина и Сергей")
            .Extract().Should().Equal(
                Flh(0,"Марина", "Дяченко"),
                Flh(0,"Сергей", "Дяченко"));
    
    [Fact]
    public void SingleMixWithAndIn2RdPosition() =>
        new SingleMix(0, "Марина и Сергей Дяченко")
            .Extract().Should().Equal(
                Flh(0,"Марина", "Дяченко"),
                Flh(0,"Сергей", "Дяченко"));
    
    [Fact]
    public void SingleMixWithAndSeparatedNames() =>
        new SingleMix(6208654, "Фалий Светлана и Сандрацкая Алина")
            .Extract().Should().Equal(
                Flh(6208654,"Светлана", "Фалий"),
                Flh(6208654,"Алина", "Сандрацкая"));

    [Fact]
    public void SingleMixWith3Words() =>
        new SingleMix(0, "Гай Юлий Орловский")
            .Extract().Should().Equal(
                O(0, "Гай Юлий Орловский"));

    [Fact]
    public void Single() =>
        new Single(0, "Роман", "Суржиков")
            .Extract().Should().Equal(
                Flh(0,"Роман", "Суржиков"));

    [Fact]
    public void SingleWithMoniker() =>
        new Single(6253789, "Б.", "Беломор (Борис Батыршин)")
            .Extract().Should().Equal(
                new WithHeader<PurifiedAuthor>(6253789,
                    new WithMoniker(
                        new FirstLast("Батыршин", "Борис"),
                        new FirstLast("Б.", "Беломор"))));

    [Fact]
    public void PluralMix() =>
        new PluralMix(0, "Круз Андрей, Царев Андрей")
            .Extract().Should().Equal(
                Flh(0, "Андрей", "Круз"),
                Flh(0, "Андрей", "Царев"));

    [Fact]
    public void Plural() =>
        new Plural(0, "Ерофей, Андрей", "Трофимов, Земляной")
            .Extract().Should().Equal(
                Flh(0, "Ерофей", "Трофимов"),
                Flh(0, "Андрей", "Земляной"));
    
    [Fact]
    public void PluralWithDuplicate() =>
        new Plural(5847772, "Зайцев Константин, Алексей", "Зайцев, Тихий")
            .Extract().Should().Equal(
                Flh(5847772, "Константин", "Зайцев"),
                Flh(5847772, "Алексей", "Тихий"));

    [Fact]
    public void CommonLastMix() =>
        new CommonLastMix(5651029, "Аркадий Натанович, Борис Натанович", "Стругацкие") 
            .Extract().Should().Equal(
                Flh(5651029, "Аркадий Натанович", "Стругацкие"),
                Flh(5651029, "Борис Натанович", "Стругацкие"));
}