using ServiceStack;

namespace Tests.Rutracker;

public abstract record ClassifiedAuthor(int TopicId);

public sealed record Single(int TopicId, string FirstName, string LastName) : ClassifiedAuthor(TopicId);
public sealed record Plural(int TopicId, string FirstNames, string LastNames) : ClassifiedAuthor(TopicId);
public sealed record SingleMix(int TopicId, string Name) : ClassifiedAuthor(TopicId);
public sealed record PluralMix(int TopicId, string Names) : ClassifiedAuthor(TopicId);
public sealed record CommonLastMix(int TopicId, string FirstNames, string CommonLastName) : ClassifiedAuthor(TopicId);
public sealed record Empty(int TopicId) : ClassifiedAuthor(TopicId);

public abstract record PurifiedAuthor(int TopicId);
public record FirstLast(int TopicId, string FirstName, string LastName) : PurifiedAuthor(TopicId);
public sealed record UnrecognizedFirstLast(int TopicId, string FirstName, string LastName) : FirstLast(TopicId, FirstName, LastName);
public sealed record Only(int TopicId, string Name) : PurifiedAuthor(TopicId);
public sealed record ThreePartsName(int TopicId, string FirstName, string MiddleName, string LastName) : PurifiedAuthor(TopicId);

public static class AuthorExtraction
{
    public static ClassifiedAuthor Classify(this RawAuthor raw)
    {
        return (raw.FirstName, raw.LastName,
                raw.FirstNames, raw.LastNames,
                raw.Name, raw.Names) switch
        {
            ({ } x, null, null, null, null, null) =>
                x.ContainsAny(",")
                    ? new PluralMix(raw.Id, x)
                    : new SingleMix(raw.Id, x),
            ({ } f, { } l, null, null, null, null) =>
                Single(f.Replace(';', ',').Replace(" и ", ", "), l.Replace(';', ',')),
            ({ } f, null, null, { } l, null, null) =>
                new CommonLastMix(raw.Id, f, l),
            (null, null, { } f, { } l, null, null) =>
                new Plural(raw.Id, f, l),
            (null, null, null, null, { } x, null) =>
                x.ContainsAny(",")
                    ? new PluralMix(raw.Id, x)
                    : new SingleMix(raw.Id, x),
            (null, null, null, null, null, { } x) =>
                new PluralMix(raw.Id, x.Replace(';', ',').Replace('.', ',')),
            ({ } d, null, null, null, null, { } x) =>
                IsDuplicate(x, d)
                    ? new PluralMix(raw.Id, x)
                    : throw new ArgumentOutOfRangeException(raw.ToString()),
            (null, { } x, null, null, null, null) =>
                x.ContainsAny(",")
                    ? new PluralMix(raw.Id, x)
                    : new SingleMix(raw.Id, x),
            (null, null, null, { } x, null, null) =>
                new PluralMix(raw.Id, x),
            (null, null, null, null, null, null) =>
                new Empty(raw.Id),
            _ => throw new ArgumentOutOfRangeException(raw.ToString())
        };

        bool IsDuplicate(string input, string needle)
        {
            if (input.Contains(needle))
                return true;
            var parts = needle.Split(' ');
            if (parts.Length == 3 && parts[1] == "и")
                return input.Contains(parts[0]) && input.Contains(parts[2]);
            return false;
        }

        ClassifiedAuthor Single(string f, string l)
        {
            if (f == l)
                return new SingleMix(raw.Id, f);
            if (f.ContainsAny(",") && l.ContainsAny(","))
                return new Plural(raw.Id, f, l);
            if (f.ContainsAny(",") && !l.ContainsAny(","))
                return new CommonLastMix(raw.Id, f, l);
            return new Single(raw.Id, f, l);
        }
    }

    public static PurifiedAuthor[] Extract(this ClassifiedAuthor author)
    {
        return author switch
        {
            CommonLastMix mix => CommonLastMix(mix.FirstNames, mix.CommonLastName),
            Plural plural => Plural(plural.FirstNames, plural.LastNames),
            PluralMix pluralMix => PluralMix(pluralMix.Names),
            Single single => new[] { Single(single.FirstName, single.LastName) },
            SingleMix singleMix => SingleMix(singleMix.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(author), author.ToString())
        };
        PurifiedAuthor[] SingleMix(string name)
        {
            var parts = name.Split(' ');
            var idxOfAnd = Array.IndexOf(parts, "и");
            return (parts.Length, idxOfAnd) switch
            {
                (2, _) => new PurifiedAuthor[]
                {
                    new FirstLast(author.TopicId, parts[1], parts[0])
                },
                (4, 1) => new PurifiedAuthor[]
                {
                    new FirstLast(author.TopicId, parts[0], parts[3]),
                    new FirstLast(author.TopicId, parts[2], parts[3])
                },
                (4, 2) => new PurifiedAuthor[]
                {
                    new FirstLast(author.TopicId, parts[1], parts[0]),
                    new FirstLast(author.TopicId, parts[3], parts[0])
                },
                (5, 2) => new PurifiedAuthor[]
                {
                    new FirstLast(author.TopicId, parts[1], parts[0]),
                    new FirstLast(author.TopicId, parts[4], parts[3])
                },
                (_, -1) => new PurifiedAuthor[]
                {
                    new Only(author.TopicId, name)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(name), name)
            };
        }
        PurifiedAuthor[] PluralMix(string name) =>
            ListSplit(name).SelectMany(SingleMix).ToArray();

        PurifiedAuthor Single(string firstName, string lastName)
        {
            if (firstName == lastName)
                return new Only(author.TopicId, firstName);
            lastName = lastName.Replace(firstName, "").Trim();
            firstName = firstName.Replace(lastName, "").Trim();
            
            return new FirstLast(author.TopicId, firstName, lastName);
        }

        PurifiedAuthor[] Plural(string firstNames, string lastNames) =>
            ListSplit(firstNames)
                .Zip(ListSplit(lastNames))
                .Select(t => Single(t.First, t.Second))
                .ToArray();

        static IEnumerable<string> ListSplit(string input) =>
            input.Split(',').Select(x => x.Trim());

        PurifiedAuthor[] CommonLastMix(string firstNames, string commonLastName) =>
            ListSplit(firstNames)
                .Select(firstName => new FirstLast(author.TopicId, firstName, commonLastName))
                .OfType<PurifiedAuthor>()
                .ToArray();
    }

}