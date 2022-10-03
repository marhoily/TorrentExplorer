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
            .Output.ReadTypedJson<WithHeader<ClassifiedAuthor>[]>();
        await Output.SaveTypedJson(authors!
            .Where(a => a.Payload is not Empty)
            .SelectMany(a => a.Extract()));
    }

    [Fact]
    public void SingleMix() =>
        new SingleMix("Суржиков Роман").WithHeader(0)
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
        new SingleMix("SadSlim").WithHeader(0).Extract()
            .Should().Equal(O(0, "SadSlim"));

    [Fact]
    public void SingleMixWithAndIn3RdPosition() =>
        new SingleMix("Дяченко Марина и Сергей").WithHeader(0)
            .Extract().Should().Equal(
                Flh(0, "Марина", "Дяченко"),
                Flh(0, "Сергей", "Дяченко"));

    [Fact]
    public void SingleMixWithAndIn2RdPosition() =>
        new SingleMix("Марина и Сергей Дяченко").WithHeader(0)
            .Extract().Should().Equal(
                Flh(0, "Марина", "Дяченко"),
                Flh(0, "Сергей", "Дяченко"));

    [Fact]
    public void SingleMixWithAndSeparatedNames() =>
        new SingleMix("Фалий Светлана и Сандрацкая Алина")
            .WithHeader(6208654).Extract().Should().Equal(
                Flh(6208654, "Светлана", "Фалий"),
                Flh(6208654, "Алина", "Сандрацкая"));

    [Fact]
    public void SingleMixWith3Words() =>
        new SingleMix("Гай Юлий Орловский").WithHeader(0)
            .Extract().Should().Equal(
                O(0, "Гай Юлий Орловский"));

    [Fact]
    public void Single() =>
        new Single("Роман", "Суржиков").WithHeader(0)
            .Extract().Should().Equal(
                Flh(0, "Роман", "Суржиков"));
 
    [Fact]
    public void SingleUnderscore() =>
        new Single("Роман", "Суржиков_").WithHeader(0)
            .Extract().Should().Equal(
                Flh(0, "Роман", "Суржиков"));

    [Fact]
    public void InclusionWithoutSpace() =>
        new Single("Макс", "Максимов").WithHeader(0)
            .Extract().Should().Equal(
                Flh(0, "Макс", "Максимов"));

    [Fact]
    public void SingleWithMoniker() =>
        new Single("Б.", "Беломор (Борис Батыршин)")
            .WithHeader(6253789).Extract().Should().Equal(
                new WithHeader<PurifiedAuthor>(6253789,
                    new WithMoniker(
                        new FirstLast("Батыршин", "Борис"),
                        new FirstLast("Б.", "Беломор"))));

    [Fact]
    public void PluralMix() =>
        new PluralMix("Круз Андрей, Царев Андрей")
            .WithHeader(0).Extract().Should().Equal(
                Flh(0, "Андрей", "Круз"),
                Flh(0, "Андрей", "Царев"));

    [Fact]
    public void Plural() =>
        new Plural("Ерофей, Андрей", "Трофимов, Земляной")
            .WithHeader(0).Extract().Should().Equal(
                Flh(0, "Ерофей", "Трофимов"),
                Flh(0, "Андрей", "Земляной"));

    [Fact]
    public void PluralWithDuplicate() =>
        new Plural("Зайцев Константин, Алексей", "Зайцев, Тихий")
            .WithHeader(5847772).Extract().Should().Equal(
                Flh(5847772, "Константин", "Зайцев"),
                Flh(5847772, "Алексей", "Тихий"));

    [Fact]
    public void CommonLastMix() =>
        new CommonLastMix("Аркадий Натанович, Борис Натанович", "Стругацкие")
            .WithHeader(5651029)
            .Extract().Should().Equal(
                Flh(5651029, "Аркадий Натанович", "Стругацкие"),
                Flh(5651029, "Борис Натанович", "Стругацкие"));

    [Fact]
    public void CommonLastMixWithinPlural() =>
        new Plural("Аркадий и Борис", "Стругацкие")
            .WithHeader(5852207)
            .Extract().Should().Equal(
                Flh(5852207, "Аркадий", "Стругацкие"),
                Flh(5852207, "Борис", "Стругацкие"));
  
    [Fact]
    public void CommonLastMixWithinSingle() =>
        new Single("Александр Сергей", "Абрамовы")
            .WithHeader(5133057)
            .Extract().Should().Equal(
                Flh(5133057, "Александр", "Абрамовы"),
                Flh(5133057, "Сергей", "Абрамовы"));

    [Fact]
    public void DoubleLevelCommonLastMix() =>
        new PluralMix("Стругацкие Аркадий и Борис, Быков Дмитрий")
            .WithHeader(5929456)
            .Extract().Should().Equal(
                Flh(5929456, "Аркадий", "Стругацкие"),
                Flh(5929456, "Борис", "Стругацкие"),
                Flh(5929456, "Дмитрий", "Быков"));
}