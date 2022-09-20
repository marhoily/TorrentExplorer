namespace Tests.Html;

public sealed class CircuitBreaker
{
    private int _successCount;
    private int _millisecondsDelay = 100;

    public async Task<TResult?> Execute<TResult>(Func<Task<TResult>> action)
    {
        try
        {
            var result = await action();
            _successCount++;
            return result;
        }
        catch (Exception)
        {
            await Task.Delay(_millisecondsDelay = _successCount switch
            {
                < 2 => 1000 + _millisecondsDelay * 2,
                < 5 => _millisecondsDelay * 2,
                > 20 => _millisecondsDelay / 2,
                _ => _millisecondsDelay
            });
            _successCount = 0;
        }
        return default;
    }
}