# Remove topic Fluent APIs

- Dropped `AsStream`, `WithReplicationFactor`, and `WithPartitioner` methods from `EntityModelBuilder`.
- Stream registration is now implicit; use `AsTable` only when a table is required.
- Updated examples and API reference to reflect the streamlined model.
