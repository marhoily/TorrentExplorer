using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class AuthorExtractionTests
{
    public const string Output = @"C:\temp\TorrentsExplorerData\Extract\Rutracker\authors-extracted.json";

    [Fact]
    public async Task ClassifyAuthors()
    {
        var posts = await AuthorClassificationTests
            .Output.ReadTypedJson<ClassifiedAuthor[]>();
        
    }
}