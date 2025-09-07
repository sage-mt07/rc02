using Kafka.Ksql.Linq.Query.Analysis;

namespace Kafka.Ksql.Linq.Query.Builders.Core;

/// <summary>
/// Describes window, emit and sync behavior for each query role.
/// Final roles never compose intermediate sources; they operate on physical or view tables
/// and require windowing with <c>EMIT FINAL</c>.
/// </summary>
internal static class RoleTraits
{
    public static OperationSpec For(Role role, Timeframe tf)
    {
        var is1m = tf.Unit == "m" && tf.Value == 1;
        return role switch
        {
            Role.Live => new(true, "CHANGES", false, false, is1m),
            Role.AggFinal => new(true, "FINAL GRACE", true, false, false),
            Role.Final => new(true, "FINAL", true, false, is1m),
            _ => new(false, null, false, false, false)
        };
    }
}
