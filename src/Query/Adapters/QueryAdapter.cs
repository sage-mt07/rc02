using System;
using System.Collections.Generic;
using System.Linq;
using Kafka.Ksql.Linq.Query.Analysis;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal static class QueryAdapter
{
    public static IReadOnlyList<QuerySpec> Build(IReadOnlyList<DerivedEntity> entities, DerivationDag dag)
    {
        var specs = new List<QuerySpec>();
        foreach (var e in entities)
        {
            if (string.IsNullOrWhiteSpace(e.Id))
                throw new InvalidOperationException("TargetId must not be empty");
            dag.AddNode(e.Id);
            var sources = dag.Edges.TryGetValue(e.Id, out var src) ? src.ToList() : new List<string>();
            var op = e.Role switch
            {
                Role.AggFinal => $"Window(TUMBLING,{e.Timeframe.Value}{e.Timeframe.Unit})+Emit(FINAL+GRACE)",
                Role.Live => $"Window(TUMBLING,{e.Timeframe.Value}{e.Timeframe.Unit})+Emit(CHANGES)",
                Role.Final => "Compose(AggFinal⟂Prev1m)",
                _ => string.Empty
            };
            var projector = e.Role == Role.AggFinal ? "BucketStartFromWindowStart" : null;
            var spec = new QuerySpec
            {
                TargetId = e.Id,
                Sources = sources,
                Operation = op,
                Projector = projector,
                Sync = e.SyncHint,
                BasedOnRef = e.BasedOnSpec,
                ColumnPlan = e.ValueShape.Select(v => v.Name).ToArray()
            };
            specs.Add(spec);
        }
        return specs;
    }
}
