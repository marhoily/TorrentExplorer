using Tests.Rutracker;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Flibusta;

public sealed class FixAuthorTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";

    public const string OutputUnrecognized =
        @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-unrecognized.json";

    [Fact]
    public async Task FixAuthors()
    {
        var fixData = await InpxTests.AuthorDataCollapsed.ReadJson<AuthorData[]>();
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
                g.Key.FirstName,
                g.Key.LastName,
                Topics = g.Select(fl => fl.TopicId).StrJoin()
            }));
    }
}

public static class AuthorFixerExt
{
    public static IEnumerable<WithHeader<TOutput>> OfType<TInput, TOutput>(
        this IEnumerable<WithHeader<TInput>> src) where TOutput : TInput
    {
        foreach (var item in src)
            if (item.Payload is TOutput output)
                yield return new WithHeader<TOutput>(item.TopicId, output);
    }
    public static IEnumerable<WithHeader<PurifiedAuthor>> Fix(
        this AuthorFixer authorFixer, WithHeader<PurifiedAuthor>[] src)
    {
        foreach (var item in src)
            foreach (var author in One(item.Payload))
                yield return item with { Payload = author };

        IEnumerable<PurifiedAuthor> One(PurifiedAuthor purifiedAuthor)
        {
            return purifiedAuthor switch
            {
                FirstLast fl => authorFixer.Fix(fl),
                Only o => authorFixer.Fix(o),
                WithMoniker m => new PurifiedAuthor[]
                {
                    new WithMoniker(
                        One(m.RealName).Single(),
                        One(m.Moniker).Single())
                },
                _ => throw new ArgumentOutOfRangeException(purifiedAuthor.ToString())
            };
        }
    }

    public static string Simplify(this string input) =>
        input.Replace('Ё', 'Е').Replace('ё', 'е');

    public static string ReplaceLast(this string input, string needle, string value)
    {
        if (input.EndsWith(needle))
            return input.Remove(input.Length - needle.Length) + value;
        return input;
    }

    public static string Depluralize(this string input)
    {
        return input
            .ReplaceLast("ие", "ий")
            .ReplaceLast("вы", "в");
    }
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

    public IEnumerable<PurifiedAuthor> Fix(FirstLast input)
    {
        var result = Fix2(input.FirstName, input.LastName).ToList();
        if (!result.All(r => r is UnrecognizedFirstLast))
            return result;
        var reverse = Fix2(input.LastName, input.FirstName).ToList();
        if (!reverse.All(r => r is UnrecognizedFirstLast))
            return reverse;
        return result;
    }
    public IEnumerable<PurifiedAuthor> Fix2(string originalFirstName, string originalLastName)
    {
        var firstName = originalFirstName.Simplify();
        var lastName = originalLastName.Simplify().Depluralize();

        if (_byLastName.Contains(lastName))
        {
            var result = _byLastName[lastName]
                .Where(a => a.FirstName == firstName)
                .ToList();
            if (result.Count > 0)
            {
                yield return new FirstLast(result[0].FirstName!, result[0].LastName!);
                yield break;
            }
        }

        if (firstName.Contains(' '))
        {
            var parts = firstName.Split(' ');
            if (_byFirstName.Contains(parts[0]) &&
                _byMiddleName.Contains(parts[1]) &&
                _byLastName.Contains(lastName))
            {
                yield return new ThreePartsName(
                    parts[0], parts[1], originalLastName);
                yield break;
            }
        }

        if (firstName.Contains('.'))
        {
            var parts = firstName.Split('.', RemoveEmptyEntries);
            foreach (var fix in ByPartialName(parts, lastName))
            {
                yield return fix;
                yield break;
            }
        }

        if (firstName.Contains(' ') && lastName.Contains(' '))
        {
            var aa = firstName.Split(' ');
            var bb = lastName.Split(' ');
            if (aa.Length == 2 && bb.Length == 2)
            {
                var xx = Fix(new FirstLast(aa[0], aa[1]))
                    .Concat(Fix(new FirstLast(bb[0], bb[1])))
                    .ToList();
                if (xx.Any(x => x is not UnrecognizedFirstLast))
                {
                    foreach (var purifiedAuthor in xx)
                        yield return purifiedAuthor;
                    yield break;
                }
            }
        }

        if (_byFirstName.Contains(firstName) && _byLastName.Contains(lastName))
        {
            yield return new FirstLast(originalFirstName, originalLastName);
            yield break;
        }
        yield return new UnrecognizedFirstLast(originalFirstName, originalLastName);

        IEnumerable<PurifiedAuthor> ByPartialName(string[] parts, string key)
        {
            if (parts.Length is < 1 or > 2 || parts.Any(p => p.Length != 1))
                yield break;

            var options = _byLastName[key]
                .Where(a => a.FirstName?[0] == parts[0][0]);
            if (parts.Length == 2)
                options = options.Where(a => a.MiddleName?[0] == parts[1][0]);
            var selected = options.ToList();
            if (selected.Count == 0) yield break;
            var isUndefined = selected.Select(x => x.FirstName).Distinct().Skip(1).Any();
            var first = selected[0];
            if (isUndefined)
            {
                if (parts.Length == 1)
                {
                    yield return new FirstLast(parts[0], originalLastName);
                }
                else
                {
                    yield return new ThreePartsName(
                        parts[0], parts[1], originalLastName);
                }
            }
            else if (first.MiddleName == null)
            {
                yield return new FirstLast(first.FirstName!, originalLastName);
            }
            else
            {
                yield return new ThreePartsName(
                    first.FirstName!, first.MiddleName, originalLastName);
            }
        }
    }

    public IEnumerable<Only> Fix(Only input)
    {
        yield return input;
    }
}