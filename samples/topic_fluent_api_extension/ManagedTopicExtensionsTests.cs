using System;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Configuration;
using Samples.TopicFluentApiExtension;
using Xunit;

public class ManagedTopicExtensionsTests
{
    private static readonly Type ModelBuilderType = typeof(IModelBuilder).Assembly.GetType("Kafka.Ksql.Linq.Core.Modeling.ModelBuilder")!;

    private class LogEntry
    {
        public string Id { get; set; } = string.Empty;
    }

    [Fact]
    public void ManagedFlag_IsTrue_WhenMarked()
    {
        dynamic modelBuilder = Activator.CreateInstance(ModelBuilderType, new object[] { ValidationMode.Strict })!;
        dynamic builder = modelBuilder.Entity<LogEntry>();
        builder.IsManaged(true);
        Assert.True(builder.GetIsManaged());
    }
}
