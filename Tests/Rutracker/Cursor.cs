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

    public void Set(HtmlNode? value) =>
        Node = value?.XPath.StartsWith(_ceiling) != true ? null : value;

    public Cursor GoFurther()
    {
        Set(Node?.GoFurther());
        return this;
    }

    public Cursor GoDeeper()
    {
        Set(Node?.GoDeeper());
        return this;
    }

    public Cursor SkipWhile(Func<HtmlNode, bool> predicate)
    {
        Set(Node?.SkipWhile(predicate));
        return this;
    }

    public string GetBarrier() =>
        Node?.XPath ?? throw new InvalidOperationException(
            "Cannot start a barrier from empty cursor");
    public void AssertBarrierIsRespected(string barrier)
    {
        if (!barrier.Contains(GetBarrier()))
            throw new Exception("Barrier is broken!");
    }
}