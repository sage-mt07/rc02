# TimeKey extraction cleanup

- Removed redundant `ExtractPropertyName` helper from `KsqlQueryable` and `KsqlQueryable2`.
- Time key and schedule property names are parsed via `MethodCallCollectorVisitor`.
- Added tests for property name parsing in `Tumbling` and `TimeFrame`.
