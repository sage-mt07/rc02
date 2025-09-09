# diff: derived tumbling pipeline source projection override (2025-09-09)

- `Role.Final` and `Role.Prev1m` clear `qm.Windows`
- `FROM` source is replaced with `AdditionalSettings["input"]`
- projection uses `Open`, `High`, `Low`, `KsqlTimeFrameClose` from the input table
