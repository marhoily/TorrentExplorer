using HtmlAgilityPack;
using Tests.Utilities;

namespace Tests.Rutracker;

public sealed class Cursor
{
    private readonly string _ceiling;
    public HtmlNode? Node { get; private set; }

    public Cursor(HtmlNode node)
    {
        Node = node;
        _ceiling = node.XPath;
    }

    public void Set(HtmlNode? value) =>
        Node = value?.XPath.StartsWith(_ceiling) != true ? null : value;

    public void GoFurther() => Set(Node?.GoFurther());
    public void GoDeeper() => Set(Node?.GoDeeper());
}