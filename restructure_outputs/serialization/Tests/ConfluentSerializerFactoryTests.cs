using Confluent.SchemaRegistry;
using Confluent.SchemaRegistry.Serdes;
using Kafka.Ksql.Linq.Serialization;
using Moq;
using Xunit;
using System;
namespace Serialization.Tests;

public class ConfluentSerializerFactoryTests
{
    private class Person
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void CreateSerializer_ReturnsAvroSerializer()
    {
        var client = new Mock<ISchemaRegistryClient>().Object;
        var factory = new ConfluentSerializerFactory(client);

        var serializer = factory.CreateSerializer<Person>();

        Assert.IsType<AvroSerializer<Person>>(serializer);
    }

    [Fact]
    public void CreateDeserializer_ReturnsAvroDeserializer()
    {
        var client = new Mock<ISchemaRegistryClient>().Object;
        var factory = new ConfluentSerializerFactory(client);

        var deserializer = factory.CreateDeserializer<Person>();

        Assert.IsType<AvroDeserializer<Person>>(deserializer);
    }

    [Fact]
    public void Constructor_NullClient_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => new ConfluentSerializerFactory(null!));
    }

    [Fact]
    public void Factory_CanBeReusedToCreateMultipleInstances()
    {
        var client = new Mock<ISchemaRegistryClient>().Object;
        var factory = new ConfluentSerializerFactory(client);

        var serializer1 = factory.CreateSerializer<Person>();
        var serializer2 = factory.CreateSerializer<Person>();
        var deserializer1 = factory.CreateDeserializer<Person>();
        var deserializer2 = factory.CreateDeserializer<Person>();

        Assert.NotSame(serializer1, serializer2);
        Assert.NotSame(deserializer1, deserializer2);
    }
}
