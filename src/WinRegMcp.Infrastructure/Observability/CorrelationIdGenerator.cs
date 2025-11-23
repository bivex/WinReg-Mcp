namespace WinRegMcp.Infrastructure.Observability;

/// <summary>
/// Generates unique correlation IDs for request tracking.
/// </summary>
public static class CorrelationIdGenerator
{
    private static long _counter = 0;

    public static string Generate()
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var counter = Interlocked.Increment(ref _counter);
        return $"req-{timestamp}-{counter:X8}";
    }
}

