using HtmlAgilityPack;

namespace Tests.Utilities;

public static class HtmlWalker
{
    private static readonly ApplyResultMarker Stub = new();

    public sealed class WalkConfig<TResult>
    {
        public bool Stop { get; private set; }
        public bool GoDeep { get; private set; }
        public List<TResult> Collector { get; } = new();

        public ApplyResultMarker Yield(TResult item, WalkInstruction cmd)
        {
            Collector.Add(item);
            GoDeep = cmd == WalkInstruction.GoDeep;
            return Stub;
        }

        public ApplyResultMarker YieldBreak(WalkInstruction cmd)
        {
            Stop = true;
            GoDeep = cmd == WalkInstruction.GoDeep;
            return Stub;
        }
        public ApplyResultMarker Continue(WalkInstruction cmd)
        {
            Stop = false;
            GoDeep = cmd == WalkInstruction.GoDeep;
            return Stub;
        }
    }

    public enum WalkInstruction{GoBy, GoDeep}

    public sealed class ApplyResultMarker
    {
    }

    public static List<TResult> Walk<TResult>(this HtmlNode root,
        Func<HtmlNode, WalkConfig<TResult>, ApplyResultMarker> apply)
    {
        var cfg = new WalkConfig<TResult>();
        bool Inner(HtmlNode x)
        {
            foreach (var child in x.ChildNodes)
            {
                apply(child, cfg);
                if (cfg.Stop) return true; 
                if (!cfg.GoDeep) continue;
                if (Inner(child)) return true;
                if (cfg.Stop) return true;
            }
            return cfg.Stop;
        }

        Inner(root);
        return cfg.Collector;
    }
}