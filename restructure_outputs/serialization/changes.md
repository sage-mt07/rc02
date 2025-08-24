# Serialization Namespace Restructure Changes

- Deprecated custom `AvroSerializer` and `AvroDeserializer` implementations.
- Introduced `ISerializerFactory` and `IDeserializerFactory` interfaces.
- Added `ConfluentSerializerFactory` which wraps Confluent.SchemaRegistry Avro serializers.
- All serializers/deserializers are now created through this factory.
