using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Builders;
using Kafka.Ksql.Linq.Query.Builders.Core;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Query.Analysis;

internal static class DerivedTumblingPipeline
{
    public static async Task RunAsync(
        TumblingQao qao,
        EntityModel baseModel,
        KsqlQueryModel queryModel,
        Func<string, Task> execute,
        Func<string, Type> resolveType,
        MappingRegistry mapping,
        ConcurrentDictionary<Type, EntityModel> registry,
        ILogger logger)
    {
        var baseAttr = baseModel.EntityType.GetCustomAttribute<KsqlTopicAttribute>();
        var baseName = (baseAttr?.Name ?? baseModel.TopicName ?? baseModel.EntityType.Name).ToLowerInvariant();
        var entities = PlanDerivedEntities(qao, baseModel, queryModel.WhenEmptyFiller != null);
        var models = AdaptModels(entities);
        foreach (var m in models)
        {
            var role = Enum.Parse<Role>((string)m.AdditionalSettings["role"]);
            var tf = (string)m.AdditionalSettings["timeframe"];
            var allow = role switch
            {
                Role.Final1sStream or Role.Final1s => tf == "1s",
                Role.Prev1m => tf == "1m",
                Role.Live => true,
                Role.Hb => true,
                Role.Fill => true,
                _ => true
            };
            if (!allow)
                continue;
            var (ddl, dt, ns) = BuildDdlAndRegister(baseName, queryModel, m, role, resolveType);
            logger.LogInformation("KSQL DDL (derived {Entity}): {Sql}", m.TopicName, ddl);
            await execute(ddl);
            mapping.RegisterEntityModel(m, genericValue: true, overrideNamespace: ns);
            registry[dt] = m;
        }
    }

    public static IReadOnlyList<DerivedEntity> PlanDerivedEntities(TumblingQao qao, EntityModel model, bool whenEmpty)
        => DerivationPlanner.Plan(qao, model, whenEmpty);

    public static IReadOnlyList<EntityModel> AdaptModels(IReadOnlyList<DerivedEntity> entities)
        => EntityModelAdapter.Adapt(entities);

