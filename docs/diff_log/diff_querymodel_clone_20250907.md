# Query model clone

- DerivedTumblingPipeline now clones `KsqlQueryModel` per role
- BuildDdlAndRegister expects preconfigured model without mutating flags
- `KsqlQueryModel.Clone()` added for deep copy
