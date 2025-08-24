# Topic Fluent API Extension Samples

This directory contains runnable examples showing how to configure Kafka topics
using POCO attributes. Four sample levels are provided:

- **Beginner** – declare topic settings via `[KsqlTopic]` (`Example1_Basic`).
- **Intermediate** – use `[KsqlTopic]` with the custom `IsManaged` flag (`Example2_ManagedMode`).
- **Advanced** – demonstrate custom retention and cleanup policy extensions (`Example3_AdvancedOptions`).
- **Decimal Precision** – specify `[KsqlDecimal]` for precise fields (`Example4_DecimalPrecision`).

## Running the Samples

1. Ensure the root project has been restored.
2. From this directory run:

```bash
dotnet test
```

This builds the test project and executes a simple unit test verifying the
`IsManaged` extension. During the test `Example2_ManagedMode.Run()` is executed
which prints the configured managed flag to the console.
