# Messaging Namespace Restructure Changes

## Removed Files
- `src/Messaging/Producers/Core/PooledProducer.cs`
- `src/Messaging/Consumers/Core/PooledConsumer.cs`
- `src/Messaging/Consumers/Core/ConsumerInstance.cs`
- `src/Messaging/Producers/Exception/ProducerPoolException.cs`
- `src/Messaging/Consumers/Exceptions/ConsumerPoolException.cs`

## Added Interfaces
- `src/Messaging/Abstractions/IKafkaProducerFactory.cs`
- `src/Messaging/Abstractions/IKafkaConsumerFactory.cs`

## Added Implementations
- `src/Messaging/Producers/KafkaProducerFactory.cs`
- `src/Messaging/Consumers/KafkaConsumerFactory.cs`

Interface definitions reflecting the latest design are saved as `IKafkaProducer_latest.cs` and `IKafkaConsumer_latest.cs` in this folder.
