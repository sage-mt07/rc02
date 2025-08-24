using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;
using System;
using System.Linq.Expressions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests;

public class ApplyModelBuilderSettingsTests
{
    private class Sample
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
    }

    private static EntityModel BuildModel()
    {
        var modelBuilder = new ModelBuilder();

        // Simulate Set<T>() call which registers the model
        modelBuilder.Entity<Sample>();

        var builder = (EntityModelBuilder<Sample>)modelBuilder.Entity<Sample>()
            .AsTable(useCache: false);
        builder.OnError(ErrorAction.DLQ);
        var model = builder.GetModel();
        model.DeserializationErrorPolicy = DeserializationErrorPolicy.DLQ;
        model.BarTimeSelector = (Expression<Func<Sample, DateTime>>)(x => x.Time);
        return modelBuilder.GetEntityModel<Sample>()!;
    }

    [Fact]
    public void AutoRegisteredModel_PropertiesFromModelBuilderAreApplied()
    {
        var model = BuildModel();

        Assert.Equal(ErrorAction.DLQ, model.ErrorAction);
        Assert.Equal(DeserializationErrorPolicy.DLQ, model.DeserializationErrorPolicy);
        Assert.False(model.EnableCache);
        Assert.NotNull(model.BarTimeSelector);
    }
}
