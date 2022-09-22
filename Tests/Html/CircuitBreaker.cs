using ServiceStack;

namespace Tests.Html;

public sealed class CircuitBreaker
{
    private readonly Dictionary<string, Thread> _threads = new();
    private sealed class Thread
    {
        private int _successCount;
        private int _millisecondsDelay = 100;
        
        public void Success() => _successCount++;

        public async Task<int> Fail()
        {
            await Task.Delay(_millisecondsDelay = _successCount switch
            {
                < 2 => 1000 + _millisecondsDelay * 2,
                < 5 => _millisecondsDelay * 2,
                > 20 => _millisecondsDelay / 2,
                _ => _millisecondsDelay
            });
            _successCount = 0;
            return _millisecondsDelay;
        }
    }

    public async Task<TResult?> Execute<TResult>(string threadId, Func<Task<TResult>> action)
    {
        try
        {
            var result = await action();
            GetThread(threadId).Success();
            return result;
        }
        catch (Exception)
        {
            Console.WriteLine($"{threadId}: {await GetThread(threadId).Fail()}");
        }
        return default;
    }

    private Thread GetThread(string threadId) => 
        _threads.GetOrAdd(threadId, _ => new Thread());
}