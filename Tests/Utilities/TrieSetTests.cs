using FluentAssertions;

namespace Tests.Utilities;

public sealed class TrieSetTests
{
    [Fact]
    public void IsPrefixOfSize()
    {
        var trie = new TrieSet { "AB", "ABCD" };
        trie.HasPrefixOver("").Should().Be(0);
        trie.HasPrefixOver("A").Should().Be(0);
        trie.HasPrefixOver("AB").Should().Be(2);
        trie.HasPrefixOver("AB??").Should().Be(2);
        trie.HasPrefixOver("ABCD").Should().Be(4);
        trie.HasPrefixOver("ABCDE").Should().Be(4);
        trie.HasPrefixOver("A???").Should().Be(0);
    }
}