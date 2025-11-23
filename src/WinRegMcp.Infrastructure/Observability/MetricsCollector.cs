using System.Collections.Concurrent;
using System.Diagnostics;

namespace WinRegMcp.Infrastructure.Observability;

/// <summary>
/// Simple metrics collector for registry operations.
/// In production, integrate with Prometheus or similar.
/// </summary>
public sealed class MetricsCollector
{
    private readonly ConcurrentDictionary<string, long> _counters = new();
    private readonly ConcurrentDictionary<string, ConcurrentBag<double>> _histograms = new();
    private long _concurrentOperations = 0;

    public void IncrementCounter(string name, Dictionary<string, string>? labels = null)
    {
        var key = BuildMetricKey(name, labels);
        _counters.AddOrUpdate(key, 1, (_, count) => count + 1);
    }

    public void RecordDuration(string name, double durationSeconds, Dictionary<string, string>? labels = null)
    {
        var key = BuildMetricKey(name, labels);
        var bag = _histograms.GetOrAdd(key, _ => new ConcurrentBag<double>());
        bag.Add(durationSeconds);
    }

    public void IncrementConcurrentOperations()
    {
        Interlocked.Increment(ref _concurrentOperations);
    }

    public void DecrementConcurrentOperations()
    {
        Interlocked.Decrement(ref _concurrentOperations);
    }

    public long GetConcurrentOperations() => Interlocked.Read(ref _concurrentOperations);

    public IOperationTimer StartTimer(string operationName, Dictionary<string, string>? labels = null)
    {
        return new OperationTimer(this, operationName, labels);
    }

    public Dictionary<string, object> GetMetricsSnapshot()
    {
        var snapshot = new Dictionary<string, object>
        {
            ["concurrent_operations"] = GetConcurrentOperations(),
            ["counters"] = new Dictionary<string, long>(_counters),
            ["histograms"] = _histograms.ToDictionary(
                kvp => kvp.Key,
                kvp => new
                {
                    count = kvp.Value.Count,
                    avg = kvp.Value.Count > 0 ? kvp.Value.Average() : 0,
                    min = kvp.Value.Count > 0 ? kvp.Value.Min() : 0,
                    max = kvp.Value.Count > 0 ? kvp.Value.Max() : 0
                })
        };

        return snapshot;
    }

    private static string BuildMetricKey(string name, Dictionary<string, string>? labels)
    {
        if (labels == null || labels.Count == 0)
            return name;

        var labelString = string.Join(",", labels.Select(kvp => $"{kvp.Key}={kvp.Value}"));
        return $"{name}{{{labelString}}}";
    }

    private sealed class OperationTimer : IOperationTimer
    {
        private readonly MetricsCollector _collector;
        private readonly string _operationName;
        private readonly Dictionary<string, string>? _labels;
        private readonly Stopwatch _stopwatch;
        private bool _disposed;

        public OperationTimer(
            MetricsCollector collector,
            string operationName,
            Dictionary<string, string>? labels)
        {
            _collector = collector;
            _operationName = operationName;
            _labels = labels;
            _stopwatch = Stopwatch.StartNew();
            _collector.IncrementConcurrentOperations();
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            _stopwatch.Stop();
            _collector.DecrementConcurrentOperations();
            _collector.RecordDuration(
                _operationName,
                _stopwatch.Elapsed.TotalSeconds,
                _labels);
        }
    }
}

public interface IOperationTimer : IDisposable
{
}

