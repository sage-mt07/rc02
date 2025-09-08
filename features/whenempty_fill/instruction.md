# WhenEmpty filler
- Add WhenEmpty(Func<T,T,T>) to KsqlQueryable storing lambda in KsqlQueryModel.
- MethodCallCollectorVisitor detects WhenEmpty.
- DerivationPlanner adds HB+LEFT JOIN+Fill derived entity only when flagged.
