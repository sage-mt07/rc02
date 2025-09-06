# GroupBy before Select build
- Ensure `GroupByClauseBuilder` runs before `SelectClauseBuilder` in `KsqlCreateStatementBuilder.Build`.
- Enables `SELECT` clause to include grouping keys like `Broker, Symbol, WINDOWSTART`.
