namespace Tests.Utilities;

public static class SpanExtensions
{
    public static LineSplitEnumerator SplitLines(this ReadOnlySpan<char> str)
    {
        return new LineSplitEnumerator(str);
    }
    public static SplitEnumerator1Exact SplitExact(this ReadOnlySpan<char> str, char delimiter)
    {
        return new SplitEnumerator1Exact(str, delimiter);
    }
    public static SplitEnumerator1 Split(this ReadOnlySpan<char> str, char delimiter)
    {
        return new SplitEnumerator1(str, delimiter);
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
            int index;
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

    // Must be a ref struct as it contains a ReadOnlySpan<char>
    public ref struct SplitEnumerator1Exact
    {
        private ReadOnlySpan<char> _str;
        private readonly char _delimiter;
        private bool _extraEmpty;

        public SplitEnumerator1Exact(ReadOnlySpan<char> str, char delimiter)
        {
            _str = str;
            _delimiter = delimiter;
            Current = default;
            _extraEmpty = false;
        }

        // Needed to be compatible with the foreach operator
        public SplitEnumerator1Exact GetEnumerator() => this;

        public bool MoveNext()
        {
            if (_str.IsEmpty && _extraEmpty)
            {
                Current = _str;
                _extraEmpty = false;
                return true;
            }
            if (_str.IsEmpty) // Reach the end of the string
                return false;

            var index = _str.IndexOf(_delimiter);
            if (index == -1) // The string is composed of only one line
            {
                _extraEmpty = false;
                Current = _str;
                _str = ReadOnlySpan<char>.Empty; // The remaining string is an empty string
                return true;
            }
            _extraEmpty = true;
            Current = _str[..index];
            _str = _str[(index+1)..];
            return true;
        }

        public ReadOnlySpan<char> Current { get; private set; }
    }

}