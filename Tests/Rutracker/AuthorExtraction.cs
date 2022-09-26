﻿using ServiceStack;
using Tests.Utilities;
using static System.StringSplitOptions;

namespace Tests.Rutracker;

public record AuthorInfo(
    string? FirstName,
    string? LastName,
    string? UnknownName,
    string? Malformed);

public static class AuthorExtraction
{
    public static List<AuthorInfo> Refine(this RawAuthor raw)
    {/*
        var single = raw.FirstName != null || raw.LastName != null;
        var plural = raw.FirstNames != null || raw.LastNames != null;
        if (raw.Names != null)
        {
            if (single)
            {
                if (firstName != null && names.Contains(firstName) ||
                    lastName != null && names.Contains(lastName))
                {
                    firstName = lastName = null;
                    single = false;
                    dic["FirstName"] = null;
                    dic["LastName"] = null;
                }

                if (firstName != null)
                {
                    var strings = firstName.Split(' ');
                    if (strings.Length == 3 && strings.Contains("и"))
                    {
                        if (names.Contains(strings[0]) && names.Contains(strings[2]))
                        {
                            firstName = lastName = null;
                            single = false;
                            dic["FirstName"] = null;
                            dic["LastName"] = null;
                        }
                    }
                }
            }
        }
        if (single && (plural || name != null || names != null))
            throw new Exception();
        AllowedMix("FirstName", "LastName");
        AllowedMix("FirstNames", "LastNames");
        AllowedMix("Name");
        AllowedMix("Names");
        return Single(firstName, lastName) ??
               Multiple(firstNames, lastNames) ??
               SingleMix(name) ??
               MultipleMix(names) ??
               new List<AuthorInfo>();

        void AllowedMix(params string[] keys)
        {
            var keyed = keys.Select(k => dic[k]);
            var rest = dic
                .Where(pair => !keys.Contains(pair.Key))
                .Select(pair => pair.Value);

            if (keyed.All(p => p != null) && rest.Any(p => p != null))
                throw new Exception();
        }
        */
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
                if (s.Length == 1)
                    1.ToString();
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
}