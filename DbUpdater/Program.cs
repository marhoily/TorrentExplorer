using System.Diagnostics;
using Humanizer;
using Tests.BookUnification;
using Xunit.Abstractions;

var startNew = Stopwatch.StartNew();
await new Search(new MyTestOutputHelper()).Do();
Console.WriteLine(startNew.Elapsed.Humanize());

public class MyTestOutputHelper : ITestOutputHelper
{
    public void WriteLine(string message) => Console.WriteLine(message);
    public void WriteLine(string format, params object[] args) => Console.WriteLine(format, args);
}