using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Builders.Core;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Builders;

internal static class AggFinalBuilder
{
    public static string Build(QueryMetadata md, string timeframe)
        => WindowedQueryBuilder.Build(Role.AggFinal, timeframe, md);
}
