using System;
using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivationPlanner
{
    public static (IReadOnlyList<DerivedEntity>, DerivationDag) Plan(TumblingQao qao)
    {
        var entities = new List<DerivedEntity>();
        var dag = new DerivationDag();

        var keyShapes = qao.Keys.Select(k =>
        {
            var match = qao.PocoShape.FirstOrDefault(p => p.Name == k)
                ?? throw new InvalidOperationException($"Key property '{k}' not found");
            return match;
        }).ToArray();
        var valueShapes = qao.PocoShape.ToArray();

        DerivedEntity? prev = null;
        foreach (var tf in qao.Windows)
        {
            var tfStr = $"{tf.Value}{tf.Unit}";
            var aggId = $"bar_{tfStr}_agg_final";
            var liveId = $"bar_{tfStr}_live";
            var finalId = $"bar_{tfStr}_final";

            var agg = new DerivedEntity
            {
                Id = aggId,
                Role = Role.AggFinal,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                BasedOnSpec = qao.BasedOn
            };
            entities.Add(agg); dag.AddNode(aggId);

            var live = new DerivedEntity
            {
                Id = liveId,
                Role = Role.Live,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = tf.Unit == "m" && tf.Value == 1 ? "10sAgg" : tf.Unit == "wk" ? "bar_1m_final" : "bar_1m_live",
                SyncHint = tf.Unit == "m" && tf.Value == 1 ? "HB_1m" : null,
                BasedOnSpec = qao.BasedOn
            };
            entities.Add(live); dag.AddNode(liveId);

            var final = new DerivedEntity
            {
                Id = finalId,
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = tf.Unit == "m" && tf.Value == 1 ? "bar_1m_agg_final ⟂ bar_prev_1m" : $"bar_{tfStr}_agg_final ⟂ bar_prev_1m",
                SyncHint = tf.Unit == "m" && tf.Value == 1 ? "HB_1m" : null,
                BasedOnSpec = qao.BasedOn
            };
            entities.Add(final); dag.AddNode(finalId);

            dag.AddEdge(aggId, finalId);
            dag.AddEdge("bar_prev_1m", finalId);
            if (tf.Unit == "wk")
                dag.AddEdge("bar_1m_final", liveId);
            else if (!(tf.Unit == "m" && tf.Value == 1))
                dag.AddEdge("bar_1m_live", liveId);

            if (tf.Unit == "m" && tf.Value == 1 && prev == null)
            {
                prev = new DerivedEntity
                {
                    Id = "bar_prev_1m",
                    Role = Role.Prev1m,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    BasedOnSpec = qao.BasedOn
                };
                entities.Add(prev); dag.AddNode(prev.Id);

                var hb = new DerivedEntity
                {
                    Id = "hb_1m",
                    Role = Role.Hb,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = Array.Empty<ColumnShape>(),
                    MaterializationHint = MaterializationHint.Stream,
                    BasedOnSpec = qao.BasedOn
                };
                entities.Add(hb); dag.AddNode(hb.Id);
            }
        }
        return (entities, dag);
    }
}
