# Remove AsTable Fluent API

- Dropped `AsTable` method; table registration now uses `[KsqlTable]` attribute.
- Eliminated `IEntityBuilder` interface; `IModelBuilder.Entity` returns `EntityModelBuilder`.
- Updated docs, tests, and samples to reflect attribute-based table registration.
