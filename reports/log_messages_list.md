| File | Line | Severity | Message | Category | LogID |
|------|------|----------|---------|----------|-------|
| physicalTests/TestEnvironment.cs | 130 | Error | Failed to drop objects | Tests | TES-001 |
| physicalTests/TestEnvironment.cs | 168 | Warning | Failed to delete schema {Subject}: {StatusCode} | Tests | TES-002 |
| physicalTests/TestEnvironment.cs | 174 | Error | Failed to delete schema {Subject} | Tests | TES-003 |
| physicalTests/TestEnvironment.cs | 205 | Error | Service check failed | Tests | TES-004 |
| physicalTests/TestEnvironment.cs | 228 | Error | Failed to create DLQ topic: {Reason} | Tests | TES-005 |
| src/Cache/Core/ReadCachedEntitySet.cs | 33 | Warning | Table cache not available for {Entity} | Cache | CAC-001 |
| src/Cache/Core/RocksDbTableCache.cs | 27 | Information | Table cache for {Type} is RUNNING | Cache | CAC-002 |
| src/Cache/Core/RocksDbTableCache.cs | 46 | Information | Table cache for {Type} disposed | Cache | CAC-003 |
| src/Cache/Core/TableCacheRegistry.cs | 33 | Information | Initialized cache for {Entity} | Cache | CAC-004 |
| src/Core/Extensions/LoggerFactoryExtensions.cs | 62 | Debug | <message> | Core | COR-001 |
| src/Core/Extensions/LoggerFactoryExtensions.cs | 86 | Debug | <message> | Core | COR-002 |
| src/Core/Extensions/LoggerFactoryExtensions.cs | 110 | Information | <message> | Core | COR-003 |
| src/Core/Extensions/LoggerFactoryExtensions.cs | 133 | Warning | <message> | Core | COR-004 |
| src/Core/Extensions/LoggerFactoryExtensions.cs | 157 | Error | <exception> | Core | COR-005 |
| src/Core/Models/KeyExtractor.cs | 86 | Error | Failed to convert key value '{Value}' to {Type} | Core | COR-006 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 30 | Debug | KafkaAdminService initialized with BootstrapServers: {BootstrapServers} | Infrastructure | INF-001 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 47 | Debug | DLQ topic already exists: {DlqTopicName} | Infrastructure | INF-002 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 53 | Information | DLQ topic created successfully: {DlqTopicName} | Infrastructure | INF-003 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 57 | Error | Failed to ensure DLQ topic exists: {DlqTopicName} | Infrastructure | INF-004 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 68 | Debug | Topic already exists: {TopicName} | Infrastructure | INF-005 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 86 | Information | Topic created: {TopicName} | Infrastructure | INF-006 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 93 | Debug | Topic already exists (race): {TopicName} | Infrastructure | INF-007 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 130 | Warning | Failed to check topic existence: {TopicName} | Infrastructure | INF-008 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 149 | Debug | DB topic already exists: {Topic} | Infrastructure | INF-009 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 163 | Information | DB topic created: {Topic} | Infrastructure | INF-010 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 170 | Debug | DB topic already exists (race): {Topic} | Infrastructure | INF-011 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 189 | Information | Skipping DLQ topic creation because auto-creation is disabled: {TopicName} | Infrastructure | INF-012 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 218 | Information | DLQ topic created: {TopicName} with {RetentionMs}ms retention, {Partitions} partitions | Infrastructure | INF-013 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 227 | Debug | DLQ topic already exists (race condition): {TopicName} | Infrastructure | INF-014 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 249 | Debug | Kafka connectivity validated: {BrokerCount} brokers available | Infrastructure | INF-015 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 312 | Debug | KafkaAdminService disposed | Infrastructure | INF-016 |
| src/Infrastructure/Admin/KafkaAdminService.cs | 316 | Warning | Error disposing KafkaAdminService | Infrastructure | INF-017 |
| src/KsqlContext.cs | 301 | Information | Kafka initialization completed; DLQ topic '{Topic}' ready with 5-second retention | KsqlContext | KSQ-001 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 80 | Warning | Error consuming message from topic {TopicName} | Messaging | MES-001 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 145 | Error | Failed to consume batch: {EntityType} -> {Topic} | Messaging | MES-002 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 156 | Trace | Offset committed: {EntityType} -> {Topic} | Messaging | MES-003 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 160 | Error | Failed to commit offset: {EntityType} -> {Topic} | Messaging | MES-004 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 174 | Information | Seeked to offset: {EntityType} -> {TopicPartitionOffset} | Messaging | MES-005 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 178 | Error | Failed to seek to offset: {EntityType} -> {TopicPartitionOffset} | Messaging | MES-006 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 192 | Warning | Failed to get assigned partitions: {EntityType} | Messaging | MES-007 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 205 | Debug | Subscribed to topic: {EntityType} -> {Topic} | Messaging | MES-008 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 209 | Error | Failed to subscribe to topic: {EntityType} -> {Topic} | Messaging | MES-009 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 255 | Warning | Failed to deserialize key for topic {TopicName}, using default key | Messaging | MES-010 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 269 | Debug | Key/Value merge completed: {EntityType}, HasKeys: {HasKeys}, KeyType: {KeyType} | Messaging | MES-011 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 275 | Warning | Failed to merge key/value for topic {TopicName}, using value-only entity | Messaging | MES-012 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 323 | Warning | Deserialization failed for topic {Topic} | Messaging | MES-013 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 345 | Error | Failed to send deserialization error to DLQ | Messaging | MES-014 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 364 | Warning | Failed to extract correlation ID from headers | Messaging | MES-015 |
| src/Messaging/Consumers/Core/KafkaConsumer.cs | 386 | Warning | Error disposing consumer: {EntityType} | Messaging | MES-016 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 54 | Information | Type-safe KafkaConsumerManager initialized | Messaging | MES-017 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 104 | Debug | Consumer created: {EntityType} -> {TopicName} | Messaging | MES-018 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 109 | Error | Failed to create consumer: {EntityType} | Messaging | MES-019 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 192 | Error | Message handler failed: {EntityType} | Messaging | MES-020 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 198 | Information | Subscription cancelled: {EntityType} | Messaging | MES-021 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 202 | Error | Subscription error: {EntityType} | Messaging | MES-022 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 241 | Debug | Created SchemaRegistryClient with URL: {Url} | Messaging | MES-023 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 278 | Debug | Generated key schema: {Schema} | Messaging | MES-024 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 289 | Debug | Generated value schema: {Schema} | Messaging | MES-025 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 371 | Information | Disposing type-safe KafkaConsumerManager... | Messaging | MES-026 |
| src/Messaging/Consumers/KafkaConsumerManager.cs | 393 | Information | Type-safe KafkaConsumerManager disposed | Messaging | MES-027 |
| src/Messaging/Internal/ErrorHandlingContext.cs | 69 | Error | Failed in custom handler | Messaging | MES-028 |
| src/Messaging/Internal/ErrorHandlingContext.cs | 78 | Warning | Skipping item after error | Messaging | MES-029 |
| src/Messaging/Internal/ErrorHandlingContext.cs | 118 | Error | Skipping item due to unknown error action: {Action} | Messaging | MES-030 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 71 | Debug | Message sent: {EntityType} -> {Topic}, Partition: {Partition}, Offset: {Offset} | Messaging | MES-031 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 86 | Error | Failed to send message: {EntityType} -> {Topic} | Messaging | MES-032 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 116 | Debug | Tombstone sent: {EntityType} -> {Topic}, Partition: {Partition}, Offset: {Offset} | Messaging | MES-033 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 131 | Error | Failed to send tombstone: {EntityType} -> {Topic} | Messaging | MES-034 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 147 | Trace | Producer flushed: {EntityType} -> {Topic} | Messaging | MES-035 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 151 | Warning | Failed to flush producer: {EntityType} -> {Topic} | Messaging | MES-036 |
| src/Messaging/Producers/Core/KafkaProducer.cs | 189 | Warning | Error disposing producer: {EntityType} | Messaging | MES-037 |
| src/Messaging/Producers/KafkaProducerManager.cs | 51 | Information | Type-safe KafkaProducerManager initialized | Messaging | MES-038 |
| src/Messaging/Producers/KafkaProducerManager.cs | 96 | Debug | Producer created: {EntityType} -> {TopicName} | Messaging | MES-039 |
| src/Messaging/Producers/KafkaProducerManager.cs | 101 | Error | Failed to create producer: {EntityType} | Messaging | MES-040 |
| src/Messaging/Producers/KafkaProducerManager.cs | 261 | Debug | Created SchemaRegistryClient with URL: {Url} | Messaging | MES-041 |
| src/Messaging/Producers/KafkaProducerManager.cs | 296 | Debug | Generated key schema: {Schema} | Messaging | MES-042 |
| src/Messaging/Producers/KafkaProducerManager.cs | 307 | Debug | Generated value schema: {Schema} | Messaging | MES-043 |
| src/Messaging/Producers/KafkaProducerManager.cs | 391 | Information | Disposing type-safe KafkaProducerManager... | Messaging | MES-044 |
| src/Messaging/Producers/KafkaProducerManager.cs | 424 | Information | Type-safe KafkaProducerManager disposed | Messaging | MES-045 |
