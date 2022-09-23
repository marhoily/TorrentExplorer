using Newtonsoft.Json;
using YAXLib;

namespace Tests.Utilities;

public static class MyFile
{
    private static string CreateDirIfNeeded(this string file)
    {
        CreateIfNeeded(Path.GetDirectoryName(file));
        return file;
    }

    private static void CreateIfNeeded(this string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder)) return;
        if (folder.Length < 3) return;
        if (Directory.Exists(folder)) return;
        CreateIfNeeded(Path.GetDirectoryName(folder));
        Directory.CreateDirectory(folder);
    }

    public static async Task SaveJson<T>(this string file, T obj) => 
        await WriteAllTextAsync(file, JsonConvert.SerializeObject(obj, JsonSettings));

    public static void SaveXml<T>(this string file, T obj)
    {
        var serializer = new YAXSerializer(typeof(T));
        serializer.SerializeToFile(obj, file);
    }
    public static async Task WriteAllTextAsync(this string file, string text)
    {
        try
        {
            await File.WriteAllTextAsync(file, text);
        }
        catch
        {
            await File.WriteAllTextAsync(file.CreateDirIfNeeded(), text);
        }
    }

    public static async Task<string?> ReadAllTextOrNullAsync(this string file) =>
        File.Exists(file) ? await File.ReadAllTextAsync(file) : null;
    public static async Task<T?> ReadJson<T>(this string file)
    {
        var json = await file.ReadAllTextOrNullAsync();
        return json == null ? default : JsonConvert.DeserializeObject<T>(json, JsonSettings);
    }
    public static T? ReadXml<T>(this string file)
    {
        var serializer = new YAXSerializer(typeof(T));
        return (T?)serializer.DeserializeFromFile(file);
    }

    private static readonly JsonSerializerSettings JsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore
    };

    public static StreamWriter CreateText(string fileName)
    {
        try
        {
            return File.CreateText(fileName);
        }
        catch (DirectoryNotFoundException)
        {
            CreateDirIfNeeded(fileName);
            return File.CreateText(fileName);
        }
    }
}