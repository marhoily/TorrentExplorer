using FluentAssertions;

namespace Tests.Utilities;

public static class SpanExtensions
{
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str)
    {
        return new LineSplitEnumerator(str);
    }
    public static SplitEnumerator1 Split(this ReadOnlySpan<char> str, char delimiter)
    {
        return new SplitEnumerator1(str,delimiter);
    }

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct LineSplitEnumerator
    {
        private ReadOnlySpan<char> _str;

        public LineSplitEnumerator(ReadOnlySpan<char> str)
        {
            _str = str;
            Current = default;
        }

        // Needed to be compatible with the foreach operator
        public LineSplitEnumerator GetEnumerator() => this;

        public bool MoveNext()
        {
            var span = _str;
            if (span.Length == 0) // Reach the end of the string
                return false;

            var index = span.IndexOfAny('\r', '\n');
            if (index == -1) // The string is composed of only one line
            {
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                Current = span;
                return true;
            }

            if (index < span.Length - 1 && span[index] == '\r')
            {
                // Try to consume the '\n' associated to the '\r'
                var next = span[index + 1];
                if (next == '\n')
                {
                    Current = span.Slice(0, index);
                    _str = span.Slice(index + 2);
                    return true;
                }
            }

            Current = span.Slice(0, index);
            _str = span.Slice(index + 1);
            return true;
        }

        public ReadOnlySpan<char> Current { get; private set; }
    }


    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct SplitEnumerator1
    {
        private ReadOnlySpan<char> _str;
        private readonly char _delimiter;

        public SplitEnumerator1(ReadOnlySpan<char> str, char delimiter)
        {
            _str = str;
            _delimiter = delimiter;
            Current = default;
        }

        // Needed to be compatible with the foreach operator
        public SplitEnumerator1 GetEnumerator() => this;

        public bool MoveNext()
        {
            var index = 0;
            do
            {
                if (_str.Length == 0) // Reach the end of the string
                    return false;

                index = _str.IndexOf(_delimiter);
                if (index == -1) // The string is composed of only one line
                {
                    Current = _str;
                    _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                    return true;
                }

                Current = _str.Slice(0, index);
                while (index < _str.Length - 1 && _str[++index] == _delimiter)
                {
                }

                _str = _str.Slice(index);
            } while (index < _str.Length - 1 && Current.IsEmpty);
         
            return !Current.IsEmpty;
        }

        public ReadOnlySpan<char> Current { get; private set; }
    }

}

public sealed class SplitLinesTests
{
    [Fact]
    public void SplitLines()
    {
        Split("a\nb").Should().Equal("a", "b");
    }

    [Fact] public void Split1_04() => Split("a;b",';').Should().Equal("a", "b");
    [Fact] public void Split1_05() => Split(";",';').Should().Equal();
    [Fact] public void Split1_06() => Split(";;",';').Should().Equal();
    [Fact] public void Split1_07() => Split("a;;b",';').Should().Equal("a", "b");
    [Fact] public void Split1_08() => Split("a;;;b",';').Should().Equal("a","b");
    [Fact] public void Split1_09() => Split("aaa;b;",';').Should().Equal("aaa","b");
    [Fact] public void Split1_1O() => Split(";a;b",';').Should().Equal("a","b");

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
}