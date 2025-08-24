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

public static class TimeBucket
{
    public static TimeBucket<T> Get<T>(ITimeBucketContext ctx, Period period) where T : class
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

