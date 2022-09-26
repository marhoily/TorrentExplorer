using Tests.Rutracker;
using Tests.Utilities;

namespace Tests.Flibusta;

public sealed class Merge
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-fixed.json";

    [Fact]
    public async Task LocalAuthorsToAtoms()
    {
        var fixData = await @"c:\temp\TorrentsExplorerData\Extract\AuthorData.json"
            .ReadJson<AuthorData[]>();
        var fixer = new Fixer(fixData!);

        var rutracker = await AuthorExtractionTests
            .Output.ReadTypedJson<PurifiedAuthor[]>();
        await Output.SaveTypedJson(fixer.Fix(rutracker!));
    }
}
public static class FixerExt
{
    public static IEnumerable<PurifiedAuthor> Fix(this Fixer fixer, PurifiedAuthor[] src)
    {
        foreach (var author in src)
            yield return author switch
            {
                FirstLast fl => fixer.Fix(fl),
                Only o => fixer.Fix(o),
                _ => throw new ArgumentOutOfRangeException()
            };
    }
}
public sealed class Fixer
{
    private readonly ILookup<string, AuthorData> _byFirstName;
    private readonly ILookup<string, AuthorData> _byMiddleName;
    private readonly ILookup<string, AuthorData> _byLastName;

    public Fixer(AuthorData[] authors)
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

    public FirstLast Fix(FirstLast input)
    {
        if (_byFirstName.Contains(input.FirstName) &&
            _byLastName.Contains(input.LastName))
            return input;

        if (_byFirstName.Contains(input.LastName) &&
            _byLastName.Contains(input.FirstName))
            return new FirstLast(input.LastName, input.FirstName);

        return new UnrecognizedFirstLast(input.FirstName, input.LastName);
    }

    public Only Fix(Only input)
    {
        return input;
    }
}
