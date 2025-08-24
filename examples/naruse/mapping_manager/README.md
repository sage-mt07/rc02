# MappingManager AddAsync Sample

This sample demonstrates the standardized `AddAsync` flow using `MappingManager`.
A simple `Order` entity is registered via `AddSampleModels`, converted to
key/value pairs, and sent through `KsqlContext.AddAsync`.

Run with:
```bash
dotnet run --project .
```
