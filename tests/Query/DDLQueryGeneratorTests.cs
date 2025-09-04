using Kafka.Ksql.Linq.Query.Ddl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query;

public class DdlColumnDefinitionsTests
{
    private sealed class SchemaProvider : IDdlSchemaProvider
    {
        private readonly DdlSchemaDefinition _schema;
        public SchemaProvider(DdlSchemaDefinition schema) => _schema = schema;
        public DdlSchemaDefinition GetSchema() => _schema;
    }

    [Fact]
    public void CreateStream_SingleKey_UsesKeyModifier()
    {
        var schema = new DdlSchemaBuilder("orders", DdlObjectType.Stream, "orders", 1, 1)
            .AddColumn("Id", "INT", isKey: true)
            .AddColumn("Name", "VARCHAR")
            .Build();
        var gen = new DDLQueryGenerator();
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            var sql = gen.GenerateCreateStream(new SchemaProvider(schema));
            Assert.Contains("CREATE STREAM IF NOT EXISTS orders (Id INT KEY, Name VARCHAR)", sql);
            Assert.DoesNotContain("PRIMARY KEY", sql);
        }
    }

    [Fact]
    public void CreateTable_SingleKey_UsesPrimaryKey()
    {
        var schema = new DdlSchemaBuilder("orders", DdlObjectType.Table, "orders", 1, 1)
            .AddColumn("Id", "INT", isKey: true)
            .AddColumn("Name", "VARCHAR")
            .Build();
        var gen = new DDLQueryGenerator();
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            var sql = gen.GenerateCreateTable(new SchemaProvider(schema));
            Assert.Contains("CREATE TABLE IF NOT EXISTS orders (Id INT PRIMARY KEY, Name VARCHAR)", sql);
        }
    }

    [Fact]
    public void CreateStream_MultiKey_StructKey_QuotesReservedFields()
    {
        var schema = new DdlSchemaBuilder("dead_letter_queue", DdlObjectType.Stream, "dead-letter-queue", 1, 1)
            .AddColumn("Topic", "VARCHAR", isKey: true)
            .AddColumn("Partition", "INT", isKey: true)
            .AddColumn("Offset", "BIGINT", isKey: true)
            .AddColumn("ErrorMessage", "VARCHAR")
            .Build();
        var gen = new DDLQueryGenerator();
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            var sql = gen.GenerateCreateStream(new SchemaProvider(schema));
            Assert.Contains("STRUCT<`Topic` VARCHAR, `Partition` INT, `Offset` BIGINT> KEY", sql);
            Assert.Contains("ErrorMessage VARCHAR", sql);
        }
    }

    [Fact]
    public void WithClause_UsesValueSchemaOnly_NoKeySchemaFullName()
    {
        var schema = new DdlSchemaBuilder("orders", DdlObjectType.Stream, "orders", 1, 1)
            .AddColumn("Id", "INT", isKey: true)
            .AddColumn("Name", "VARCHAR")
            .WithSchemaFullNames(keySchemaFullName: "my.key.FullName", valueSchemaFullName: "my.value.FullName")
            .Build();
        var gen = new DDLQueryGenerator();
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            var sql = gen.GenerateCreateStream(new SchemaProvider(schema));
            Assert.Contains("VALUE_AVRO_SCHEMA_FULL_NAME='my.value.FullName'", sql);
            Assert.DoesNotContain("KEY_AVRO_SCHEMA_FULL_NAME", sql);
        }
    }
}
