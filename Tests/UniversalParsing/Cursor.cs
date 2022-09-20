using System.Xml.Linq;
using Tests.Utilities;

namespace Tests.UniversalParsing;

/// <summary>
/// Enforces 2 contracts:
/// 1) While you never set Node directly, never cycles
/// 2) Never breaks boundaries of the original node
/// </summary>
public sealed class Cursor
{
    private readonly XNode _ceiling;
    public XNode? Node { get; private set; }
    public XElement? Element => Node as XElement;
    public static implicit operator XNode?(Cursor c) => c.Node;

    public Cursor(XNode node) => _ceiling = Node = node;

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
    public Cursor SkipWhile(Func<XNode, bool> predicate)
    {
        Set(Node?.SkipWhile(predicate));
        return this;
    }

    private void Set(XNode? value) =>
        Node = value?.Ancestors().Contains(_ceiling) == true ? value : null;
}