﻿using Newtonsoft.Json.Linq;
using FluentAssertions;
using Tests.UniversalParsing;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record RawAuthor(
    int Id,
    string? FirstName, string? LastName,
    string? FirstNames, string? LastNames,
    string? Name, string? Names);

public sealed class AuthorClassificationTests
{
    public const string Raw = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-raw.json";
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-classified.json";

    public static RawAuthor ToRawAuthor(JObject post)
    {
        string? Get(params string[] keys) => post.FindTags(keys)?.SkipLong(20);
        return new RawAuthor(
            post.FindTag("topic-id")!.ParseInt(),
            Get("Имя автора"), 
            Get("Фамилия автора", "Фамилия автора сценария"),
            Get("Имена авторов"), 
            Get("Фамилии авторов", "Фамилия авторов"),
            Get("Автор"), 
            Get("Фамилии и имена авторов", "Автора", "Авторы"));
    }

    [Fact]
    public async Task SaveRaw()
    {
        var posts = await Step2.Output.ReadJson<JObject[][]>();

        await Raw.SaveJson(posts!
            .SelectMany(p => p)
            .WhereNotNull()
            .Select(ToRawAuthor)
            .ToList());
    }

    [Fact]
    public async Task ClassifyAuthors()
    {
        var posts = await Raw.ReadJson<RawAuthor[]>();

        await Output.SaveTypedJson(
            posts!.Select(section => section.Classify()));
    }

    [Fact]
    public void SingleMix() =>
        new RawAuthor(0, null, null, null, null, "Суржиков Роман", null)
            .Classify()
            .Should().Be(new SingleMix("Суржиков Роман"));

    [Fact]
    public void Single() =>
        new RawAuthor(0, "Суржиков", "Роман", null, null, null, null)
            .Classify()
            .Should().Be(new Single("Суржиков", "Роман"));

    [Fact]
    public void PluralMix() =>
        new RawAuthor(0, null, null, null, null, null, "Круз Андрей, Царев Андрей")
            .Classify()
            .Should().Be(new PluralMix("Круз Андрей, Царев Андрей"));
    [Fact]
    public void Empty() =>
        new RawAuthor(0, null, null, null, null, null, null)
            .Classify()
            .Should().Be(new Empty());
    [Fact]
    public void PluralMixInsideLastName() =>
        new RawAuthor(0, null, "Жуков Клим, Зорич Александр", null, null, null, null)
            .Classify()
            .Should().Be(new PluralMix("Жуков Клим, Зорич Александр"));
    [Fact]
    public void SingleMixInsideLastName() =>
        new RawAuthor(0, null, "Жуков Клим", null, null, null, null)
            .Classify()
            .Should().Be(new SingleMix("Жуков Клим"));
    [Fact]
    public void Plural() =>
        new RawAuthor(0, null, null, "Ерофей, Андрей", "Трофимов, Земляной", null, null)
            .Classify()
            .Should().Be(new Plural("Ерофей, Андрей", "Трофимов, Земляной"));
    [Fact]
    public void PluralMixInsideLastNames() =>
        new RawAuthor(0, null, null, null, "Злотников Роман, Корнилов Антон", null, null)
            .Classify()
            .Should().Be(new PluralMix("Злотников Роман, Корнилов Антон"));
    [Fact]
    public void PluralMixInsideFirstName() =>
        new RawAuthor(0, "Алексей Махров, Борис Орлов", null, null, null, null, null)
            .Classify()
            .Should().Be(new PluralMix("Алексей Махров, Борис Орлов"));
    [Fact]
    public void PluralMixInsideFirstNameDividedWithAnd() =>
        new RawAuthor(0, "Аркадий и Борис", null, null, null, null, "Стругацкий Аркадий, Стругацкий Борис")
            .Classify()
            .Should().Be(new PluralMix("Стругацкий Аркадий, Стругацкий Борис"));
    [Fact]
    public void SingleMixInsideFirstName() =>
        new RawAuthor(0, "Алексей Махров", null, null, null, null, null)
            .Classify()
            .Should().Be(new SingleMix("Алексей Махров"));
    [Fact]
    public void FirstNameDuplicateToPluralMix() =>
        new RawAuthor(0, "Владимир", null, null, null, null, "Кучеренко Владимир, Лис Ирина")
            .Classify()
            .Should().Be(new PluralMix("Кучеренко Владимир, Лис Ирина"));
    [Fact]
    public void SingleFirstPluralLastMix() =>
        new RawAuthor(5651029, "Аркадий Натанович, Борис Натанович", null, null, "Стругацкие", null, null)
            .Classify()
            .Should().Be(new CommonLastMix("Аркадий Натанович, Борис Натанович", "Стругацкие"));
}