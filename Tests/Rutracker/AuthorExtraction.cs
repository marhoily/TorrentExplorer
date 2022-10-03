using ServiceStack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record WithHeader<T>(int TopicId, T Payload);

public abstract record ClassifiedAuthor;

public sealed record Single(string FirstName, string LastName) : ClassifiedAuthor;
public sealed record Plural(string FirstNames, string LastNames) : ClassifiedAuthor;
public sealed record SingleMix(string Name) : ClassifiedAuthor;
public sealed record PluralMix(string Names) : ClassifiedAuthor;
public sealed record CommonLastMix(string FirstNames, string CommonLastName) : ClassifiedAuthor;
public sealed record Empty : ClassifiedAuthor;

public abstract record PurifiedAuthor;
public record FirstLast(string FirstName, string LastName) : PurifiedAuthor;
public sealed record UnrecognizedFirstLast(string FirstName, string LastName) : FirstLast(FirstName, LastName);
public sealed record WithMoniker(PurifiedAuthor RealName, PurifiedAuthor Moniker) : PurifiedAuthor;
public sealed record Only(string Name) : PurifiedAuthor;
public sealed record ThreePartsName(string FirstName, string MiddleName, string LastName) : PurifiedAuthor;

public static class AuthorExtraction
{
    public static WithHeader<T> WithHeader<T>(this T payload, int topicId) => new(topicId, payload);

    public static WithHeader<ClassifiedAuthor> Classify(this RawAuthor raw)
    {
        var result = (raw.FirstName, raw.LastName,
                raw.FirstNames, raw.LastNames,
                raw.Name, raw.Names) switch
            {
                ({ } x, null, null, null, null, null) =>
                    x.ContainsAny(",")
                        ? new PluralMix(x)
                        : new SingleMix(x),
                ({ } f, { } l, null, null, null, null) =>
                    Single(f.Replace(';', ',').Replace(" и ", ", "), l.Replace(';', ',')),
                ({ } f, null, null, { } l, null, null) =>
                    new CommonLastMix(f, l),
                (null, null, { } f, { } l, null, null) =>
                    new Plural(f, l),
                (null, null, null, null, { } x, null) =>
                    x.ContainsAny(",")
                        ? new PluralMix(x)
                        : new SingleMix(x),
                (null, null, null, null, null, { } x) =>
                    new PluralMix(x.Replace(';', ',').Replace('.', ',')),
                ({ } d, null, null, null, null, { } x) =>
                    IsDuplicate(x, d)
                        ? new PluralMix(x)
                        : throw new ArgumentOutOfRangeException(raw.ToString()),
                (null, { } x, null, null, null, null) =>
                    x.ContainsAny(",")
                        ? new PluralMix(x)
                        : new SingleMix(x),
                (null, null, null, { } x, null, null) =>
                    new PluralMix(x),
                (null, null, null, null, null, null) =>
                    new Empty(),
                _ => throw new ArgumentOutOfRangeException(raw.ToString())
            };
        return result.WithHeader(raw.Id);

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
                return new SingleMix(f);
            if (f.ContainsAny(",") && l.ContainsAny(","))
                return new Plural(f, l);
            if (f.ContainsAny(",") && !l.ContainsAny(","))
                return new CommonLastMix(f, l);
            return new Single(f, l);
        }
    }

    public static WithHeader<PurifiedAuthor>[] Extract<T>(this WithHeader<T> author)
        where T: ClassifiedAuthor
    {
        var result = author.Payload switch
        {
            CommonLastMix mix => CommonLastMix(mix.FirstNames, mix.CommonLastName),
            Plural plural => Plural(plural.FirstNames, plural.LastNames),
            PluralMix pluralMix => PluralMix(pluralMix.Names),
            Single single => new[] { Single(single.FirstName, single.LastName) },
            SingleMix singleMix => SingleMix(singleMix.Name),
            _ => throw new ArgumentOutOfRangeException(nameof(author), author.ToString())
        };
        return result
            .Select(x => new WithHeader<PurifiedAuthor>(author.TopicId, x))
            .ToArray();
        PurifiedAuthor[] SingleMix(string name)
        {
            var parts = name.Split(' ');
            var idxOfAnd = Array.IndexOf(parts, "и");
            return (parts.Length, idxOfAnd) switch
            {
                (2, _) => new[]
                {
                    Single(parts[1], parts[0])
                },
                (4, 1) => new[]
                {
                    Single(parts[0], parts[3]),
                    Single(parts[2], parts[3])
                },
                (4, 2) => new[]
                {
                    Single(parts[1], parts[0]),
                    Single(parts[3], parts[0])
                },
                (5, 2) => new[]
                {
                    Single(parts[1], parts[0]),
                    Single(parts[4], parts[3])
                },
                (_, -1) => new PurifiedAuthor[]
                {
                    new Only(name)
                },
                _ => throw new ArgumentOutOfRangeException(nameof(name), name)
            };
        }
        PurifiedAuthor[] PluralMix(string name) =>
            ListSplit(name).SelectMany(SingleMix).ToArray();

        PurifiedAuthor Single(string firstName, string lastName)
        {
            if (firstName == lastName)
                return new Only(firstName);
            
            static string Peel(string input)
            {
                var tmp = input.Trim().TrimEnd('_');
                return tmp.Length > 2 && tmp[^3] is not (' ' or '.') 
                    ? tmp.TrimEnd('.') 
                    : tmp;
            }

            lastName = Peel(lastName.Replace(firstName + " ", ""));
            firstName = Peel(firstName.Replace(lastName + " ", ""));
            (lastName, var arg) = lastName.ExtractRoundBraceArgument();
            if (arg != null)
            {
                var realName = SingleMix(arg);
                if (realName.Length != 1)
                    throw new Exception("Double real name?");
                return new WithMoniker(realName[0], 
                    new FirstLast(firstName, lastName));
            }
            return new FirstLast(firstName, lastName);
        }

        PurifiedAuthor[] Plural(string firstNames, string lastNames) =>
            firstNames.Contains(" и ")
                ? CommonLastMix(firstNames, lastNames)
                : ListSplit(firstNames)
                    .Zip(ListSplit(lastNames))
                    .Select(t => Single(t.First, t.Second))
                    .ToArray();

        static IEnumerable<string> ListSplit(string input) =>
            input.Split(',').Select(x => x.Trim());

        PurifiedAuthor[] CommonLastMix(string firstNames, string commonLastName) =>
            ListSplit(firstNames.Replace(" и ", ","))
                .Select(firstName => new FirstLast(firstName, commonLastName))
                .OfType<PurifiedAuthor>()
                .ToArray();
    }
}