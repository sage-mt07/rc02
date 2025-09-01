using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Kafka.Ksql.Linq.Infrastructure.KsqlDb;
using Kafka.Ksql.Linq;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Infrastructure.KsqlDb;

public class DictionaryKvClientTests
{
    private class StubClient : IKsqlDbClient
    {
        public KsqlDbResponse Response { get; set; } = new(true, "[]");
        public Task<KsqlDbResponse> ExecuteStatementAsync(string statement) => Task.FromResult(Response);
        public Task<KsqlDbResponse> ExecuteExplainAsync(string ksql) => Task.FromResult(new KsqlDbResponse(true, ""));
        public Task<HashSet<string>> GetTableTopicsAsync() => Task.FromResult(new HashSet<string>());
    }

    [Fact]
    public async Task GetAsync_ReturnsValue()
    {
        var stub = new StubClient { Response = new KsqlDbResponse(true, "[{\"row\":{\"columns\":[\"v\"]}}]") };
        var client = new DictionaryKvClient(stub, "t");
        Assert.Equal("v", await client.GetAsync("k"));
    }

    [Fact]
    public async Task GetAsync_ThrowsOnFailure()
    {
        var stub = new StubClient { Response = new KsqlDbResponse(false, "fail") };
        var client = new DictionaryKvClient(stub, "t");
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("k"));
        Assert.Contains("fail", ex.Message);
    }

    [Fact]
    public async Task GetAsync_ThrowsOnDuplicate()
    {
        var json = "[{\"row\":{\"columns\":[\"a\"]}},{\"row\":{\"columns\":[\"b\"]}}]";
        var stub = new StubClient { Response = new KsqlDbResponse(true, json) };
        var client = new DictionaryKvClient(stub, "t");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("k"));
    }

    [Fact]
    public async Task GetAsync_ThrowsOnEmptyValue()
    {
        var stub = new StubClient { Response = new KsqlDbResponse(true, "[{\"row\":{\"columns\":[\"\"]}}]") };
        var client = new DictionaryKvClient(stub, "t");
        await Assert.ThrowsAsync<InvalidOperationException>(() => client.GetAsync("k"));
    }

    [Fact]
    public async Task GetAsync_ReturnsNullWhenMissing()
    {
        var stub = new StubClient { Response = new KsqlDbResponse(true, "[]") };
        var client = new DictionaryKvClient(stub, "t");
        var res = await client.GetAsync("k");
        Assert.Null(res);
    }
}
