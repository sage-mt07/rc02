# diff: sanitized namespace generation

- EntityModelAdapter strips timeframe and role from derived IDs and appends `_ksql`.
- MappingRegistry.RegisterEntityModel honored overrideNamespace for derived models.
- Added test ensuring derived namespace `bar_5m_live` registers as `bar_ksql`.

