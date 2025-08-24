namespace Kafka.Ksql.Linq.Query.Builders.Core;

internal readonly record struct OperationSpec(
    bool Window,
    string? Emit,
    bool Projector,
    bool Compose,
    bool SyncHb1m);
