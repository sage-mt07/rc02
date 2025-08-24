using System;
using System.Collections.Generic;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal enum Role
{
    Live,
    AggFinal,
    Final,
    Prev1m,
    Hb
}

internal enum MaterializationHint
{
    Table,
    Stream
}

internal record ColumnShape(string Name, Type Type, bool IsNullable);

internal class DerivedEntity
{
    public string Id { get; init; } = string.Empty;
    public Role Role { get; init; }
    public Timeframe Timeframe { get; init; }
    public IReadOnlyList<ColumnShape> KeyShape { get; init; } = Array.Empty<ColumnShape>();
    public IReadOnlyList<ColumnShape> ValueShape { get; init; } = Array.Empty<ColumnShape>();
    public MaterializationHint MaterializationHint { get; init; } = MaterializationHint.Table;
    public string? TopicHint { get; init; }
    public string? InputHint { get; init; }
    public string? SyncHint { get; init; }
    public BasedOnSpec BasedOnSpec { get; init; } = new(new List<string>(), string.Empty, string.Empty, string.Empty);
}
