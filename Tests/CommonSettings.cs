using Newtonsoft.Json;

namespace Tests;

public static class CommonSettings
{
    public static readonly JsonSerializerSettings Json = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };
}