using ServiceStack;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Rutracker;

public record AuthorInfo(
    string? FirstName,
    string? LastName,
    string? UnknownName,
    string? Malformed);

public abstract record ClassifiedAuthor;

public sealed record Single(string FirstName, string LastName) : ClassifiedAuthor;
public sealed record Plural(string FirstNames, string LastNames) : ClassifiedAuthor;
public sealed record SingleMix(string Name) : ClassifiedAuthor;
public sealed record PluralMix(string Names) : ClassifiedAuthor;
public sealed record CommonLastMix(string FirstNames, string CommonLastName) : ClassifiedAuthor;
public sealed record Empty : ClassifiedAuthor;

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
    
    static List<AuthorInfo>? Single(string? firstName, string? lastName)
    {
        if (firstName == null && lastName == null) return null;
        if (firstName == null)
            return SingleMix(lastName);
        if (lastName == null)
            return SingleMix(lastName);
        if (firstName.Contains(' ') && lastName[^1] is 'и' or 'ы')
        {
            var firstNames = firstName.Split(' ', RemoveEmptyEntries);
            if (firstNames.Length != 3 || firstNames[1] != "и")
                return new List<AuthorInfo>
                    {
                        new(firstName, lastName, null, null),
                    };
            return new List<AuthorInfo>
                {
                    new(firstNames[0], lastName, null, null),
                    new(firstNames[2], lastName, null, null)
                };
        }

        if ((firstName + lastName).ContainsAny("/", ",", ";"))
            return Multiple(firstName, lastName);
        return new List<AuthorInfo> { new(firstName, lastName, null, null) };
    }
    static List<AuthorInfo>? Multiple(string? firstNames, string? lastNames)
    {
        if (firstNames == null && lastNames == null) return null;
        if (firstNames == null)
            return MultipleMix(lastNames);
        if (lastNames == null)
            throw new Exception();
        var ff = firstNames.Split(',', ';', RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        var ss = lastNames.Split(',', ';', RemoveEmptyEntries).Select(x => x.Trim()).ToArray();
        if (ff.Any(x => x.Contains(' ')) || ss.Any(x => x.Contains(' ')))
        {
            return new List<AuthorInfo>()
                {
                    new(null, null, null, ff.Concat(ss).StrJoin(" "))
                };
        }
        if (ff.Concat(ss).Any(x => x.Contains(" ")))
            throw new Exception(firstNames + " " + lastNames);

        return ff.Zip(ss)
            .Select(p => new AuthorInfo(p.First, p.Second, null, null))
            .ToList();
    }

    static List<AuthorInfo>? SingleMix(string? name)
    {
        if (name == null && name == null) return null;
        return new List<AuthorInfo> { new(null, null, name, null) };
    }
    static List<AuthorInfo>? MultipleMix(string? names)
    {
        if (names == null) return null;
        var result = new List<AuthorInfo>();
        foreach (var s in names.Split(',', '/', ';', '.'))
        {
            var strings = s.Split(' ', RemoveEmptyEntries);
            if (strings.Length == 4)
            {
                if (strings[2] == "и")
                {
                    result.Add(new AuthorInfo(strings[1], strings[0], null, null));
                    result.Add(new AuthorInfo(strings[2], strings[0], null, null));
                }
                if (strings[1] == "и")
                {
                    result.Add(new AuthorInfo(strings[0], strings[1], null, null));
                    result.Add(new AuthorInfo(strings[0], strings[2], null, null));
                }
                return result;
            }

            if (strings.Length == 5 && strings[2] == "и")
            {
                result.Add(new AuthorInfo(strings[0], strings[1], null, null));
                result.Add(new AuthorInfo(strings[3], strings[4], null, null));
                return result;
            }
            if (strings.Length > 3)
                throw new Exception(names);
            result.Add(new AuthorInfo(null, null, s, null));
        }
        return result;
    }

}