# diff: SelectExpressionVisitor MemberInit alias (2025-09-06)

- Handle MemberInit expressions in SelectExpressionVisitor to apply property aliases.
- Prevents reserved column name `WINDOWSTART` from appearing without alias in generated KSQL.
