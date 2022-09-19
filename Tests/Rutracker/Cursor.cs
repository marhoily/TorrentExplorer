using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class Cursor
{
    public static implicit operator HtmlNode?(Cursor c) => c.Node;
    private readonly string _ceiling;
    public HtmlNode? Node { get; private set; }

    public Cursor(HtmlNode node)
    {
        Node = node;
        _ceiling = node.XPath;
    }

    private void Set(HtmlNode? value) =>
        Node = value?.XPath.StartsWith(_ceiling) != true ? null : value;

    // ReSharper disable once UnusedMethodReturnValue.Global
    public Cursor GoFurther()
    {
        Set(Node?.GoFurther());
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public Cursor GoDeeper()
    {
        Set(Node?.GoDeeper());
        return this;
    }

    // ReSharper disable once UnusedMethodReturnValue.Global
    public Cursor SkipWhile(Func<HtmlNode, bool> predicate)
    {
        Set(Node?.SkipWhile(predicate));
        return this;
    }
}