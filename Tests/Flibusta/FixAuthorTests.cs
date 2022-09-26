using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class FixAuthorTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";

    [Fact]
    public async Task LocalAuthorsToAtoms()
    {
        var fixData = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        var fixer = new AuthorFixer(fixData!);

        var rutracker = await AuthorExtractionTests
            .Output.ReadTypedJson<PurifiedAuthor[]>();
        await Output.SaveTypedJson(fixer.Fix(rutracker!));
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
            .ToLookup(a => a.FirstName!);
        _byMiddleName = authors
            .Where(a => a.MiddleName != null)
            .ToLookup(a => a.MiddleName!);
        _byLastName = authors
            .Where(a => a.LastName != null)
            .ToLookup(a => a.LastName!);
    }

    public PurifiedAuthor Fix(FirstLast input)
    {
        if (_byFirstName.Contains(input.FirstName) &&
            _byLastName.Contains(input.LastName))
            return input;

        if (_byFirstName.Contains(input.LastName) &&
            _byLastName.Contains(input.FirstName))
            return new FirstLast(0, input.LastName, input.FirstName);

        if (input.FirstName.Contains(' '))
        {
            var parts = input.FirstName.Split(' ');
            if (_byFirstName.Contains(parts[0]) &&
                _byMiddleName.Contains(parts[1]) &&
                _byLastName.Contains(input.LastName))
                return new ThreePartsName(0, parts[0], parts[1], input.LastName);

        }
        return new UnrecognizedFirstLast(0, input.FirstName, input.LastName);
    }

    public Only Fix(Only input)
    {
        return input;
    }
}
