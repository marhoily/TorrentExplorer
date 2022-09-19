using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Rutracker;

/// <summary>
/// Enforces 2 contracts:
/// 1) While you never set Node directly, never cycles
/// 2) Never breaks boundaries of the original node
/// </summary>
public sealed class Cursor
{
    private readonly string _ceiling;
    public HtmlNode? Node { get; private set; }
    public static implicit operator HtmlNode?(Cursor c) => c.Node;

    public Cursor(HtmlNode node)
    {
        Node = node;
        _ceiling = node.XPath;
    }

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

    private void Set(HtmlNode? value) =>
        Node = value?.XPath.StartsWith(_ceiling) != true ? null : value;
}