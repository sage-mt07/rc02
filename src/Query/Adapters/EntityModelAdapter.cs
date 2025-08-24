using System;
using System.Collections.Generic;
using System.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Analysis;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal static class EntityModelAdapter
{
    public static IReadOnlyList<EntityModel> Adapt(IReadOnlyList<DerivedEntity> entities)
    {
        var list = new List<EntityModel>();
        foreach (var e in entities)
        {
            var keys = e.KeyShape.Select(k => k.Name).ToArray();
            var values = e.ValueShape.Select(v => v.Name).ToArray();
            var types = e.ValueShape.Select(v => v.Type).ToArray();
            var nulls = e.ValueShape.Select(v => v.IsNullable).ToArray();
            if (keys.Length == 0 || (values.Length == 0 && e.Role != Role.Hb))
                throw new InvalidOperationException("Key and value must not be empty");

            var model = new EntityModel { EntityType = typeof(object) };
            model.AdditionalSettings["id"] = e.Id;
            model.AdditionalSettings["keys"] = keys;
            model.AdditionalSettings["projection"] = values;
            model.AdditionalSettings["projection/types"] = types;
            model.AdditionalSettings["projection/nulls"] = nulls;
            model.AdditionalSettings["basedOn/joinKeys"] = e.BasedOnSpec.JoinKeys.ToArray();
            model.AdditionalSettings["basedOn/openProp"] = e.BasedOnSpec.OpenProp;
            model.AdditionalSettings["basedOn/closeProp"] = e.BasedOnSpec.CloseProp;
            model.AdditionalSettings["basedOn/dayKey"] = e.BasedOnSpec.DayKey;
            model.AdditionalSettings["basedOn/openInclusive"] = e.BasedOnSpec.IsOpenInclusive;
            model.AdditionalSettings["basedOn/closeInclusive"] = e.BasedOnSpec.IsCloseInclusive;
            model.AdditionalSettings["role"] = e.Role.ToString();
            model.AdditionalSettings["timeframe"] = $"{e.Timeframe.Value}{e.Timeframe.Unit}";
            if (e.SyncHint != null) model.AdditionalSettings[$"sync"] = e.SyncHint;
            if (e.InputHint != null) model.AdditionalSettings[$"input"] = e.InputHint;
            if (e.TopicHint != null) model.AdditionalSettings[$"topicCandidate"] = e.TopicHint;
            if (e.Role == Role.Hb) model.AdditionalSettings["forceStream"] = true;
            list.Add(model);
        }
        return list;
    }
}
