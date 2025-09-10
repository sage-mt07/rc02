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
                "s" => w.Value / 60m,
                "m" => w.Value,
                "h" => w.Value * 60,
                "d" => w.Value * 1440,
                "wk" => w.Value * 10080,
                "mo" => w.Value * 43200m,
                _ => w.Value
            })
            .ToList();
        if (!windows.Any(w => w.Unit == "s" && w.Value == 1))
            windows.Insert(0, new Timeframe(1, "s"));
        var graceMap = new Dictionary<string, int>();
        var prevGrace = qao.GraceSeconds ?? 0;
        foreach (var tf in windows)
        {
            var key = $"{tf.Value}{tf.Unit}";
            if (qao.GracePerTimeframe.TryGetValue(key, out var parent))
                prevGrace = parent;
            var next = prevGrace + 1;
            graceMap[key] = next;
            prevGrace = next;
        }
        qao.GracePerTimeframe.Clear();
        foreach (var kv in graceMap)
            qao.GracePerTimeframe[kv.Key] = kv.Value;
        var hub = $"{baseId}_1s_final_s";
        foreach (var tf in windows)
        {
            var tfStr = $"{tf.Value}{tf.Unit}";
            var hbId = $"{baseId}_hb_{tfStr}";
            if (tf.Unit == "s" && tf.Value == 1)
            {
                var final1s = new DerivedEntity
                {
                    Id = $"{baseId}_1s_final",
                    Role = Role.Final1s,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = graceMap[tfStr]
                };
                entities.Add(final1s);

                var final1sStream = new DerivedEntity
                {
                    Id = hub,
                    Role = Role.Final1sStream,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = valueShapes,
                    InputHint = $"{baseId}_1s_final",
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = graceMap[tfStr]
                };
                entities.Add(final1sStream);

                var hb1s = new DerivedEntity
                {
                    Id = hbId,
                    Role = Role.Hb,
                    Timeframe = tf,
                    KeyShape = keyShapes,
                    ValueShape = Array.Empty<ColumnShape>(),
                    MaterializationHint = MaterializationHint.Stream,
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = graceMap[tfStr]
                };
                entities.Add(hb1s);
                continue;
            }

            var liveId = $"{baseId}_{tfStr}_live";
            var finalId = $"{baseId}_{tfStr}_final";
            var live = new DerivedEntity
            {
                Id = liveId,
                Role = Role.Live,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = hub,
                SyncHint = hbId.ToUpperInvariant(),
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor,
                GraceSeconds = graceMap[tfStr]
            };
            entities.Add(live);

            var final = new DerivedEntity
            {
                Id = finalId,
                Role = Role.Final,
                Timeframe = tf,
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = hub,
                SyncHint = hbId.ToUpperInvariant(),
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor,
                GraceSeconds = graceMap[tfStr]
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
                WeekAnchor = qao.WeekAnchor,
                GraceSeconds = graceMap[tfStr]
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
                    InputHint = hub,
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = graceMap[tfStr]
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
                    InputHint = hub,
                    SyncHint = hbId.ToUpperInvariant(),
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = graceMap[tfStr]
                };
                entities.Add(prev);
            }
        }
        if (!entities.Any(e => e.Role == Role.Prev1m))
        {
            var hbId = $"{baseId}_hb_1m";
            var grace1m = graceMap.TryGetValue("1m", out var g1) ? g1 : graceMap.Values.Last() + 1;
            var prev = new DerivedEntity
            {
                Id = $"{baseId}_prev_1m",
                Role = Role.Prev1m,
                Timeframe = new Timeframe(1, "m"),
                KeyShape = keyShapes,
                ValueShape = valueShapes,
                InputHint = hub,
                SyncHint = hbId.ToUpperInvariant(),
                BasedOnSpec = basedOn,
                WeekAnchor = qao.WeekAnchor,
                GraceSeconds = grace1m
            };
            entities.Add(prev);
            if (!entities.Any(e => e.Id == hbId))
            {
                var hb = new DerivedEntity
                {
                    Id = hbId,
                    Role = Role.Hb,
                    Timeframe = new Timeframe(1, "m"),
                    KeyShape = keyShapes,
                    ValueShape = Array.Empty<ColumnShape>(),
                    MaterializationHint = MaterializationHint.Stream,
                    BasedOnSpec = basedOn,
                    WeekAnchor = qao.WeekAnchor,
                    GraceSeconds = grace1m
                };
                entities.Add(hb);
            }
        }
        return entities;
    }
}
