using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class TumblingAnalyzer
{
    public static TumblingQao Analyze(Expression expr, Type sourceType)
    {
        var visitor = new MethodCallCollectorVisitor();
        visitor.Visit(expr);
        var res = visitor.Result;

        if (res.Windows.Count == 0)
            throw new InvalidOperationException("Tumbling windows are required");
        if (res.TimeKey == null)
            throw new InvalidOperationException("Time key is required");
        if (!res.GroupByKeys.Contains(res.TimeKey) && !res.GroupByKeys.Contains("BucketStart"))
            throw new InvalidOperationException("Time key must be part of keys");
        if (res.BasedOnJoinKeys.Count == 0 || res.BasedOnOpen == null || res.BasedOnClose == null || res.BasedOnDayKey == null)
            throw new InvalidOperationException("TimeFrame is required");

        var windows = res.Windows.Distinct().Select(ParseWindow).OrderBy(tf => ToMinutes(tf)).ToList();
        var keys = res.GroupByKeys.ToArray();
        var projection = res.GroupByKeys.ToArray();
        var basedOn = new BasedOnSpec(res.BasedOnJoinKeys.ToArray(), res.BasedOnOpen, res.BasedOnClose, res.BasedOnDayKey, res.BasedOnOpenInclusive, res.BasedOnCloseInclusive);

        var nullCtx = new NullabilityInfoContext();
        var pocoShape = sourceType.GetProperties()
            .OrderBy(p => p.MetadataToken)
            .Select(p => {
                var nullable = nullCtx.Create(p).WriteState == NullabilityState.Nullable;
                return new ColumnShape(p.Name, p.PropertyType, nullable);
            })
            .ToArray();

        return new TumblingQao
        {
            TimeKey = res.TimeKey,
            Windows = windows,
            Keys = keys,
            Projection = projection,
            PocoShape = pocoShape,
            BasedOn = basedOn
        };
    }

    private static Timeframe ParseWindow(string w)
    {
        if (w.EndsWith("mo"))
            return new Timeframe(int.Parse(w[..^2]), "mo");
        if (w.EndsWith("wk"))
            return new Timeframe(int.Parse(w[..^2]), "wk");
        var unit = w[^1].ToString();
        var value = int.Parse(w[..^1]);
        return new Timeframe(value, unit);
    }

    private static int ToMinutes(Timeframe tf) => tf.Unit switch
    {
        "m" => tf.Value,
        "h" => tf.Value * 60,
        "d" => tf.Value * 1440,
        "wk" => tf.Value * 10080,
        "mo" => tf.Value * 43200,
        _ => tf.Value
    };
}
