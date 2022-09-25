using System.IO.Compression;
using System.Text;
using ServiceStack;

namespace Tests.Flibusta;


public enum GenreType
{
    Fb2,
    Any
}

public enum Field
{
    None,
    Author,
    Title,
    Series,
    SerNo,
    Genre,
    LibId,
    InsideNo,
    File,
    Folder,
    Ext,
    Size,
    Lang,
    Date,
    Code,
    Deleted,
    Rate,
    Uri,
    LibRate,
    KeyWords
}

public record GenreData(
    string GenreCode, string ParentCode,
    string Fb2GenreCode, string GenreAlias);

public record AuthorData(
    string? FirstName,
    string? MiddleName, string? LastName);

public enum BookProp { IsLocal = 1, IsDeleted, HasReview }

public sealed record BookRecord(
    string? Title,
    string? Series,
    List<GenreData> Genres,
    List<AuthorData> Authors,
    string? Lang,
    int? Size,
    // Номер внутри серии
    int? SeqNumber,
    // внешний рейтинг
    int? LibRate,
    List<BookProp> BookProps,
    DateOnly? Date,
    string? FileExt,
    string? FileName,
    // внутр. номер   ИСПОЛЬЗУЕТСЯ ВО ВСЕХ КОЛЛЕКЦИЯХ!
    string? LibId,
    string? Folder,
    // номер в архиве
    int? InsideNo,
    string? KeyWords);
public abstract class InpxFormat
{
    private static readonly Dictionary<string, Field> PossibleFields = new()
    {
        ["AUTHOR"] = Field.Author,
        ["TITLE"] = Field.Title,
        ["SERIES"] = Field.Series,
        ["SERNO"] = Field.SerNo,
        ["GENRE"] = Field.Genre,
        ["LIBID"] = Field.LibId,
        ["INSNO"] = Field.InsideNo,
        ["FILE"] = Field.File,
        ["FOLDER"] = Field.Folder,
        ["EXT"] = Field.Ext,
        ["SIZE"] = Field.Size,
        ["LANG"] = Field.Lang,
        ["DATE"] = Field.Date,
        ["CODE"] = Field.Code,
        ["DEL"] = Field.Deleted,
        ["RATE"] = Field.Rate,
        ["URI"] = Field.Uri,
        ["LIBRATE"] = Field.LibRate,
        ["KEYWORDS"] = Field.KeyWords,
        ["URL"] = Field.Uri
    };

    private const string DefaultStructure = "AUTHOR;GENRE;TITLE;SERIES;SERNO;FILE;SIZE;LIBID;DEL;EXT;DATE;LANG;LIBRATE;KEYWORDS";
    private const string StructureInfoFilename = "structure.info";
    private const char FieldDelimiter = (char)4;
    private const char ItemDelimiter = ':';
    private const char SubItemDelimiter = ',';
    private static readonly HashSet<char> InvalidFileNameChars = new(Path.GetInvalidPathChars());
    private static readonly char[] InvalidFileNameCharsArray = InvalidFileNameChars.ToArray();
    private static string FilterValidFileNameSymbols(string input)
    {
        if (input.IndexOfAny(InvalidFileNameCharsArray) != -1)
            return string.Concat(input.Select(c => !InvalidFileNameChars.Contains(c))).TrimEnd('.');
        else
            return input.TrimEnd('.');
    }

