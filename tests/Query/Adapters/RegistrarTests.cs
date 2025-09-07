using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Query.Adapters;
using System.Collections.Generic;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Adapters;

public class RegistrarTests
{
    [Fact]
    public void Registrar_Defaults_Stream_When_HasPK()
    {
        var hb = new EntityModel { EntityType = typeof(object) };
        hb.AdditionalSettings["keys"] = new[] { "K" };
        hb.AdditionalSettings["forceStream"] = true;
        var live = new EntityModel { EntityType = typeof(object) };
        live.AdditionalSettings["keys"] = new[] { "K" };
        live.SetStreamTableType(StreamTableType.Table);
        var plain = new EntityModel { EntityType = typeof(object) };
        plain.AdditionalSettings["keys"] = new[] { "K" };
        var registry = new MappingRegistry();
        EntityModelRegistrar.Register(registry, new List<EntityModel> { hb, live, plain });
        Assert.Equal(StreamTableType.Stream, hb.GetExplicitStreamTableType());
        Assert.Equal(StreamTableType.Table, live.GetExplicitStreamTableType());
        Assert.Equal(StreamTableType.Stream, plain.GetExplicitStreamTableType());
    }
}
