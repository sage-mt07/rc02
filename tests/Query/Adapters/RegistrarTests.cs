using System.Collections.Generic;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Query.Adapters;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Adapters;

public class RegistrarTests
{
    [Fact]
    public void Registrar_Defaults_Table_When_HasPK_And_ForceStream_For_HB()
    {
        var hb = new EntityModel { EntityType = typeof(object) };
        hb.AdditionalSettings["keys"] = new[] { "K" };
        hb.AdditionalSettings["forceStream"] = true;
        var live = new EntityModel { EntityType = typeof(object) };
        live.AdditionalSettings["keys"] = new[] { "K" };
        var registry = new MappingRegistry();
        EntityModelRegistrar.Register(registry, new List<EntityModel> { hb, live });
        Assert.Equal(StreamTableType.Stream, hb.GetExplicitStreamTableType());
        Assert.Equal(StreamTableType.Table, live.GetExplicitStreamTableType());
    }
}
