using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Kafka.Ksql.Linq.Query.Analysis;
using Kafka.Ksql.Linq.Query.Adapters;
using Kafka.Ksql.Linq.Query.Dsl;
using Kafka.Ksql.Linq.Query.Pipeline;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Abstractions;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Analysis;

public class Step1Tests
{
    public class Rate
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime BucketStart { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
    }

    public class MarketSchedule
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime MarketDate { get; set; }
    }

    private static Expression BuildExpression() =>
        ((Expression<Func<KsqlQueryable<Rate>, object>>)(q => q
            .TimeFrame<MarketSchedule>(
                (r, s) =>
                    r.Broker == s.Broker &&
                    r.Symbol == s.Symbol &&
                    s.Open <= r.Timestamp &&
                    r.Timestamp < s.Close,
                s => s.MarketDate)
            .Tumbling(r => r.Timestamp, new[] { 1, 5 }, null, null, null, null, null)
            .GroupBy(r => new { r.Broker, r.Symbol, BucketStart = r.Timestamp })
            .Select(g => new
            {
                g.Key.Broker,
                g.Key.Symbol,
                g.Key.BucketStart,
                Open = g.Max(x => x.Open)
            }))).Body;

    [Fact]
    public void Analyzer_Extracts_TimeKey_Windows_TimeFrame_InclusiveFlags()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        Assert.Equal("Timestamp", qao.TimeKey);
        Assert.Equal(new[] { "1m", "5m" }, qao.Windows.Select(w => $"{w.Value}{w.Unit}").ToArray());
        Assert.True(qao.BasedOn.IsOpenInclusive);
        Assert.False(qao.BasedOn.IsCloseInclusive);
    }

    [Fact]
    public void Planner_Generates_Roles_Per_TF_With_HB1m_Only()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        Assert.Single(entities.Where(e => e.Role == Role.Hb));
        foreach (var tf in new[] { "1m", "5m" })
        {
            Assert.Contains(entities, e => e.Role == Role.Live && e.Timeframe.Value + e.Timeframe.Unit == tf);
            Assert.Contains(entities, e => e.Role == Role.AggFinal && e.Timeframe.Value + e.Timeframe.Unit == tf);
            Assert.Contains(entities, e => e.Role == Role.Final && e.Timeframe.Value + e.Timeframe.Unit == tf);
        }
    }

    [Fact]
    public void Adapter_Entity_Preserves_KeyOrder_And_ProjectionOrder()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        var models = EntityModelAdapter.Adapt(entities);
        var model = models.First(m => (string)m.AdditionalSettings["role"] == "Live");
        Assert.Equal(new[] { "Broker", "Symbol", "Timestamp", "BucketStart", "Open", "High", "Low", "Close" }, (string[])model.AdditionalSettings["projection"]);
        Assert.Equal(new[] { "Broker", "Symbol", "BucketStart" }, (string[])model.AdditionalSettings["keys"]);
    }

    [Fact]
    public void Adapter_QuerySpec_Builds_Operations_And_TimeFrameRef()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var specs = QueryAdapter.Build(entities, dag);
        var agg = specs.First(s => s.TargetId.StartsWith("bar_1m_agg_final"));
        Assert.Contains("FINAL+GRACE", agg.Operation);
        Assert.Equal("BucketStartFromWindowStart", agg.Projector);
        var live = specs.First(s => s.TargetId.StartsWith("bar_1m_live"));
        Assert.Contains("CHANGES", live.Operation);
        var final = specs.First(s => s.TargetId.StartsWith("bar_1m_final"));
        Assert.Contains("Compose", final.Operation);
        Assert.NotEmpty(agg.BasedOnRef.JoinKeys);
    }

    [Fact]
    public void QuerySpec_TargetId_NotEmpty_And_Dag_Toposortable()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var specs = QueryAdapter.Build(entities, dag);
        Assert.All(specs, s => Assert.False(string.IsNullOrWhiteSpace(s.TargetId)));
        var order = dag.TopologicalSort();
        Assert.Equal(dag.Edges.Count, order.Count());
    }

    [Fact]
    public void HB_Has_Keys_And_TimeFrame()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        var hb = entities.First(e => e.Role == Role.Hb);
        Assert.NotEmpty(hb.KeyShape);
        Assert.Empty(hb.ValueShape);
        Assert.Equal(MaterializationHint.Stream, hb.MaterializationHint);
        Assert.NotEmpty(hb.BasedOnSpec.JoinKeys);
    }
    
    [Fact]
    public void Planner_Clones_ValueShape_FromPoco()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        var live = entities.First(e => e.Role == Role.Live && e.Timeframe.Value == 1);
        var pocoOrder = typeof(Rate).GetProperties().OrderBy(p => p.MetadataToken).Select(p => p.Name).ToArray();
        Assert.Equal(pocoOrder, live.ValueShape.Select(v => v.Name).ToArray());
    }

    [Fact]
    public void Planner_Builds_1wk_From_1m_Final()
    {
        var qao = new TumblingQao
        {
            TimeKey = "Timestamp",
            Windows = new List<Timeframe> { new(1, "m"), new(1, "wk") },
            Keys = new[] { "Broker", "Symbol", "BucketStart" },
            Projection = new[] { "Broker", "Symbol", "BucketStart" },
            PocoShape = new[]
            {
                new ColumnShape("Broker", typeof(string), false),
                new ColumnShape("Symbol", typeof(string), false),
                new ColumnShape("Timestamp", typeof(DateTime), false),
                new ColumnShape("BucketStart", typeof(DateTime), false)
            },
            BasedOn = new BasedOnSpec(new[] { "Broker" }, "Open", "Close", "MarketDate")
        };
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var liveWeek = entities.First(e => e.Id == "bar_1wk_live");
        Assert.Equal("bar_1m_final", liveWeek.InputHint);
        Assert.Contains("bar_1m_final", dag.Edges[liveWeek.Id]);
    }

    [Fact]
    public void Adapter_Defaults_Table_When_HasPK_And_Respects_ForceStream_ForHB()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        var models = EntityModelAdapter.Adapt(entities);
        var hb = models.First(m => (string)m.AdditionalSettings["role"] == "Hb");
        Assert.True((bool)hb.AdditionalSettings["forceStream"]);
        var live = models.First(m => (string)m.AdditionalSettings["role"] == "Live");
        Assert.False(live.AdditionalSettings.ContainsKey("forceStream"));
    }

    [Fact]
    public void Live_IsTable_NoDepOn_AggFinal_And_1mSyncsOnHB()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var live = entities.First(e => e.Role == Role.Live && e.Timeframe.Value == 1);
        Assert.Equal(MaterializationHint.Table, live.MaterializationHint);
        Assert.Equal("HB_1m", live.SyncHint);
        Assert.DoesNotContain(dag.Edges[live.Id], id => id.Contains("_agg_final"));
    }

    [Fact]
    public void Registrar_Defaults_Table_When_HasPK_And_Respects_ForceStream()
    {
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, _) = DerivationPlanner.Plan(qao);
        var models = EntityModelAdapter.Adapt(entities);
        var registry = new MappingRegistry();
        EntityModelRegistrar.Register(registry, models);
        var hb = models.First(m => (string)m.AdditionalSettings["role"] == "Hb");
        var live = models.First(m => (string)m.AdditionalSettings["role"] == "Live");
        Assert.Equal(StreamTableType.Stream, hb.GetExplicitStreamTableType());
        Assert.Equal(StreamTableType.Table, live.GetExplicitStreamTableType());
    }

    [Fact]
    public void Apply_SchemaHash_Validates_Once_PerPoco_Skips_HB()
    {
        SchemaValidator.Reset();
        var qao = TumblingAnalyzer.Analyze(BuildExpression(), typeof(Rate));
        var (entities, dag) = DerivationPlanner.Plan(qao);
        var models = EntityModelAdapter.Adapt(entities);
        EntityModelRegistrar.Register(new MappingRegistry(), models);
        var specs = QueryAdapter.Build(entities, dag).ToList();

        var live = specs.First(s => s.TargetId.StartsWith("bar_1m_live"));
        var badLive = new List<QuerySpec>
        {
            new() { TargetId = live.TargetId, Sources = live.Sources, Operation = live.Operation, Projector = live.Projector, Sync = live.Sync, BasedOnRef = live.BasedOnRef, ColumnPlan = new[] { "Broker" } }
        };
        Assert.Throws<InvalidOperationException>(() => SchemaValidator.Validate(badLive, models));

        SchemaValidator.Reset();
        var hb = specs.First(s => s.TargetId.StartsWith("hb_1m"));
        var badHb = new List<QuerySpec>
        {
            new() { TargetId = hb.TargetId, Sources = hb.Sources, Operation = hb.Operation, Projector = hb.Projector, Sync = hb.Sync, BasedOnRef = hb.BasedOnRef, ColumnPlan = new[] { "X" } }
        };
        SchemaValidator.Validate(badHb, models);

        SchemaValidator.Reset();
        SchemaValidator.Validate(specs, models);
        var mutated = specs.Select(s => s.TargetId == live.TargetId
            ? new QuerySpec { TargetId = s.TargetId, Sources = s.Sources, Operation = s.Operation, Projector = s.Projector, Sync = s.Sync, BasedOnRef = s.BasedOnRef, ColumnPlan = new[] { "X" } }
            : s).ToList();
        SchemaValidator.Validate(mutated, models);
    }
}
