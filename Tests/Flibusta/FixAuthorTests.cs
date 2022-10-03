using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class FixAuthorTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";

    public const string OutputUnrecognized =
        @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-unrecognized.json";

    [Fact]
    public async Task FixAuthors()
    {
        var fixData = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        var fixer = new AuthorFixer(fixData!);

        var rutracker = await AuthorExtractionTests
            .Output.ReadTypedJson<WithHeader<PurifiedAuthor>[]>();
        var result = fixer.Fix(rutracker!).ToList();
        await Output.SaveTypedJson(result);
        await OutputUnrecognized.SaveTypedJson(result
            .OfType<PurifiedAuthor, UnrecognizedFirstLast>()
            .GroupBy(fl => new { fl.Payload.FirstName, fl.Payload.LastName })
            .Select(g => new
            {
                g.Key.FirstName, g.Key.LastName,
                Topics = g.Select(fl => fl.TopicId).ToArray()
            }));
    }
}

public static class AuthorFixerExt
{
    public static IEnumerable<WithHeader<TOutput>> OfType<TInput, TOutput>(
        this IEnumerable<WithHeader<TInput>> src) where TOutput:TInput
    {
        foreach (var item in src)
            if (item.Payload is TOutput output)
                yield return new WithHeader<TOutput>(item.TopicId, output);
    }
    public static IEnumerable<WithHeader<PurifiedAuthor>> Fix(
        this AuthorFixer authorFixer, WithHeader<PurifiedAuthor>[] src)
    {
        foreach (var author in src)
            yield return author with
            {
                Payload = One(author.Payload)
            };

        PurifiedAuthor One(PurifiedAuthor purifiedAuthor)
        {
            return purifiedAuthor switch
            {
                FirstLast fl => authorFixer.Fix(fl),
                Only o => authorFixer.Fix(o),
                WithMoniker m => new WithMoniker(One(m.RealName), One(m.Moniker)),
                _ => throw new ArgumentOutOfRangeException(purifiedAuthor.ToString())
            };
        }
    }

    public static string Simplify(this string input) =>
        input.Replace('Ё', 'Е').Replace('ё', 'е');
}

public sealed class AuthorFixer
{
    private readonly ILookup<string, AuthorData> _byFirstName;
    private readonly ILookup<string, AuthorData> _byMiddleName;
    private readonly ILookup<string, AuthorData> _byLastName;

    public AuthorFixer(AuthorData[] authors)
    {
        _byFirstName = authors
            .Where(a => a.FirstName != null)
            .ToLookup(a => a.FirstName!.Simplify());
        _byMiddleName = authors
            .Where(a => a.MiddleName != null)
            .ToLookup(a => a.MiddleName!.Simplify());
        _byLastName = authors
            .Where(a => a.LastName != null)
            .ToLookup(a => a.LastName!.Simplify());
    }

    public PurifiedAuthor Fix(FirstLast input)
    {
        var firstName = input.FirstName.Simplify();
        var lastName = input.LastName.Simplify();
        if (_byFirstName.Contains(firstName) &&
            _byLastName.Contains(lastName))
            return input;

        if (_byFirstName.Contains(lastName) &&
            _byLastName.Contains(firstName))
            return new FirstLast(input.LastName, input.FirstName);

        if (input.FirstName.Contains(' '))
        {
            var parts = firstName.Split(' ');
            if (_byFirstName.Contains(parts[0]) &&
                _byMiddleName.Contains(parts[1]) &&
                _byLastName.Contains(lastName))
                return new ThreePartsName(
                    parts[0], parts[1], input.LastName);
        }

        /* {
    "$type": "Tests.Rutracker.FirstLast, Tests",
    "FirstName": "Г.А.",
    "LastName": "Зотов",
    "TopicId": 2713291
  }*/
        if (input.LastName == "Зотов")
            1.ToString();

        return new UnrecognizedFirstLast(input.FirstName, input.LastName);
    }

    public Only Fix(Only input)
    {
        return input;
    }
}