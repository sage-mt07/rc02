using System.Collections.Generic;

namespace Kafka.Ksql.Linq.Query.Ddl;

public class DdlSchemaBuilder
{
    private readonly string _objectName;
    private readonly DdlObjectType _objectType;
    private readonly string _topicName;
    private readonly int _partitions;
    private short _replicas;
    private readonly List<ColumnDefinition> _columns = new();
    private int? _keySchemaId;
    private int? _valueSchemaId;

    public DdlSchemaBuilder(string objectName, DdlObjectType objectType, string topicName, int partitions = 1, short replicas = 1)
    {
        _objectName = objectName;
        _objectType = objectType;
        _topicName = topicName;
        _partitions = partitions;
        _replicas = replicas;
    }

    public DdlSchemaBuilder WithReplicas(short replicas)
    {
        _replicas = replicas;
        return this;
    }

    public DdlSchemaBuilder AddColumn(string name, string type, bool isKey = false)
    {
        _columns.Add(new ColumnDefinition(name, type, isKey));
        return this;
    }

    public DdlSchemaBuilder WithSchemaIds(int? keySchemaId, int? valueSchemaId)
    {
        _keySchemaId = keySchemaId;
        _valueSchemaId = valueSchemaId;
        return this;
    }

    public DdlSchemaDefinition Build()
    {
        return new DdlSchemaDefinition(
            _objectName,
            _topicName,
            _objectType,
            _partitions,
            _replicas,
            _keySchemaId,
            _valueSchemaId,
            _columns);
    }
}
