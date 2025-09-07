# Derived tumbling pipeline query model clone in BuildDdlAndRegister

- `BuildDdlAndRegister` now clones `KsqlQueryModel` internally per role
- `RunAsync` passes the shared model without mutating flags
- Test ensures original `KsqlQueryModel` remains unchanged
