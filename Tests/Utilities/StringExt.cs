﻿using System.Net;
using System.Text.RegularExpressions;
using FluentAssertions;

namespace Tests.Utilities;

public static class StringExt
{
    public static bool IsInitials(this string? name)
    {
        if (name == null) return false;
        if (name.Length == 1) return false;
        if (!name.Contains('.')) return false;
        var parts = name
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .ToList();
        return parts.All(p => p.Length == 1) && 
               parts.Count is 1 or 2;
    }

    public static string ReplaceLast(this string input, string needle, string value)
    {
        if (input.EndsWith(needle))
            return input.Remove(input.Length - needle.Length) + value;
        return input;
    }

    public static string Depluralize(this string input)
    {
        return input
            .ReplaceLast("ие", "ий")
            .ReplaceLast("ны", "н")
            .ReplaceLast("вы", "в");
    }
    public static string StrJoin<T>(this IEnumerable<T> values, string separator = ", ")
    {
        return string.Join(separator, values);
    }

    public static string StrJoin<T>(this IEnumerable<T> values,
        Func<T, string> selector, string separator = ", ")
    {
        return string.Join(separator, values.Select(selector));
    }

    public static string? NullifyWhenEmpty(this string x) => x == "" ? null : x;

    public static bool StartsOrEndsWith(this string s, char c) =>
        s.StartsWith(c) || s.EndsWith(c);

    public static string HtmlTrim(this string s) =>
        s.Replace("&nbsp;", " ").Trim();

    public static string CleanUp(this string s) =>
        WebUtility.HtmlDecode(s.Replace("&nbsp;", " ").Replace("&quot;", "\""));

    private const string Ru = "абвгдеёжзийёклмнопрстуфхцчшщъыьэюя" +
                              "АБВГДЕЁЖЗИЙЁКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ";
    private const string Eng = "abcdefghijklmnopqrstuvwxyz" +
                               "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
    private const string SuspiciousEng = "MPTHBecaoxECAOXp";
    private const string SuspiciousRu = "МРТНВесаохЕСАОХр";
    
    public static string LanguageMixFix(this string input)
    {
        var result = HasMix(input) != 0 
            ? input.Split(' ').Select(FixIfNeeded).StrJoin(" ") 
            : input;
        return result.Replace("ё", "ё").Replace("Ё", "Ё");

        string FixIfNeeded(string word)
        {
            var balance = HasMix(word);
            if (balance == 0) return word;
            return balance > 0
                ? Replace(word, SuspiciousRu, SuspiciousEng)
                : Replace(word, SuspiciousEng, SuspiciousRu);
        }
        static string Replace(string input, string from, string to)
        {
            for (var i = 0; i < from.Length; i++)
                input = input.Replace(from[i], to[i]);
            return input;
        }
        int HasMix(string s)
        {
            var eng = 0;
            var ru = 0;
            for (int i = 0; i < s.Length; i++)
            {
                if (!char.IsLetterOrDigit(s[i]) && s[i] != '_')
                {
                    if (eng > 0 && ru > 0)
                        return eng - ru;
                    eng = ru = 0;
                }

                if (Eng.IndexOf(s[i]) != -1)
                    eng++;
                if (Ru.IndexOf(s[i]) != -1)
                    ru++;
            }
            return eng > 0 && ru > 0 ? eng - ru : 0;
        }
    }

    public static string Unquote(this string s, char quote = '\"')
    {
        return s.StartsWith(quote) && s.EndsWith(quote) ? s.Trim('\"') : s;
    }

    public static string Unbrace(this string input, char open, char close)
    {
        return input.StartsWith(open) && input.EndsWith(close)
            ? input.TrimStart(open).TrimEnd(close)
            : input;
    }

    public static string RemoveRegexIfItIsNotTheWholeString(this string input, string regex)
    {
        var result = Regex.Replace(input, regex, "").Trim();
        return string.IsNullOrWhiteSpace(result) ? input : result;
    }

    public static int ParseInt(this string s)
    {
        return !int.TryParse(s, out var result)
            ? throw new Exception($"'{s}' is not a valid int")
            : result;
    }

    public static string CompressIfPossible(this string s)
    {
        var strings = s.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (strings.Length * 2 - 1 == s.Length)
            return string.Concat(strings);
        return s;
    }

    public static int? TryParseIntOrWord(this string s)
    {
        return int.TryParse(s, out var result)
            ? result
            : s switch
            {
                // книга..
                "первая" => 1,
                "вторая" => 2,
                "третья" => 3,
                _ => null
            };
    }

    public static int? ParseIntOrNull(this string input)
    {
        return int.TryParse(input, out var result) ? result : null;
    }

    public static string TrimPostfix(this string input, string postfix)
    {
        return input.EndsWith(postfix) ? input[..^postfix.Length] : input;
    }

    public static string TrimPrefix(this string input, string prefix) =>
        input.StartsWith(prefix) ? input[prefix.Length..] : input;

    private static readonly Regex RoundBraceArgument = 
        new("^\\s*(?<first>.*?)\\s*\\(\\s*(?<last>[^\\)]*?)\\s*\\)\\s*", RegexOptions.Compiled);
    public static (string, string?) ExtractRoundBraceArgument(this string input)
    {
        var match = RoundBraceArgument.Match(input);
        if (!match.Success || match.Value != input) return (input, null);
        return (match.Groups["first"].Value, match.Groups["last"].Value);
    }
}

public sealed class StringExtTests
{
    [Theory]
    [InlineData("", "", null)]
    [InlineData("()", "", "")]
    [InlineData("a()", "a", "")]
    [InlineData("a ()", "a", "")]
    [InlineData("a(b)", "a", "b")]
    [InlineData("a( b)", "a", "b")]
    [InlineData("a( b )", "a", "b")]
    [InlineData("a(b )", "a", "b")]
    [InlineData("a(b) ", "a", "b")]
    [InlineData(" a b (c)", "a b", "c")]
    [InlineData("a(b)!", "a(b)!", null)]
    public void ExtractRoundBraceArgument(string input, string expectedOutput, string? expectedArg)
    {
        input.ExtractRoundBraceArgument()
            .Should().Be((expectedOutput, expectedArg));
    }
}