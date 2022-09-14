using System.Globalization;
using CsvHelper;
using Newtonsoft.Json;

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
    
    public static async Task SaveJson<T>(this string file, T obj)
    {
        await WriteAllTextAsync(file, JsonConvert.SerializeObject(obj, CommonSettings.Json));
    }
    public static async Task SaveCsv<T>(this string file, IEnumerable<T> records)
    {
        await using var writer = new StreamWriter(file);
        await using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        await csv.WriteRecordsAsync(records);
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
        return json == null ? default : JsonConvert.DeserializeObject<T>(json, CommonSettings.Json);
    }
    
}