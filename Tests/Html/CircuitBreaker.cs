using ServiceStack;

namespace Tests.Html;

public sealed class CircuitBreaker
{
    private sealed class Counter
    {
        private int _counter;
        private int _generation;
        
        public bool Inc()
        {
            var value = Interlocked.Increment(ref _counter);
            var gen = value%100;
            return Interlocked.Exchange(ref _generation, gen) != gen;
        }

        public int Reset()
        {
            Interlocked.Exchange(ref _generation, 0);
            return Interlocked.Exchange(ref _counter, 0);
        }
    }

    private readonly Dictionary<string, Thread> _threads = new();
    private sealed class Thread
    {
        private readonly Counter _counter = new();
        private int _millisecondsDelay = 100;
        
        public int? Success()
        {
            if (_counter.Inc())
                return _millisecondsDelay /= 2;
            return default;
        }

        public async Task<int> Fail()
        {
            await Task.Delay(_millisecondsDelay = _counter.Reset() switch
            {
                < 2 => 1000 + _millisecondsDelay * 2,
                < 5 => 1+ _millisecondsDelay * 2,
                > 20 => _millisecondsDelay / 2,
                _ => _millisecondsDelay
            });
            return _millisecondsDelay;
        }
    }

    public async Task<TResult?> Execute<TResult>(string threadId, Func<Task<TResult>> action)
    {
        try
        {
            var result = await action();
            var success = GetThread(threadId).Success();
            if (success != default)
                Console.WriteLine($"{threadId}: {success}");
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