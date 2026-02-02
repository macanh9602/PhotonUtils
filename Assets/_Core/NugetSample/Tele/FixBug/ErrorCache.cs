using System.Collections.Generic;

public static class ErrorCache
{
    private static readonly Dictionary<string, ErrorContext> _cache = new();

    public static void Add(string id, ErrorContext ctx)
    {
        _cache[id] = ctx;
    }

    public static bool TryGet(string id, out ErrorContext ctx)
    {
        return _cache.TryGetValue(id, out ctx);
    }
}