# Logging ID Scheme

Log messages are cataloged with a **category** and a sequential **LogID**. The category is inferred from the file path.

## Categories

- **Importer** – any file under `Kafka.Ksql.Linq.Importer/`
- **Cache** – files in `src/Cache/`
- **Core** – files in `src/Core/`
- **Infrastructure** – files in `src/Infrastructure/`
- **Messaging** – files in `src/Messaging/`
- **Window** – files in `src/Window/`
- **KsqlContext** – messages in `src/KsqlContext.cs`
- **Tests** – anything under `physicalTests/`
- Other files fall under **Misc**.

## LogID Format

Each category receives a unique three‑letter abbreviation. Messages are numbered sequentially within their category using this abbreviation followed by a zero‑padded number, for example `IMP-001`.

```
<Category Abbreviation>-<Sequential Number>
```

The abbreviation is formed from the first three alphanumeric characters of the category name.
