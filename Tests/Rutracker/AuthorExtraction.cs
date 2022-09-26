using ServiceStack;

namespace Tests.Rutracker;

public abstract record ClassifiedAuthor;

public sealed record Single(string FirstName, string LastName) : ClassifiedAuthor;
public sealed record Plural(string FirstNames, string LastNames) : ClassifiedAuthor;
public sealed record SingleMix(string Name) : ClassifiedAuthor;
public sealed record PluralMix(string Names) : ClassifiedAuthor;
public sealed record CommonLastMix(string FirstNames, string CommonLastName) : ClassifiedAuthor;
public sealed record Empty : ClassifiedAuthor;

public abstract record PurifiedAuthor;
public sealed record FirstLast(string FirstName, string LastName) : PurifiedAuthor;
public sealed record Only(string Name) : PurifiedAuthor;

public static class AuthorExtraction
{
    public static ClassifiedAuthor Classify(this RawAuthor raw)
    {
        return (raw.FirstName, raw.LastName,
                raw.FirstNames, raw.LastNames,
                raw.Name, raw.Names) switch
        {
            ({ } x, null, null, null, null, null) => x.ContainsAny(",") ? new PluralMix(x) : new SingleMix(x),
            ({ } f, { } l, null, null, null, null) => new Single(f, l),
            ({ } f, null, null, { } l, null, null) => new CommonLastMix(f, l),
            (null, null, { } f, { } l, null, null) => new Plural(f, l),
            (null, null, null, null, { } x, null) => new SingleMix(x),
            (null, null, null, null, null, { } x) => new PluralMix(x),
            ({ } d, null, null, null, null, { } x) => IsDuplicate(x, d) ? new PluralMix(x) : throw new ArgumentOutOfRangeException(raw.ToString()),
            (null, { } x, null, null, null, null) => x.ContainsAny(",") ? new PluralMix(x) : new SingleMix(x),
            (null, null, null, { } x, null, null) => new PluralMix(x),
            (null, null, null, null, null, null) => new Empty(),
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
    }

    public static PurifiedAuthor[] Extract(this ClassifiedAuthor author)
    {
        return author switch
        {
          //  CommonLastMix commonLastMix => throw new NotImplementedException(),
          //  Empty empty => throw new NotImplementedException(),
            Plural plural => Plural(plural.FirstNames, plural.LastNames),
            PluralMix pluralMix => PluralMix(pluralMix.Names),
            Single single => new PurifiedAuthor[] { new FirstLast(single.FirstName, single.LastName) },
            SingleMix singleMix => SingleMix(singleMix.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(author), author.ToString())
        };
        static PurifiedAuthor[] SingleMix(string name)
        {
            var parts = name.Split(' ');
            return parts.Length switch
            {
                1 => new PurifiedAuthor[]{new Only(parts[0])},
                2 => new PurifiedAuthor[]{new FirstLast(parts[1], parts[0])},
                3 => new PurifiedAuthor[]{new Only(name)},
                4 => parts[2] is "и" 
                    ? new PurifiedAuthor[]
                    {
                        new FirstLast(parts[1], parts[0]),
                        new FirstLast(parts[3], parts[0])
                    }
                    : throw new ArgumentOutOfRangeException(name),
                _ => throw new ArgumentOutOfRangeException(name)
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
    }
}