    private static Field[] GetFields(string structureInfo = DefaultStructure)
    {
        // если среди полей есть Folder,
        // то необходимо использовать это поле при создании BookRecord
        return structureInfo
            .Split(';')
            .Select(p => PossibleFields.TryGetValue(p, out var r) ? r : Field.None)
            .ToArray();
    }
    private static BookRecord ParseData(string input, Field[] fieldOrder, GenreType genreType)
    {
        var authorList = new List<AuthorData>();
        var genreList = new List<GenreData>();
        var bookProps = new List<BookProp>();
        var date = default(DateOnly);
        var title = default(string);
        var series = default(string);
        var fileName = default(string);
        var folder = default(string);
        var fileExt = default(string);
        var libId = default(string);
        var lang = default(string);
        var keyWords = default(string);
        var seqNumber = 0;
        var size = 0;
        var insideNo = 0;
        var libRate = 0;
        var actualFields = input.Split(FieldDelimiter);
        var length = Math.Min(actualFields.Length, fieldOrder.Length) - 1; // костыль
        for (var i = 0; i < length; i++)
            switch (fieldOrder[i])
            {
                case Field.Author:
                    authorList = GetAuthors(actualFields[i]).ToList();
                    break;
                case Field.Genre:
                    genreList = GetGenres(actualFields[i], genreType).ToList();
                    break;
                case Field.Title:
                    title = actualFields[i];
                    break;
                case Field.Series:
                    series = actualFields[i];
                    break;
                case Field.SerNo:
                    int.TryParse(actualFields[i], out seqNumber);
                    break;
                case Field.Folder:
                    folder = actualFields[i];
                    break;
                case Field.File:
                    fileName = FilterValidFileNameSymbols(actualFields[i].Trim());
                    break;
                case Field.Ext:
                    fileExt = '.' + actualFields[i];
                    break;
                case Field.Size:
                    int.TryParse(actualFields[i], out size);
                    break;
                case Field.LibId:
                    libId = actualFields[i];
                    break;
                case Field.Deleted:
                    if (actualFields[i] == "1")
                        bookProps.Add(BookProp.IsDeleted);
                    break;
                case Field.Date:
                    date = ParseDateOnly(actualFields[i]);
                    break;
                case Field.InsideNo:
                    insideNo = actualFields[i].ToInt();
                    break;
                case Field.LibRate:
                    int.TryParse(actualFields[i], out libRate);
                    break;
                case Field.Lang:
                    lang = actualFields[i];
                    break;
                case Field.KeyWords:
                    keyWords = actualFields[i];
                    break;
            }

        return new BookRecord(title, series, genreList,
            authorList, lang, size, seqNumber, libRate, bookProps,
            date, fileExt, fileName, libId, folder, insideNo, keyWords);
    }

    private static DateOnly ParseDateOnly(string field) =>
        field != ""
            ? new DateOnly(
                field.Substring(0, 4).ToInt(),
                field.Substring(5, 2).ToInt(),
                field.Substring(8, 2).ToInt())
            : new DateOnly(1970, 1, 1);
    private static IEnumerable<AuthorData> GetAuthors(string field)
    {
        foreach (var item in field.Split(ItemDelimiter, StringSplitOptions.RemoveEmptyEntries))
        {
            var subItems = item.Split(SubItemDelimiter);
            yield return new AuthorData(
                subItems.Length > 1 ? subItems[1] : null,
                subItems.Length > 2 ? subItems[2] : null,
                subItems.Length > 0 ? subItems[0] : null);
        }
    }
    private static IEnumerable<GenreData> GetGenres(string field, GenreType genreType)
    {
        foreach (var item in field.Split(ItemDelimiter, StringSplitOptions.RemoveEmptyEntries))
            yield return genreType == GenreType.Fb2
                ? new GenreData("", "", item, "")
                : new GenreData(item, "", "", "");
    }

    public static IEnumerable<BookRecord> ReadInpx(string inpxFileName, GenreType genreType = GenreType.Fb2)
    {
        using var fileStream = File.OpenRead(inpxFileName);
        var zipArchive = new ZipArchive(fileStream);
        var structureInfo = zipArchive.Entries.SingleOrDefault(e => e.FullName == StructureInfoFilename);
        var fields = GetFields(structureInfo != null
            ? structureInfo.Open().ReadToEnd(Encoding.ASCII)
            : DefaultStructure); // read 
        foreach (var entry in zipArchive.Entries)
            if (entry.FullName.GetExtension() == ".inp")
            {
                using var stream = entry.Open();
                foreach (var line in stream.ReadLines())
                    yield return ParseData(line, fields, genreType);
            }
    }
}