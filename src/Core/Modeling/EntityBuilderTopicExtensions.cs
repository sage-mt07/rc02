using Kafka.Ksql.Linq.Core.Abstractions;
using System;

namespace Kafka.Ksql.Linq.Core.Modeling;

/// <summary>
/// Extensions to expose topic configuration fluent API on <see cref="IEntityBuilder{T}"/>.
/// </summary>
public static class EntityBuilderTopicExtensions
{

    public static IEntityBuilder<T> WithPartitions<T>(this IEntityBuilder<T> builder, int partitions) where T : class
    {
        if (builder is not EntityModelBuilder<T> concrete)
            throw new ArgumentException("Invalid builder type", nameof(builder));
        return concrete.WithPartitions(partitions);
    }

    public static IEntityBuilder<T> WithReplicationFactor<T>(this IEntityBuilder<T> builder, int replicationFactor) where T : class
    {
        if (builder is not EntityModelBuilder<T> concrete)
            throw new ArgumentException("Invalid builder type", nameof(builder));
        return concrete.WithReplicationFactor(replicationFactor);
    }


    public static IEntityBuilder<T> WithPartitioner<T>(this IEntityBuilder<T> builder, string partitioner) where T : class
    {
        if (builder is not EntityModelBuilder<T> concrete)
            throw new ArgumentException("Invalid builder type", nameof(builder));
        return concrete.WithPartitioner(partitioner);
    }
}

