using Kafka.Ksql.Linq.Query.Analysis;

namespace Kafka.Ksql.Linq.Query.Builders.Core;

internal static class RoleTraits
{
    public static OperationSpec For(Role role, Timeframe tf)
    {
        var is1m = tf.Unit == "m" && tf.Value == 1;
        return role switch
        {
            Role.Live => new(true, "CHANGES", false, false, is1m),
            Role.AggFinal => new(true, "FINAL GRACE", true, false, false),
            Role.Final => new(false, null, false, true, is1m),
            _ => new(false, null, false, false, false)
        };
    }
}
