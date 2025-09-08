using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Kafka.Ksql.Linq.Core.Attributes;

namespace Kafka.Ksql.Linq.Query.Pipeline;

/// <summary>
/// 式解析結果（簡略版）
/// </summary>
internal class ExpressionAnalysisResult
{
    public List<MethodCallExpression> MethodCalls { get; set; } = new();
    public bool HasGroupBy => MethodCalls.Any(mc => mc.Method.Name == "GroupBy");
    public bool HasAggregation => MethodCalls.Any(mc => IsAggregateMethod(mc.Method.Name));

    public List<string> Windows { get; } = new();
    public string? TimeKey { get; set; }
    public List<string> GroupByKeys { get; } = new();
    public DayOfWeek WeekAnchor { get; set; } = DayOfWeek.Monday;
    public int? BaseUnitSeconds { get; set; }

    public List<string> BasedOnJoinKeys { get; } = new();
    public string? BasedOnOpen { get; set; }
    public string? BasedOnClose { get; set; }
    public bool BasedOnOpenInclusive { get; set; } = true;
    public bool BasedOnCloseInclusive { get; set; } = false;
    public string? BasedOnDayKey { get; set; }
    public Type? PocoType { get; set; }
    public bool WhenEmpty { get; set; }
    public int WindowStartCallCount { get; set; }
    public string? BucketColumnName { get; set; }

    private static bool IsAggregateMethod(string methodName)
    {
        return methodName is "Sum" or "Count" or "Max" or "Min" or "Average" or "Aggregate";
    }

    public QueryMetadata ToMetadata()
    {
        var md = new QueryMetadata(DateTime.UtcNow, "Query");
        md = md.WithProperty("windows", Windows.ToArray());
        md = md.WithProperty("timeKey", TimeKey!);
        md = md.WithProperty("basedOn/joinKeys", BasedOnJoinKeys.ToArray());
        md = md.WithProperty("basedOn/openProp", BasedOnOpen!);
        md = md.WithProperty("basedOn/closeProp", BasedOnClose!);
        md = md.WithProperty("basedOn/dayKey", BasedOnDayKey!);
        md = md.WithProperty("basedOn/openInclusive", BasedOnOpenInclusive);
        md = md.WithProperty("basedOn/closeInclusive", BasedOnCloseInclusive);

        if (BucketColumnName != null)
            md = md.WithProperty("bucketColumn", BucketColumnName);

        md = md.WithProperty("roles/live", Windows.ToArray());
        md = md.WithProperty("roles/aggFinal", Windows.ToArray());
        md = md.WithProperty("roles/final", Windows.ToArray());
        md = md.WithProperty("roles/prev", new[] { "1m" });
        md = md.WithProperty("roles/hb", new[] { "1m" });

        if (PocoType != null)
        {
            var topicAttr = PocoType.GetCustomAttribute<KsqlTopicAttribute>();
            var baseId = (topicAttr?.Name ?? PocoType.Name).ToLowerInvariant();
            var hb = $"{baseId}_hb_1m".ToUpperInvariant();
            var prevId = $"{baseId}_prev_1m";
            md = md.WithProperty("sync/1mLive", hb);
            md = md.WithProperty("sync/1mFinal", hb);
            md = md.WithProperty("prev/1mFinal", prevId);
            foreach (var tf in Windows)
            {
                var liveInput = tf switch
                {
                    "1m" => "10sAgg",
                    "1wk" => $"{baseId}_1m_final",
                    _ => $"{baseId}_1m_live"
                };
                md = md.WithProperty($"input/{tf}Live", liveInput);
                md = md.WithProperty($"input/{tf}Final", liveInput);
                md = md.WithProperty($"prev/{tf}Final", prevId);
            }
        }
        return md;
    }
}
