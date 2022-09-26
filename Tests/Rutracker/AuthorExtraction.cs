using ServiceStack;

namespace Tests.Rutracker;

public abstract record ClassifiedAuthor(int TopicId);

public sealed record Single(int TopicId, string FirstName, string LastName) : ClassifiedAuthor(TopicId);
public sealed record Plural(int TopicId, string FirstNames, string LastNames) : ClassifiedAuthor(TopicId);
public sealed record SingleMix(int TopicId, string Name) : ClassifiedAuthor(TopicId);
public sealed record PluralMix(int TopicId, string Names) : ClassifiedAuthor(TopicId);
public sealed record CommonLastMix(int TopicId, string FirstNames, string CommonLastName) : ClassifiedAuthor(TopicId);
public sealed record Empty(int TopicId) : ClassifiedAuthor(TopicId);

public abstract record PurifiedAuthor;
public record FirstLast(string FirstName, string LastName) : PurifiedAuthor;
public sealed record UnrecognizedFirstLast(string FirstName, string LastName) : FirstLast(FirstName, LastName);
public sealed record Only(string Name) : PurifiedAuthor;

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
            ({ } f, { } l, null, null, null, null) => Single(f, l),
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
            Single single => new PurifiedAuthor[] { new FirstLast(single.FirstName, single.LastName) },
            SingleMix singleMix => SingleMix(singleMix.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(author), author.ToString())
        };
        static PurifiedAuthor[] SingleMix(string name)
        {
            var parts = name.Split(' ');
            var idxOfAnd = Array.IndexOf(parts, "и");
            return (parts.Length, idxOfAnd) switch
            {
                (2, _) => new PurifiedAuthor[]
                {
                    new FirstLast(parts[1], parts[0])
                },
                (4, 1) => new PurifiedAuthor[]
                {
                    new FirstLast(parts[0], parts[3]),
                    new FirstLast(parts[2], parts[3])
                },
                (4, 2) => new PurifiedAuthor[]
                {
                    new FirstLast(parts[1], parts[0]),
                    new FirstLast(parts[3], parts[0])
                },
                (5, 2) => new PurifiedAuthor[]
                {
                    new FirstLast(parts[1], parts[0]),
                    new FirstLast(parts[4], parts[3])
                },
                (_, -1) => new PurifiedAuthor[]
                {
                    new Only(name)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(name), name)
            };
        }
        static PurifiedAuthor[] PluralMix(string name) =>
            ListSplit(name).SelectMany(SingleMix).ToArray();

        static PurifiedAuthor[] Plural(string firstNames, string lastNames) =>
            ListSplit(firstNames)
                .Zip(ListSplit(lastNames))
                .Select(t => new FirstLast(t.First, t.Second))
                .OfType<PurifiedAuthor>()
                .ToArray();

        static IEnumerable<string> ListSplit(string input) =>
            input.Split(',').Select(x => x.Trim());

        static PurifiedAuthor[] CommonLastMix(string firstNames, string commonLastName) =>
            ListSplit(firstNames)
                .Select(firstName => new FirstLast(firstName, commonLastName))
                .OfType<PurifiedAuthor>()
                .ToArray();
    }

}