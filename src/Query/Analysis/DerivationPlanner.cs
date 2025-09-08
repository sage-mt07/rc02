using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivationPlanner
{
    public static IReadOnlyList<DerivedEntity> Plan(TumblingQao qao, EntityModel model)
    {
        var entities = new List<DerivedEntity>();

        var keyShapes = qao.Keys.Select(k =>
        {
            var match = qao.PocoShape.FirstOrDefault(p => p.Name == k)
                ?? throw new InvalidOperationException($"Key property '{k}' not found");
            return match;
        }).ToArray();
        var valueShapes = qao.PocoShape.ToArray();

        var basedOn = qao.BasedOn;
        if (string.IsNullOrEmpty(basedOn.CloseProp))
        {
            var close = model.EntityType
                .GetProperties()
                .FirstOrDefault(p => p.GetCustomAttribute<KsqlTimeFrameCloseAttribute>() != null);
            if (close != null)
            {
                basedOn = basedOn with { CloseProp = close.Name };
            }
        }

        foreach (var tf in qao.Windows)
        {
            var tfStr = $"{tf.Value}{tf.Unit}";
            var topicAttr = model.EntityType.GetCustomAttribute<KsqlTopicAttribute>();
            var baseId = (topicAttr?.Name ?? model.TopicName ?? model.EntityType.Name).ToLowerInvariant();
            var aggId = $"{baseId}_{tfStr}_agg_final";
            var liveId = $"{baseId}_{tfStr}_live";
            var finalId = $"{baseId}_{tfStr}_final";
            var hbId = $"{baseId}_hb_{tfStr}";

            var agg = new DerivedEntity
            {
                Id = aggId,
                Role = Role.AggFinal,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(agg);

            var liveInput = tf.Unit == "m" && tf.Value == 1 ? "10sAgg" : tf.Unit == "wk" ? $"{baseId}_1m_final" : $"{baseId}_1m_live";
            var liveSync = hbId.ToUpperInvariant();
            var live = new DerivedEntity
            {
                Id = liveId,
                Role = Role.Live,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = liveInput,
                SyncHint = liveSync,
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(live);

            var finalInput = aggId;
            var final = new DerivedEntity
            {
                Id = finalId,
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = finalInput,
                SyncHint = $"{baseId}_prev_1m",
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(final);

            var hb = new DerivedEntity
            {
                Id = hbId,
                Role = Role.Hb,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = Array.Empty<ColumnShape>(),
                MaterializationHint = MaterializationHint.Stream,
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(hb);

            if (tf.Unit == "m" && tf.Value == 1)
            {
                var prev = new DerivedEntity
                {
                    Id = $"{baseId}_prev_1m",
                    Role = Role.Prev1m,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    InputHint = $"{baseId}_1m_final",
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor
                };
                entities.Add(prev);
            }
        }
        return entities;
    }
}
