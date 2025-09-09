using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivationPlanner
{
    public static IReadOnlyList<DerivedEntity> Plan(TumblingQao qao, EntityModel model, bool whenEmpty = false)
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

        var topicAttr = model.EntityType.GetCustomAttribute<KsqlTopicAttribute>();
        var baseId = (topicAttr?.Name ?? model.TopicName ?? model.EntityType.Name).ToLowerInvariant();
        var windows = qao.Windows
            .OrderBy(w => w.Unit switch
            {
                "m" => w.Value,
                "h" => w.Value * 60,
                "d" => w.Value * 1440,
                "wk" => w.Value * 10080,
                _ => w.Value
            })
            .ToArray();
        string? prevFinalId = null;
        foreach (var tf in windows)
        {
            var tfStr = $"{tf.Value}{tf.Unit}";
            var liveId = $"{baseId}_{tfStr}_live";
            var finalId = $"{baseId}_{tfStr}_final";
            var hbId = $"{baseId}_hb_{tfStr}";

            string? liveInput = tf.Unit == "m" && tf.Value == 1
                ? null
                : tf.Unit == "wk"
                    ? $"{baseId}_1d_live"
                    : $"{baseId}_1m_live";
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

            var finalInput = prevFinalId ?? baseId;
            var final = new DerivedEntity
            {
                Id = finalId,
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = finalInput,
                SyncHint = hbId.ToUpperInvariant(),
                PrevHint = $"{baseId}_prev_1m",
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(final);

            prevFinalId = finalId;

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

            if (whenEmpty)
            {
                var fill = new DerivedEntity
                {
                    Id = $"{baseId}_{tfStr}_fill",
                    Role = Role.Fill,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    InputHint = finalId,
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor
                };
                entities.Add(fill);
            }

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
        if (!windows.Any(w => w.Unit == "m" && w.Value == 1))
        {
            var tf = new Timeframe(1, "m");
            var live1m = new DerivedEntity
            {
                Id = $"{baseId}_1m_live",
                Role = Role.Live,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = null,
                SyncHint = $"{baseId}_hb_1m".ToUpperInvariant(),
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(live1m);

            prevFinalId = null;
            var finalInput = prevFinalId ?? baseId;
            var final1m = new DerivedEntity
            {
                Id = $"{baseId}_1m_final",
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = finalInput,
                SyncHint = $"{baseId}_hb_1m".ToUpperInvariant(),
                PrevHint = $"{baseId}_prev_1m",
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(final1m);

            var hb1m = new DerivedEntity
            {
                Id = $"{baseId}_hb_1m",
                Role = Role.Hb,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = Array.Empty<ColumnShape>(),
                MaterializationHint = MaterializationHint.Stream,
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(hb1m);

            var prev = new DerivedEntity
            {
                Id = $"{baseId}_prev_1m",
                Role = Role.Prev1m,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = final1m.Id,
                SyncHint = $"{baseId}_hb_1m".ToUpperInvariant(),
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor
            };
            entities.Add(prev);
        }
        return entities;
    }
}
