using System.Collections.Generic;
using Kafka.Ksql.Linq.Query.Analysis;

namespace Kafka.Ksql.Linq.Query.Adapters;

internal class QuerySpec
{
    public string TargetId { get; init; } = string.Empty;
    public IReadOnlyList<string> Sources { get; init; } = new List<string>();
    public string Operation { get; init; } = string.Empty;
    public string? Projector { get; init; }
    public string? Sync { get; init; }
    public BasedOnSpec BasedOnRef { get; init; } = new(new List<string>(), string.Empty, string.Empty, string.Empty);
    public string[] ColumnPlan { get; set; } = System.Array.Empty<string>();
}
