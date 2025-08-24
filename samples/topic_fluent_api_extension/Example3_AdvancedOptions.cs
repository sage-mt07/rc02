using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;
using System;

namespace Samples.TopicFluentApiExtension;

// Advanced topic configuration using custom retention and cleanup policy
public static class Example3_AdvancedOptions
{
    [KsqlTopic("event_log", PartitionCount = 3, ReplicationFactor = 2)]
    private class EventLog
    {
        [KsqlKey(Order = 0)]
        public int Id { get; set; }
    }

    public static void Configure(ModelBuilder builder)
    {
        builder.Entity<EventLog>()
            .AsTable("event_log")
            .WithRetention(TimeSpan.FromDays(3))
            .WithCleanupPolicy("compact");
    }
}
