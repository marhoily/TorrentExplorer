using ServiceStack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed record WithHeader<T>(int TopicId, T Payload);

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
public sealed record WithMoniker(PurifiedAuthor RealName, PurifiedAuthor Moniker) : PurifiedAuthor;
public sealed record Only(string Name) : PurifiedAuthor;
public sealed record ThreePartsName(string FirstName, string MiddleName, string LastName) : PurifiedAuthor;

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

    public static WithHeader<PurifiedAuthor>[] Extract(this ClassifiedAuthor author)
    {
        var result = author switch
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
        PurifiedAuthor[] PluralMix(string name) =>
            ListSplit(name).SelectMany(SingleMix).ToArray();

        PurifiedAuthor Single(string firstName, string lastName)
        {
            if (firstName == lastName)
                return new Only(firstName);
            lastName = lastName.Replace(firstName, "").Trim();
            firstName = firstName.Replace(lastName, "").Trim();
            (lastName, var arg) = lastName.ExtractRoundBraceArgument();
            if (arg != null)
            {
                var realName = SingleMix(arg);
                if (realName.Length != 1)
                    throw new Exception("Double real name?");
                return new WithMoniker(realName[0], 
                    new FirstLast(firstName, lastName));
            }
          // if (firstName.Contains('.'))
          //     1.ToString();
            return new FirstLast(firstName, lastName);
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
                .Select(firstName => new FirstLast(firstName, commonLastName))
                .OfType<PurifiedAuthor>()
                .ToArray();
    }

}