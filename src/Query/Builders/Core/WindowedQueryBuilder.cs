using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Builders.Utils;
using Kafka.Ksql.Linq.Query.Pipeline;
using System.Text;

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
        if (role == Role.Live || role == Role.Final)
            sb.Append($"TABLE {input}");
        if (spec.Window)
            sb.Append(' ').Append(QueryBuilderUtils.ApplyWindowTumbling(tfStr));
        if (spec.Emit != null)
            sb.Append(' ').Append($"EMIT {spec.Emit}");
        if (spec.SyncHb1m)
        {
            var sync = md.GetProperty<string>($"sync/{tfStr}{roleName}");
            if (sync != null)
                sb.Append(' ').Append(QueryBuilderUtils.ApplySync_HB1m(sync));
        }
        if (role == Role.Final)
        {
            var prev = md.GetProperty<string>($"prev/{tfStr}{roleName}");
            if (prev != null)
                sb.Append(' ').Append(QueryBuilderUtils.ApplyPrev_1m(prev));
        }
        sb.Append(' ').Append(QueryBuilderUtils.ApplyTimeFrame(md));
        var sql = sb.ToString().Trim();
        if (role == Role.Final && sql.Contains("COMPOSE(", System.StringComparison.OrdinalIgnoreCase))
            throw new System.InvalidOperationException("Final SQL should not include COMPOSE()");
        return sql;
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
