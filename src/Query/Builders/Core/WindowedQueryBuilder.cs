using System.Text;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Builders.Utils;
using Kafka.Ksql.Linq.Query.Pipeline;

namespace Kafka.Ksql.Linq.Query.Builders.Core;

internal static class WindowedQueryBuilder
{
    public static string Build(Role role, string timeframe, QueryMetadata md)
    {
        var tf = Parse(timeframe);
        var tfStr = timeframe;
        var spec = RoleTraits.For(role, tf);
        var roleName = role switch { Role.Live => "Live", Role.Final => "Final", _ => string.Empty };
        var input = role switch
        {
            Role.Live => QueryBuilderUtils.ResolveInput(md.GetProperty<string>($"input/{tfStr}Live")),
            Role.Final => QueryBuilderUtils.ResolveInput(md.GetProperty<string>($"input/{tfStr}Final")),
            _ => string.Empty
        };
        var sb = new StringBuilder();
        if (role == Role.Live)
            sb.Append($"TABLE {input}");
        else if (role == Role.Final)
            sb.Append(QueryBuilderUtils.ApplyCompose_FinalNonNull(input));
        if (spec.Window)
            sb.Append(' ').Append(QueryBuilderUtils.ApplyWindowTumbling(tfStr));
        if (spec.Emit != null)
            sb.Append(' ').Append($"EMIT {spec.Emit}");
        if (spec.Projector)
            sb.Append(' ').Append(QueryBuilderUtils.ApplyProjector_BucketStartFromWindowStart());
        if (spec.SyncHb1m)
        {
            var sync = md.GetProperty<string>($"sync/{tfStr}{roleName}");
            if (sync != null)
                sb.Append(' ').Append(QueryBuilderUtils.ApplySync_HB1m(sync));
        }
        sb.Append(' ').Append(QueryBuilderUtils.ApplyTimeFrame(md));
        return sb.ToString().Trim();
    }

    private static Timeframe Parse(string tf)
    {
        if (tf.EndsWith("mo"))
            return new Timeframe(int.Parse(tf[..^2]), "mo");
        if (tf.EndsWith("wk"))
            return new Timeframe(int.Parse(tf[..^2]), "wk");
        return new Timeframe(int.Parse(tf[..^1]), tf[^1].ToString());
    }
}
