namespace Tests.Utilities;

public static class EnumerableExtension
{
    public static IEnumerable<T> WhereNotNull<T>(this IEnumerable<T?> o) where T : class
        => o.Where(x => x != null)!;
}