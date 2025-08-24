using System.Collections.Generic;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Query.Abstractions;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal static class EntityModelRegistrar
{
    public static void Register(MappingRegistry registry, IReadOnlyList<EntityModel> models)
    {
        foreach (var m in models)
        {
            var force = m.AdditionalSettings.TryGetValue("forceStream", out var fs) && fs is bool b && b;
            if (force)
                m.SetStreamTableType(StreamTableType.Stream);
            else if (m.AdditionalSettings.TryGetValue("keys", out var k) && k is string[] arr && arr.Length > 0)
                m.SetStreamTableType(StreamTableType.Table);
            else
                m.SetStreamTableType(StreamTableType.Stream);
            registry.RegisterEntityModel(m);
        }
    }
}
