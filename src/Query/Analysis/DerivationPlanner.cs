using System;
using System.Collections.Generic;
using System.Linq;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivationPlanner
{
    public static IReadOnlyList<DerivedEntity> Plan(TumblingQao qao)
    {
        var entities = new List<DerivedEntity>();

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
                BasedOnSpec = qao.BasedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(agg);

            var live = new DerivedEntity
            {
                Id = liveId,
                Role = Role.Live,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = tf.Unit == "m" && tf.Value == 1 ? "10sAgg" : tf.Unit == "wk" ? "bar_1m_final" : "bar_1m_live",
                SyncHint = tf.Unit == "m" && tf.Value == 1 ? "HB_1m" : null,
                BasedOnSpec = qao.BasedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(live);

            var final = new DerivedEntity
            {
                Id = finalId,
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = tf.Unit == "m" && tf.Value == 1 ? "10sAgg" : tf.Unit == "wk" ? "bar_1m_final" : "bar_1m_live",
                SyncHint = tf.Unit == "m" && tf.Value == 1 ? "HB_1m" : null,
                BasedOnSpec = qao.BasedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(final);

            if (tf.Unit == "m" && tf.Value == 1 && prev == null)
            {
                prev = new DerivedEntity
                {
                    Id = "bar_prev_1m",
                    Role = Role.Prev1m,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    BasedOnSpec = qao.BasedOn,
                    WeekAnchor = qao.WeekAnchor
                };
                entities.Add(prev);

                var hb = new DerivedEntity
                {
                    Id = "hb_1m",
                    Role = Role.Hb,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = Array.Empty<ColumnShape>(),
                    MaterializationHint = MaterializationHint.Stream,
                    BasedOnSpec = qao.BasedOn,
                    WeekAnchor = qao.WeekAnchor
                };
                entities.Add(hb);
            }
        }
        return entities;
    }
}
