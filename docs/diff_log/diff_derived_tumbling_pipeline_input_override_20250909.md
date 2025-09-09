# Diff: Derived tumbling pipeline input override

- `BuildDdlAndRegister` clears windows for final and prev1m roles before DDL generation.
- Source topic now defaults to base name and honours `AdditionalSettings["input"]` via resolver.
