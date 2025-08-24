using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq;
using Kafka.Ksql.Linq.Core.Abstractions;
using System;
using System.Collections.Generic;
using System.Reflection;
using Kafka.Ksql.Linq.Core.Attributes;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

#nullable enable

namespace Kafka.Ksql.Linq.Tests;

public class RetryReadyChainTests
{
    private class DummyContext : IKsqlContext
    {
        public IEntitySet<T> Set<T>() where T : class => throw new NotImplementedException();
        public object GetEventSet(Type entityType) => throw new NotImplementedException();
        public Dictionary<Type, EntityModel> GetEntityModels() => new();
        public void Dispose() { }
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private class TestSet : EventSet<TestEntity>
    {
        public ErrorHandlingPolicy? LastPolicy { get; private set; }
        public TestSet() : base(new DummyContext(), CreateModel()) { }
        protected override Task SendEntityAsync(TestEntity entity, Dictionary<string, string>? headers, CancellationToken cancellationToken) => Task.CompletedTask;
        public override async IAsyncEnumerator<TestEntity> GetAsyncEnumerator(CancellationToken cancellationToken = default) { await Task.CompletedTask; yield break; }
        internal override EventSet<TestEntity> WithErrorPolicy(ErrorHandlingPolicy policy) { LastPolicy = policy; return this; }
    }

    private static EntityModel CreateModel()
    {
        var builder = new ModelBuilder();
        builder.Entity<TestEntity>();
        var model = builder.GetEntityModel<TestEntity>()!;
        model.ValidationResult = new ValidationResult { IsValid = true };
        return model;
    }

    [Fact]
    public void WithRetry_SetsPolicyAndReturnsSet()
    {
        var set = new TestSet();
        var chain = new RetryReadyChain<TestEntity>(set);
        var result = chain.WithRetry(2, TimeSpan.FromSeconds(2));

        Assert.Same(set, result);
        Assert.NotNull(set.LastPolicy);
        Assert.Equal(ErrorAction.Retry, set.LastPolicy!.Action);
        Assert.Equal(2, set.LastPolicy.RetryCount);
        Assert.Equal(TimeSpan.FromSeconds(2), set.LastPolicy.RetryInterval);
    }

    [Fact]
    public void WithRetry_NegativeCount_Throws()
    {
        var set = new TestSet();
        var chain = new RetryReadyChain<TestEntity>(set);
        Assert.Throws<ArgumentException>(() => chain.WithRetry(-1));
    }

    [Fact]
    public void Build_ReturnsOriginalSet()
    {
        var set = new TestSet();
        var chain = new RetryReadyChain<TestEntity>(set);
        Assert.Same(set, chain.Build());
    }

    [KsqlTopic("t")]
    private class TestEntity { [KsqlKey(Order = 0)] public int Id { get; set; } }
}
