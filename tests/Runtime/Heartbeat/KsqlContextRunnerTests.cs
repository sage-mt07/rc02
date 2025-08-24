using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Query.Dsl;
using Confluent.SchemaRegistry;
using Moq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Kafka.Ksql.Linq.Messaging.Consumers;
using Kafka.Ksql.Linq.Messaging.Producers;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Runtime.Heartbeat;
using Kafka.Ksql.Linq.Core.Dlq;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Runtime.Heartbeat;

public class KsqlContextRunnerTests
{
    private class Tick
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    private class TickView
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime BucketStart { get; set; }
    }

    public class MarketSchedule
    {
        public string Broker { get; set; } = string.Empty;
        public string Symbol { get; set; } = string.Empty;
        public DateTime Open { get; set; }
        public DateTime Close { get; set; }
        public DateTime MarketDate { get; set; }
    }

    private class TumblingContext : KsqlContext
    {
        public TumblingContext() : base(new KsqlDslOptions())
        {
            typeof(KsqlContext).GetField("_schemaRegistryClient", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(this, new Lazy<ISchemaRegistryClient>(() => new Mock<ISchemaRegistryClient>().Object));
        }
        protected override bool SkipSchemaRegistration => true;
        protected override void OnModelCreating(IModelBuilder builder)
        {
            builder.Entity<MarketSchedule>();
            builder.Entity<TickView>().ToQuery(q => q.From<Tick>()
                .TimeFrame<MarketSchedule>(
                    (r, s) => r.Broker == s.Broker && r.Symbol == s.Symbol && s.Open <= r.Timestamp && r.Timestamp < s.Close,
                    s => s.MarketDate)
                .Tumbling(t => t.Timestamp, minutes: new[] { 1 })
                .GroupBy(t => new { t.Broker, t.Symbol, BucketStart = t.Timestamp })
                .Select(g => new TickView { Broker = g.Key.Broker, Symbol = g.Key.Symbol, BucketStart = g.Key.BucketStart }));
        }
    }

    private class FakeManager : KafkaConsumerManager
    {
        public bool Called;
        public FakeManager()
            : base(new MappingRegistry(), Options.Create(new KsqlDslOptions()), new(),
                new Mock<IDlqProducer>().Object, new ManualCommitManager(), new LeadershipFlag(), null, new SimpleRateLimiter(0))
        { }
        public override void StartLeaderElectionSafe(string? topic = null, string? groupId = null, string? instanceId = null, CancellationToken ct = default)
        {
            Called = true;
        }
    }

    [Fact]
    public void KsqlContext_StartsLeaderElection_Then_Runner_WhenHasTumbling()
    {
        var ctx = new TumblingContext();
        var fake = new FakeManager();
        typeof(KsqlContext).GetField("_consumerManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, fake);
        var setMock = new Mock<IEntitySet<MarketSchedule>>();
        setMock.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MarketSchedule>());
        typeof(KsqlContext).GetField("_entitySets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, new Dictionary<Type, object> { { typeof(MarketSchedule), setMock.Object } });
        ctx.StartHeartbeatRunnerAsync(CancellationToken.None).GetAwaiter().GetResult();
        var runner = typeof(KsqlContext).GetField("_hbRunner", BindingFlags.NonPublic | BindingFlags.Instance)!
            .GetValue(ctx);
        Assert.NotNull(runner);
        Assert.True(fake.Called);
    }

    [Fact]
    public void ToQuery_WithTumbling_Triggers_MarketSchedule_Load_Once()
    {
        var ctx = new TumblingContext();
        var fake = new FakeManager();
        typeof(KsqlContext).GetField("_consumerManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, fake);

        var setMock = new Mock<IEntitySet<MarketSchedule>>();
        setMock.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MarketSchedule>());
        typeof(KsqlContext).GetField("_entitySets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, new Dictionary<Type, object> { { typeof(MarketSchedule), setMock.Object } });

        ctx.StartHeartbeatRunnerAsync(CancellationToken.None).GetAwaiter().GetResult();
        ctx.StartHeartbeatRunnerAsync(CancellationToken.None).GetAwaiter().GetResult();
        setMock.Verify(s => s.ToListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void StartsDailyRefresh_And_RefreshesProvider()
    {
        var ctx = new TumblingContext();
        var fake = new FakeManager();
        typeof(KsqlContext).GetField("_consumerManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, fake);

        var rows = new List<MarketSchedule>();
        var setMock = new Mock<IEntitySet<MarketSchedule>>();
        setMock.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(rows);
        typeof(KsqlContext).GetField("_entitySets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, new Dictionary<Type, object> { { typeof(MarketSchedule), setMock.Object } });

        var providerMock = new Mock<IMarketScheduleProvider>();
        providerMock.Setup(p => p.InitializeAsync(typeof(MarketSchedule), It.IsAny<IEnumerable>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        typeof(KsqlContext).GetField("_marketScheduleProvider", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, providerMock.Object);

        var cts = new CancellationTokenSource();
        TimeSpan captured = TimeSpan.Zero;
        var tcs = new TaskCompletionSource();
        providerMock.Setup(p => p.RefreshAsync(typeof(MarketSchedule), rows, It.IsAny<CancellationToken>())).Returns(() => { tcs.SetResult(); cts.Cancel(); return Task.CompletedTask; });
        typeof(KsqlContext).GetField("_now", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, (Func<DateTime>)(() => new DateTime(2025,1,1,0,0,0,DateTimeKind.Utc)));
        typeof(KsqlContext).GetField("_delay", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, (Func<TimeSpan, CancellationToken, Task>)((t, c) => { captured = t; return Task.CompletedTask; }));

        ctx.StartHeartbeatRunnerAsync(cts.Token).GetAwaiter().GetResult();
        tcs.Task.Wait(1000);
        providerMock.Verify(p => p.RefreshAsync(typeof(MarketSchedule), rows, It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal(TimeSpan.FromMinutes(5), captured);
    }

    [Fact]
    public void ToListAsync_DoesNotRequire_AppLevel_PK_Hardcode()
    {
        var ctx = new TumblingContext();
        var fake = new FakeManager();
        typeof(KsqlContext).GetField("_consumerManager", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, fake);

        var setMock = new Mock<IEntitySet<MarketSchedule>>();
        setMock.Setup(s => s.ToListAsync(It.IsAny<CancellationToken>())).ReturnsAsync(new List<MarketSchedule>());
        typeof(KsqlContext).GetField("_entitySets", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(ctx, new Dictionary<Type, object> { { typeof(MarketSchedule), setMock.Object } });

        ctx.StartHeartbeatRunnerAsync(CancellationToken.None).GetAwaiter().GetResult();
        setMock.Verify(s => s.ToListAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
