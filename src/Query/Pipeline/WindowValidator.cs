using System;

namespace Kafka.Ksql.Linq.Query.Pipeline;

internal static class WindowValidator
{
    public static void Validate(ExpressionAnalysisResult result)
    {
        if (result == null) throw new ArgumentNullException(nameof(result));
        if (!result.BaseUnitSeconds.HasValue || result.Windows.Count == 0)
            return;
        var baseUnit = result.BaseUnitSeconds.Value;
        foreach (var w in result.Windows)
        {
            if (ToSeconds(w) % baseUnit != 0)
                throw new InvalidOperationException($"Window {w} must be a multiple of base {baseUnit}s.");
        }
    }

    private static int ToSeconds(string w)
    {
        if (w.EndsWith("mo", StringComparison.OrdinalIgnoreCase))
            return int.Parse(w[..^2]) * 30 * 24 * 3600;
        if (w.EndsWith("wk", StringComparison.OrdinalIgnoreCase))
            return int.Parse(w[..^2]) * 7 * 24 * 3600;
        var unit = w[^1];
        var value = int.Parse(w[..^1]);
        return unit switch
        {
            's' => value,
            'm' => value * 60,
            'h' => value * 3600,
            'd' => value * 86400,
            _ => value
        };
    }
}
