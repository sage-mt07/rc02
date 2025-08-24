using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;
using System;

namespace Samples.TopicFluentApiExtension;

/// <summary>
/// Additional topic configuration helpers for demonstration purposes.
/// </summary>
public static class TopicAdvancedExtensions
{
    public static IEntityBuilder<T> WithRetention<T>(this IEntityBuilder<T> builder, TimeSpan retention) where T : class
    {
        if (builder is not EntityModelBuilder<T> concrete)
            throw new ArgumentException("Invalid builder type", nameof(builder));

        var model = concrete.GetModel();
        EnsureTopicAttribute(model).RetentionMs = (long)retention.TotalMilliseconds;
        return concrete;
    }

    public static IEntityBuilder<T> WithCleanupPolicy<T>(this IEntityBuilder<T> builder, string policy) where T : class
    {
        if (builder is not EntityModelBuilder<T> concrete)
            throw new ArgumentException("Invalid builder type", nameof(builder));

        var model = concrete.GetModel();
        EnsureTopicAttribute(model).Compaction = policy.Equals("compact", StringComparison.OrdinalIgnoreCase);
        return concrete;
    }

    private static TopicAttribute EnsureTopicAttribute(EntityModel model)
    {
        if (model.TopicAttribute == null)
            model.TopicAttribute = new TopicAttribute(model.EntityType.Name.ToLowerInvariant());
        return model.TopicAttribute;
    }
}