    private static (string ddl, Type entityType, string? ns) BuildDdlAndRegister(
        string baseName,
        KsqlQueryModel queryModel,
        EntityModel model,
        Role role,
        Func<string, Type> resolveType)
    {
        var qm = queryModel.Clone();
        string? inputOverride = null;
        // Respect explicit input hints when provided (e.g., hub stream/table linkage)
        if (model.AdditionalSettings.TryGetValue("input", out var inputObj))
        {
            inputOverride = inputObj?.ToString();
        }
        if (role == Role.Prev1m || role == Role.Final1sStream)
        {
            qm.Windows.Clear();
            qm.GroupByExpression = null;
            var inputType = resolveType(inputOverride ?? baseName);
            qm.SelectProjection = BuildInputProjection(inputType);
        }
        var tf = (string)model.AdditionalSettings["timeframe"];
        // Infer bucket column name when not present on the query model (e.g., tests building models directly)
        static string? InferBucketColumnName(KsqlQueryModel m, EntityModel em)
        {
            if (!string.IsNullOrWhiteSpace(m.BucketColumnName)) return m.BucketColumnName;
            static string? Pick(IEnumerable<string> cols)
            {
                var exact = cols.FirstOrDefault(x => string.Equals(x, "BucketStart", StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(exact)) return exact;
                return cols.FirstOrDefault(x => !string.IsNullOrEmpty(x) && x.IndexOf("bucket", StringComparison.OrdinalIgnoreCase) >= 0);
            }
            if (em.AdditionalSettings.TryGetValue("projection", out var pObj) && pObj is string[] projection)
            {
                var c = Pick(projection);
                if (!string.IsNullOrWhiteSpace(c)) return c;
            }
            if (em.AdditionalSettings.TryGetValue("keys", out var kObj) && kObj is string[] keys)
            {
                var c = Pick(keys);
                if (!string.IsNullOrWhiteSpace(c)) return c;
            }
            return null;
        }
        var inferredBucket = InferBucketColumnName(qm, model);
        var spec = RoleTraits.For(role);
        var emit = spec.Emit != null ? $"EMIT {spec.Emit}" : null;
        var name = role switch
        {
            Role.Live => $"{baseName}_{tf}_live",
            Role.Final => $"{baseName}_{tf}_final",
            Role.Final1s => $"{baseName}_{tf}_final",
            Role.Final1sStream => $"{baseName}_{tf}_final_s",
            Role.Prev1m => $"{baseName}_prev_1m",
            Role.Hb => $"{baseName}_hb_{tf}",
            Role.Fill => $"{baseName}_{tf}_fill",
            _ => $"{baseName}_{tf}"
        };
        string ddl;
        if (role == Role.Fill)
        {
            // Experimental: Build Fill DDL by driving from HB and left-joining live.
            // Note: prev_1m join and filler specifics will be added in a later pass.
            var keys = model.AdditionalSettings.TryGetValue("keys", out var kObj) ? (string[])kObj! : Array.Empty<string>();
            var projection = model.AdditionalSettings.TryGetValue("projection", out var pObj) ? (string[])pObj! : Array.Empty<string>();
            var bucketCol = inferredBucket ?? throw new InvalidOperationException("WhenEmpty/Fill requires WindowStart() in Select to define the bucket column.");
            // Heartbeat は各足ごと（WhenEmpty時）に生成される前提
            var hbName = $"{baseName}_hb_{tf}";
            var liveName = $"{baseName}_{tf}_live";
            // Optionally include prev_1m when timeframe is 1m to enable previous-close fill
            string? prevName = tf.Equals("1m", StringComparison.OrdinalIgnoreCase) ? $"{baseName}_prev_1m" : null;
            ddl = KsqlFillStatementBuilder.Build(name, keys, projection, bucketCol, hbName, liveName, prevName);
            if (!string.IsNullOrWhiteSpace(emit) && !ddl.Contains("EMIT ", StringComparison.OrdinalIgnoreCase))
                ddl = ddl.Replace(";", $" {emit};");
        }
        else if (role == Role.Prev1m)
        {
            var keys = model.AdditionalSettings.TryGetValue("keys", out var kObj2) ? (string[])kObj2! : Array.Empty<string>();
            var projection2 = model.AdditionalSettings.TryGetValue("projection", out var pObj2) ? (string[])pObj2! : Array.Empty<string>();
            var bucketCol2 = inferredBucket ?? throw new InvalidOperationException("Prev requires WindowStart() in Select to define the bucket column.");
            // 1m の前回値結合は同じ足の HB を使用
            var hbName2 = $"{baseName}_hb_{tf}"; // tf は 1m
            var liveName2 = $"{baseName}_{tf}_live";
            ddl = KsqlPrevStatementBuilder.Build(name, keys, projection2, bucketCol2, hbName2, liveName2, 1);
            if (!string.IsNullOrWhiteSpace(emit) && !ddl.Contains("EMIT ", StringComparison.OrdinalIgnoreCase))
                ddl = ddl.Replace(";", $" {emit};");
        }
        else if (role == Role.Final1sStream)
        {
            // ksqlDB limitation: persistent queries cannot source from windowed TABLEs.
            // Define a logical STREAM over the 1s TABLE's Kafka topic with explicit columns so keys are resolvable.
            var table1s = $"{baseName}_1s_final";
            // Build columns from AdditionalSettings (shapes captured earlier)
            var keyNames = model.AdditionalSettings.TryGetValue("keys", out var kObj) && kObj is string[] ks ? ks : Array.Empty<string>();
            var keyTypes = model.AdditionalSettings.TryGetValue("keys/types", out var ktObj) && ktObj is Type[] kts ? kts : Array.Empty<Type>();
            var valNames = model.AdditionalSettings.TryGetValue("projection", out var pObj) && pObj is string[] vs ? vs : Array.Empty<string>();
            var valTypes = model.AdditionalSettings.TryGetValue("projection/types", out var vtObj) && vtObj is Type[] vts ? vts : Array.Empty<Type>();
            static string Map(Type t) => Query.Schema.KsqlTypeMapping.MapToKsqlType(t, null);
            var cols = new System.Collections.Generic.List<string>();
            for (int i = 0; i < keyNames.Length && i < keyTypes.Length; i++)
                cols.Add($"{keyNames[i].ToUpperInvariant()} {Map(keyTypes[i])} KEY");
            for (int i = 0; i < valNames.Length && i < valTypes.Length; i++)
            {
                var n = valNames[i].ToUpperInvariant();
                if (keyNames.Any(k => string.Equals(k, valNames[i], StringComparison.OrdinalIgnoreCase)))
                    continue;
                cols.Add($"{n} {Map(valTypes[i])}");
            }
            var colList = string.Join(", ", cols);
            ddl = $"CREATE STREAM IF NOT EXISTS {name} ({colList}) WITH (KAFKA_TOPIC='{table1s}', KEY_FORMAT='AVRO', VALUE_FORMAT='AVRO');";
        }
        else
        {
            // Generate windowed CTAS/CSAS. Keep key path style default for streams (alias-qualified o.KEYCOLS)
            ddl = KsqlCreateWindowedStatementBuilder.Build(name, qm, tf, emit, inputOverride, options: null);
            // If sourcing from the 1s hub stream, remap aggregate arguments from BID to per-second OHLC columns
            if (!string.IsNullOrWhiteSpace(inputOverride) && inputOverride!.EndsWith("_1s_final_s", StringComparison.OrdinalIgnoreCase))
            {
                // Normalize spacing for easier replacement
                string ReplaceAggArg(string sql, string pattern, string replacement)
                {
                    return System.Text.RegularExpressions.Regex.Replace(
                        sql,
                        pattern,
                        replacement,
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase | System.Text.RegularExpressions.RegexOptions.CultureInvariant);
                }
                // Map aggregates: open=earliest(Open), high=max(High), low=min(Low), close=latest(KsqlTimeFrameClose)
                ddl = ReplaceAggArg(ddl, @"EARLIEST_BY_OFFSET\(\s*(?:O\.)?BID\s*\)", "EARLIEST_BY_OFFSET(O.OPEN)");
                ddl = ReplaceAggArg(ddl, @"MAX\(\s*(?:O\.)?BID\s*\)", "MAX(O.HIGH)");
                ddl = ReplaceAggArg(ddl, @"MIN\(\s*(?:O\.)?BID\s*\)", "MIN(O.LOW)");
                ddl = ReplaceAggArg(ddl, @"LATEST_BY_OFFSET\(\s*(?:O\.)?BID\s*\)", "LATEST_BY_OFFSET(O.KSQLTIMEFRAMECLOSE)");
            }
            // Inject GRACE PERIOD for Live windows to tolerate startup/lateness
            if (role == Role.Live)
            {
                var grace = 0;
                if (model.AdditionalSettings.TryGetValue("graceSeconds", out var gObj) && gObj is int g)
                    grace = g;
                if (grace < 600) grace = 600; // default 10 minutes for tests
                if (grace > 0 && ddl.IndexOf("GRACE", StringComparison.OrdinalIgnoreCase) < 0)
                {
                    ddl = System.Text.RegularExpressions.Regex.Replace(
                        ddl,
                        @"(WINDOW\s+TUMBLING\s*\(\s*SIZE\s+[^\)]+)\)",
                        $"$1, GRACE PERIOD {grace} SECONDS)",
                        System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                }
            }
        }
        var dt = resolveType(name);
        model.EntityType = dt;
        model.TopicName = name;
        model.SetStreamTableType(qm.DetermineType());
        var ns = model.AdditionalSettings.TryGetValue("namespace", out var nsObj) ? nsObj?.ToString() : null;
        return (ddl, dt, ns);
    }

    private static LambdaExpression BuildInputProjection(Type inputType)
    {
        // App-agnostic: select all columns (identity -> SELECT *)
        var p = Expression.Parameter(inputType, "x");
        return Expression.Lambda(p, p);
    }
}
