- Replaced `Parallel.ForEachAsync` with a sequential `foreach` in `DerivedTumblingPipeline.RunAsync` to send DDL commands in order.
- Maintained existing registration sequence to keep mapping and registry updates consistent.

