using System;
using System.Linq;
using System.Text;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Builders.Utils;

internal static class QueryBuilderUtils
{
    public static string ResolveInput(string? hint) => hint ?? string.Empty;

    public static string ApplyTimeFrame(QueryMetadata md)
    {
        var joinKeys = md.GetProperty<string[]>("basedOn/joinKeys") ?? Array.Empty<string>();
        var openProp = md.GetProperty<string>("basedOn/openProp");
        var closeProp = md.GetProperty<string>("basedOn/closeProp");
        var timeKey = md.GetProperty<string>("timeKey");
        var join = string.Join(" AND ", joinKeys.Select(k => $"r.{k} = s.{k}"));
        return $"JOIN ON {join} AND s.{openProp} <= r.{timeKey} AND r.{timeKey} < s.{closeProp}";
    }

    public static string ApplyWindowTumbling(string timeframe) => $"WINDOW TUMBLING({timeframe})";

    public static string ApplyProjector_BucketStartFromWindowStart() => "SELECT WINDOWSTART AS BucketStart";

    public static string ApplyCompose_FinalNonNull(string input) => $"COMPOSE({input})";

    public static string ApplySync_HB1m(string sync) => $"SYNC {sync}";
}
