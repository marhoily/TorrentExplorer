using FluentAssertions;

namespace Tests.Utilities;

public sealed class SplitLinesTests
{
    [Fact]
    public void SplitLines()
    {
        Split("a\nb").Should().Equal("a", "b");
    }

    [Fact] public void Split1_03() => Split("a", ';').Should().Equal("a");
    [Fact] public void Split1_04() => Split("a;b", ';').Should().Equal("a", "b");
    [Fact] public void Split1_05() => Split(";", ';').Should().Equal();
    [Fact] public void Split1_06() => Split(";;", ';').Should().Equal();
    [Fact] public void Split1_07() => Split("a;;b", ';').Should().Equal("a", "b");
    [Fact] public void Split1_08() => Split("a;;;b", ';').Should().Equal("a", "b");
    [Fact] public void Split1_09() => Split("aaa;b;", ';').Should().Equal("aaa", "b");
    [Fact] public void Split1_1O() => Split(";a;b", ';').Should().Equal("a", "b");

    [Fact] public void SplitExact1_02() => SplitExact("a", ';').Should().Equal("a");
    [Fact] public void SplitExact1_03() => SplitExact("a;", ';').Should().Equal("a", "");
    [Fact] public void SplitExact1_04() => SplitExact("a;;", ';').Should().Equal("a", "", "");
    [Fact] public void SplitExact1_05() => SplitExact(";", ';').Should().Equal("", "");
    [Fact] public void SplitExact1_06() => SplitExact(";;", ';').Should().Equal("", "", "");
    [Fact] public void SplitExact1_07() => SplitExact("a;;b", ';').Should().Equal("a", "", "b");
    [Fact] public void SplitExact1_08() => SplitExact("a;;;b", ';').Should().Equal("a", "", "", "b");
    [Fact] public void SplitExact1_09() => SplitExact("aaa;b;", ';').Should().Equal("aaa", "b", "");
    [Fact] public void SplitExact1_1O() => SplitExact(";a;b", ';').Should().Equal("", "a", "b");

    public static List<string> Split(string input)
    {
        List<string> result = new();
        foreach (var line in input.AsSpan().SplitLines())
            result.Add(line.ToString());
        return result;
    }
    public static List<string> Split(string input, char delimiter)
    {
        List<string> result = new();
        foreach (var line in input.AsSpan().Split(delimiter))
            result.Add(line.ToString());
        return result;
    }
    public static List<string> SplitExact(string input, char delimiter)
    {
        List<string> result = new();
        foreach (var line in input.AsSpan().SplitExact(delimiter))
            result.Add(line.ToString());
        return result;
    }
}