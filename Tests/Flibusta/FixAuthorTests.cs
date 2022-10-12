using Tests.Rutracker;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Flibusta;

public sealed class FixAuthorTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";
        
    public const string OutputUnrecognized =
        @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-unrecognized.json";

    public const string OutputOnly =
        @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-only.json";

    [Fact]
    public async Task FixAuthors()
    {
        var fixData = await InpxTests.AuthorData.ReadJson<AuthorData[]>();
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
        await OutputOnly.SaveTypedJson(result
            .OfType<PurifiedAuthor, Only>()
            .GroupBy(fl => new { fl.Payload.Name })
            .Select(g => new
            {
                g.Key.Name,
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

    //TODO: Й-И
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

    public IEnumerable<PurifiedAuthor> Fix(FirstLast input)
    {
        var (q1, m1) = Fix2(input.FirstName, input.LastName);
        var (q2, m2) = Fix2(input.LastName, input.FirstName);
        var result = q1 >= q2 ? m1 : m2;
        return result ?? new PurifiedAuthor[]
        {
            new UnrecognizedFirstLast(input.FirstName, input.LastName)
        };
    }
    public (int Quality, PurifiedAuthor[]? Matches) Fix2(string originalFirstName, string originalLastName)
    {
        var firstName = originalFirstName.Simplify();
        var lastName = originalLastName.Simplify().Depluralize();

        if (firstName.Contains(' ') && lastName.Contains(' '))
        {
            var aa = firstName.Split(' ');
            var bb = lastName.Split(' ');
            if (aa.Length == 2 && bb.Length == 2)
            {
                var xx = Fix(new FirstLast(aa[0], aa[1]))
                    .Concat(Fix(new FirstLast(bb[0], bb[1])))
                    .ToArray();
                if (xx.Any(x => x is not UnrecognizedFirstLast))
                {
                    return (3, xx);
                }
            }
        }

        var sameLastName = _byLastName[lastName].ToList();

        if (!sameLastName.Any())
        {
            return (0, null);
        }

        var sameFirstAndLast = sameLastName
            .Where(a => a.FirstName == firstName)
            .ToList();
        if (sameFirstAndLast.Count > 0)
        {
            return (3, new PurifiedAuthor[]{new FirstLast(
                sameFirstAndLast[0].FirstName!, sameFirstAndLast[0].LastName!)});
        }


        if (firstName.Contains(' '))
        {
            var parts = firstName.Split(' ');
            if (_byFirstName.Contains(parts[0]) &&
                _byMiddleName.Contains(parts[1]))
            {
                return (4, new PurifiedAuthor[]{new ThreePartsName(
                    parts[0], parts[1], originalLastName)});
            }
        }

        if (firstName.Contains('.'))
        {
            var parts = firstName.Split('.', RemoveEmptyEntries);
            foreach (var fix in ByPartialName(parts, lastName))
            {
                return (2, new[] { fix });
            }
        }

        if (_byFirstName.Contains(firstName))
        {
            return (1, new PurifiedAuthor[]{
                new FirstLast(originalFirstName, originalLastName)});
        }
        return (0, null);

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

    public IEnumerable<PurifiedAuthor> Fix(Only input)
    {
        var parts = input.Name.Split(' ', RemoveEmptyEntries);
        if (parts.Length == 3)
        {
            yield return (S(parts[0]), S(parts[1]), S(parts[2])) switch
            {
                (1, 2, 3) => new ThreePartsName(parts[0], parts[1], parts[2]),
                var t => throw new ArgumentOutOfRangeException(t.ToString())
            };
        }
        yield return input;

        int S(string s)
        {
            if (_byFirstName.Contains(s))
                return 1;
            if (_byMiddleName.Contains(s))
                return 2;
            if (_byLastName.Contains(s))
                return 3;
            return 0;
        }
    }
}