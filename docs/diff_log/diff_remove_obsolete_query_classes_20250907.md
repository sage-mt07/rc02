# diff_remove_obsolete_query_classes_20250907
- Confirmed deletion of legacy query helpers:
  - `src/Query/Adapters/EntityModelRegistrar.cs`
  - `src/Query/Adapters/TopicNameResolver.cs`
  - `src/Query/Analysis/TumblingAnalyzer.cs`
- Removed remaining project and test references to these classes.
