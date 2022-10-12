using FluentAssertions;
using ServiceStack;
using System.Text;
using SharpCompress.Archives.SevenZip;
using SharpCompress.Readers;

namespace Tests.Rutracker;

public sealed class RdbTests
{
    private const string MetaFile = @"C:\Users\marho\Downloads\База тем РуТрекера с комментариями\Demo\Короткая демо-база раздач.txt";
    private const string RdbFile = @"C:\Users\marho\Downloads\База тем РуТрекера с комментариями\Demo\Полная ДЕМО-база раздач.RDB";
    [Fact]
    public void FactMethodName()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var lines = File
            .ReadAllText(MetaFile, Encoding.GetEncoding(1251))
            .ReadLines().Select(x => x.Split("‰"));
        using var file = File.OpenRead(RdbFile);
        using var bufferedStream = new BufferedStream(file);

        foreach (var line in lines.Take(10))
        {
            bufferedStream.GetEntryId().Should().Be(line[1]);
            using var html = File.Create($@"C:\temp\torrents\{line[1]}.html");
            bufferedStream.GetArchiveEntry()!.OpenEntryStream().CopyTo(html);
        }
    }
}

public static class RutrackerArchive
{
    private static readonly byte[] Terminator = { 01, 00, 0x20, 00, 00, 00, 00, 00 };
    private static readonly MemoryStream Buffer = new();

    private static readonly ReaderOptions ReaderOptions = new()
    {
        LeaveStreamOpen = true,
        DisableCheckIncomplete = true,
        LookForHeader = true,
    };

    public static bool CopyEntryTo(this Stream stream, MemoryStream output)
    {
        var match = 0;
        while (true)
        {
            var x = stream.ReadByte();
            if (x == -1) return true;
            if (match < Terminator.Length && Terminator[match] == x)
                match++;
            else match = 0;

            output.WriteByte((byte)x);
            if (match == Terminator.Length) break;
        }

        return false;
    }

    public static SevenZipArchiveEntry? GetArchiveEntry(this Stream stream)
    {
        Buffer.Seek(0, SeekOrigin.Begin);
        if (stream.CopyEntryTo(Buffer)) return null;
        Buffer.Seek(0, SeekOrigin.Begin);
        return SevenZipArchive
            .Open(Buffer, ReaderOptions)
            .Entries.Single();
    }

    public static string GetEntryId(this Stream stream)
    {
        var sb = new StringBuilder();
        stream.ReadByte();
        while (true)
        {
            var x = stream.ReadByte();
            if (x == 0xDA) break;
            sb.Append((char)x);
        }
        return sb.ToString();
    }
}
