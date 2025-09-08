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
        return role switch
        {
            Role.Live => new(true, "CHANGES", false, false, true),
            Role.AggFinal => new(true, "FINAL GRACE", true, false, false),
            Role.Final => new(true, "FINAL", true, false, true),
            _ => new(false, null, false, false, false)
        };
    }
}
