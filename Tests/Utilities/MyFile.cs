using Newtonsoft.Json;
using YAXLib;

namespace Tests.Utilities;

public static class MyFile
{
    public static string WithFileName(this string fullPath, Func<string, string> replace)
    {
        var directoryName = Path.GetDirectoryName(fullPath);
        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(fullPath);
        var extension = Path.GetExtension(fullPath);
        var fileName = replace(fileNameWithoutExtension)+ extension;
        return directoryName != null 
            ? Path.Combine(directoryName, fileName)
            : fileName;
    }
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
    public static async Task SaveTypedJson<T>(this string file, T obj) => 
        await WriteAllTextAsync(file, JsonConvert.SerializeObject(obj, TypedJsonSettings));

    public static void SaveXml<T>(this string file, T obj)
    {
        var serializer = new YAXSerializer(typeof(T));
        try
        {
            serializer.SerializeToFile(obj, file);

        }
        catch (DirectoryNotFoundException)
        {
            CreateDirIfNeeded(file);
            serializer.SerializeToFile(obj, file);

        }
    }
    public static async Task WriteAllTextAsync(this string file, string text)
    {
        try
        {
            await File.WriteAllTextAsync(file, text);
        }
        catch (DirectoryNotFoundException)
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
    public static async Task<T?> ReadTypedJson<T>(this string file)
    {
        var json = await file.ReadAllTextOrNullAsync();
        return json == null ? default : JsonConvert.DeserializeObject<T>(json, TypedJsonSettings);
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
    private static readonly JsonSerializerSettings TypedJsonSettings = new()
    {
        Formatting = Formatting.Indented,
        NullValueHandling = NullValueHandling.Ignore,
        DefaultValueHandling = DefaultValueHandling.Ignore,
        TypeNameHandling = TypeNameHandling.Auto,
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