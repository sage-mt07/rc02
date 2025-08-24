using System;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Query.Adapters;
using Xunit;

namespace Kafka.Ksql.Linq.Tests.Query.Adapters;

public class TopicNameResolverTests
{
    private class Dummy { }

    [Fact]
    public void TopicName_Resolved_From_PocoAttribute_Else_Candidate()
    {
        var attr = new EntityModel { EntityType = typeof(Dummy), TopicName = "attr" };
        Assert.Equal("attr", TopicNameResolver.Resolve(attr, "p_"));

        var cand = new EntityModel { EntityType = typeof(Dummy) };
        cand.AdditionalSettings["topicCandidate"] = "cand";
        Assert.Equal("cand", TopicNameResolver.Resolve(cand, "p_"));

        var def = new EntityModel { EntityType = typeof(Dummy) };
        Assert.Equal("p_dummy", TopicNameResolver.Resolve(def, "p_"));
    }

    [Fact]
    public void TopicResolver_Fallbacks_To_Id_When_EntityTypeIsObject()
    {
        var model = new EntityModel { EntityType = typeof(object) };
        model.AdditionalSettings["id"] = "raw";
        Assert.Equal("raw", TopicNameResolver.Resolve(model, "p_"));
    }

    [Fact]
    public void TopicResolver_Uses_Appsettings_Over_Poco()
    {
        var model = new EntityModel { EntityType = typeof(Dummy), TopicName = "poco" };
        Assert.Equal("cfg", TopicNameResolver.Resolve(model, "p_", "cfg"));
    }

    [Fact]
    public void TopicResolver_FallsBack_To_Poco_When_NotConfigured()
    {
        var model = new EntityModel { EntityType = typeof(Dummy), TopicName = "poco" };
        Assert.Equal("poco", TopicNameResolver.Resolve(model, "p_"));
    }

    [Fact]
    public void TopicResolver_Prefers_Appsettings_Over_Poco_And_Candidate()
    {
        var model = new EntityModel { EntityType = typeof(object), TopicName = "attr" };
        model.AdditionalSettings["topicCandidate"] = "cand";
        Assert.Equal("cfg", TopicNameResolver.Resolve(model, "p_", "cfg"));
    }

    [Fact]
    public void TopicName_Sanitize_Truncates_When_TooLong()
    {
        var longName = new string('a', 260);
        var model = new EntityModel { EntityType = typeof(Dummy), TopicName = longName };
        var resolved = TopicNameResolver.Resolve(model, "");
        using var sha = System.Security.Cryptography.SHA1.Create();
        var hash = Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(longName))).ToLowerInvariant()[..6];
        Assert.Equal(237, resolved.Length);
        Assert.Equal(new string('a', 230) + "_" + hash, resolved);
    }
}
