using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class FixAuthorTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";
    public const string OutputUnrecognized = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-unrecognized.json";

    [Fact]
    public async Task FixAuthors()
    {
        var fixData = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        var fixer = new AuthorFixer(fixData!);

        var rutracker = await AuthorExtractionTests
            .Output.ReadTypedJson<PurifiedAuthor[]>();
        var result = fixer.Fix(rutracker!).ToList();
        await Output.SaveTypedJson(result);
        await OutputUnrecognized.SaveTypedJson(result.OfType<UnrecognizedFirstLast>());
    }
}
public static class AuthorFixerExt
{
    public static IEnumerable<PurifiedAuthor> Fix(this AuthorFixer authorFixer, PurifiedAuthor[] src)
    {
        foreach (var author in src)
            yield return author switch
            {
                FirstLast fl => authorFixer.Fix(fl),
                Only o => authorFixer.Fix(o),
                _ => throw new ArgumentOutOfRangeException()
            };
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
            return new FirstLast(
                input.TopicId, input.LastName, input.FirstName);

        if (input.FirstName.Contains(' '))
        {
            var parts = firstName.Split(' ');
            if (_byFirstName.Contains(parts[0]) &&
                _byMiddleName.Contains(parts[1]) &&
                _byLastName.Contains(lastName))
                return new ThreePartsName(
                    input.TopicId, parts[0], parts[1], input.LastName);

        }
        return new UnrecognizedFirstLast(
            input.TopicId, input.FirstName, input.LastName);
    }

    public Only Fix(Only input)
    {
        return input;
    }
}
