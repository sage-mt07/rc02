using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Runtime;

public interface ITimeBucketContext
{
    ITimeBucketSet<T> Set<T>(string topic, Period period) where T : class;
}

public interface ITimeBucketSet<T> where T : class
{
    Task<List<T>> ToListAsync(IReadOnlyList<string> pkFilter, CancellationToken ct);
}

/// <summary>
/// Write-context for importing bar data into time-bucketed topics.
/// An application (importer) should implement this to map to its producer.
/// </summary>
public interface ITimeBucketWriteContext
{
    /// <summary>
    /// Produce a single record to the specified topic.
    /// The implementation decides key mapping and headers.
    /// </summary>
    Task ProduceAsync<T>(string topic, T entity, CancellationToken ct = default) where T : class;
}

public static class TimeBucket
{
    public static TimeBucket<T> Get<T>(ITimeBucketContext ctx, Period period) where T : class
        => new(ctx, period);

    /// <summary>
    /// Returns a writer for importing bar data into time-bucketed topics.
    /// Topics follow the convention: {poco}_{period}_final | {poco}_{period}_live
    /// (e.g., rate_1m_final, rate_1m_live).
    /// </summary>
    public static TimeBucketWriter<T> Set<T>(ITimeBucketWriteContext ctx, Period period) where T : class
        => new(ctx, period);
}

public sealed class TimeBucket<T> where T : class
{
    private readonly ITimeBucketContext _ctx;
    private readonly Period _period;
    private readonly string _finalTopic;
    private readonly string _liveTopic;

    internal TimeBucket(ITimeBucketContext ctx, Period period)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        _period = period;
        var baseTopic = typeof(T).Name.ToLowerInvariant();
        var prefix = $"{baseTopic}_{period}";
        _finalTopic = $"{prefix}_final";
        _liveTopic = $"{prefix}_live";
    }

    public async Task<List<T>> ToListAsync(IReadOnlyList<string> pkFilter, CancellationToken ct)
    {
        if (pkFilter == null) throw new ArgumentNullException(nameof(pkFilter));
        List<T>? finalRows = null;
        var final = _ctx.Set<T>(_finalTopic, _period);
        try
        {
            finalRows = await final.ToListAsync(pkFilter, ct);
        }
        catch (InvalidOperationException)
        {
        }

        List<T>? liveRows = null;
        var live = _ctx.Set<T>(_liveTopic, _period);
        try
        {
            liveRows = await live.ToListAsync(pkFilter, ct);
        }
        catch (InvalidOperationException)
        {
        }

        if (finalRows == null && liveRows == null)
            throw new InvalidOperationException("No rows matched the filter.");

        var list = new List<T>();
        if (finalRows != null) list.AddRange(finalRows);
        if (liveRows != null) list.AddRange(liveRows);
        return list;
    }

    internal string FinalTopicName => _finalTopic;
    internal string LiveTopicName => _liveTopic;
}

/// <summary>
/// Writer counterpart to <see cref="TimeBucket{T}"/> for importing bars.
/// </summary>
public sealed class TimeBucketWriter<T> where T : class
{
    private readonly ITimeBucketWriteContext _ctx;
    private readonly string _finalTopic;
    private readonly string _liveTopic;

    internal TimeBucketWriter(ITimeBucketWriteContext ctx, Period period)
    {
        _ctx = ctx ?? throw new ArgumentNullException(nameof(ctx));
        var baseTopic = typeof(T).Name.ToLowerInvariant();
        var prefix = $"{baseTopic}_{period}";
        _finalTopic = $"{prefix}_final";
        _liveTopic = $"{prefix}_live";
    }

    public Task WriteAsync(T row, bool toFinal, CancellationToken ct = default)
        => _ctx.ProduceAsync(toFinal ? _finalTopic : _liveTopic, row, ct);

    internal string FinalTopicName => _finalTopic;
    internal string LiveTopicName => _liveTopic;
}
