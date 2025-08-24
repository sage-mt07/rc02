using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Abstractions;
using System;
 

namespace Kafka.Ksql.Linq.Core.Modeling;

public class EntityModelBuilder<T> : IEntityBuilder<T> where T : class
{
    private readonly EntityModel _entityModel;

    internal EntityModelBuilder(EntityModel entityModel)
    {
        _entityModel = entityModel ?? throw new ArgumentNullException(nameof(entityModel));
    }
    public IEntityBuilder<T> AsTable(string? topicName = null, bool useCache = true)
    {
        _entityModel.SetStreamTableType(StreamTableType.Table);
        _entityModel.EnableCache = useCache;
        if (!string.IsNullOrWhiteSpace(topicName))
        {
            _entityModel.TopicName = topicName.ToLowerInvariant();
        }
        return this;
    }
    public IEntityBuilder<T> AsStream()
    {
        _entityModel.SetStreamTableType(StreamTableType.Stream);
        return this;
    }

    public EntityModelBuilder<T> OnError(ErrorAction action)
    {
        _entityModel.ErrorAction = action;
        return this;
    }



    public EntityModelBuilder<T> WithReplicationFactor(int replicationFactor)
    {
        if (replicationFactor <= 0)
            throw new ArgumentException("ReplicationFactor must be greater than 0", nameof(replicationFactor));
        _entityModel.ReplicationFactor = (short)replicationFactor;
        return this;
    }


    public EntityModelBuilder<T> WithPartitioner(string partitioner)
    {
        if (string.IsNullOrWhiteSpace(partitioner))
            throw new ArgumentException("Partitioner cannot be null or empty", nameof(partitioner));

        // partitioner info removed in new API
        return this;
    }

    public EntityModel GetModel()
    {
        return _entityModel;
    }

    [Obsolete("Changing the topic name via Fluent API is prohibited in POCO attribute-driven mode. Use the [Topic] attribute instead.", true)]
    public EntityModelBuilder<T> HasTopicName(string topicName)
    {
        throw new NotSupportedException("Changing the topic name via Fluent API is prohibited in POCO attribute-driven mode. Use the [Topic] attribute instead.");
    }

    public override string ToString()
    {
        var entityName = _entityModel.EntityType.Name;
        var topicName = _entityModel.TopicName ?? "undefined";
        var keyCount = _entityModel.KeyProperties.Length;
        var validStatus = _entityModel.IsValid ? "valid" : "invalid";

        return $"Entity: {entityName}, Topic: {topicName}, Keys: {keyCount}, Status: {validStatus}";
    }

}
