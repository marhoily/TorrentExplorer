using System.Collections;

namespace Tests.Utilities;

public sealed class TrieSet : IEnumerable
{
    private readonly Node _root = new();

    public void Add(string key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var node = _root;
        foreach (var item in key)
            node = node.AddItem(item);

        if (node.IsTerminal)
            throw new ArgumentException(
                $"An element with the same key already exists: '{key}'", nameof(key));

        node.IsTerminal = true;

    }

    public int HasPrefixOver(string key)
    {
        var node = _root;
        var counter = 0;
        var result = 0;
        foreach (var item in key)
        {
            if (!node.Children.TryGetValue(item, out node))
                return result;

            counter++;
            if (node.IsTerminal)
                result = counter;
        }

        return result;
    }

    private sealed class Node
    {
        public Node()
        {
            Children = new Dictionary<char, Node>();
        }

        public bool IsTerminal { get; set; }
        public Dictionary<char, Node> Children { get; }
        public Node AddItem(char key)
        {
            if (Children.TryGetValue(key, out var child)) return child;
            child = new Node();
            Children.Add(key, child);
            return child;
        }
    }

    public IEnumerator GetEnumerator() => throw new NotImplementedException();
}