using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Configuration.Messaging;
using Kafka.Ksql.Linq.Infrastructure.Admin;
using Xunit;
using static Kafka.Ksql.Linq.Tests.PrivateAccessor;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Kafka.Ksql.Linq.Tests.Infrastructure;
#nullable enable

public class KafkaAdminServiceTests
{
    private class FakeAdminClient : DispatchProxy
    {
        public Func<TimeSpan, Metadata> MetadataHandler { get; set; } = _ =>
        {
            var metadata = (Metadata)RuntimeHelpers.GetUninitializedObject(typeof(Metadata));
            SetMember(metadata, "Topics", new List<TopicMetadata>());
            return metadata;
        };
        public Func<IEnumerable<TopicSpecification>, CreateTopicsOptions?, Task> CreateHandler { get; set; } = (_, __) => Task.CompletedTask;

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            switch (targetMethod?.Name)
            {
                case "CreateTopicsAsync":
                    return CreateHandler((IEnumerable<TopicSpecification>)args![0]!, (CreateTopicsOptions?)args[1]);
                case "GetMetadata":
                    return MetadataHandler((TimeSpan)args![0]!);
                case "Dispose":
                    return null;
                case "get_Name":
                    return "fake";
                case "get_Handle":
                    return null!;
            }
            throw new NotImplementedException(targetMethod?.Name);
        }
    }

    private static void SetMember(object obj, string name, object? value)
    {
        var type = obj.GetType();
        var prop = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (prop != null && prop.SetMethod != null)
        {
            prop.SetValue(obj, value);
            return;
        }
        var field = type.GetField($"<{name}>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                    ?? type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        if (field != null)
        {
            field.SetValue(obj, value);
            return;
        }
        throw new ArgumentException($"Property set method not found for {name}");
    }

    private static Metadata CreateMetadata(IEnumerable<TopicMetadata> topics)
    {
        var metadata = (Metadata)RuntimeHelpers.GetUninitializedObject(typeof(Metadata));
        SetMember(metadata, "Topics", topics.ToList());
        return metadata;
    }

    private static KafkaAdminService CreateUninitialized(KsqlDslOptions options, IAdminClient? adminClient = null)
    {
        DefaultValueBinder.ApplyDefaults(options);
        var svc = (KafkaAdminService)RuntimeHelpers.GetUninitializedObject(typeof(KafkaAdminService));
        typeof(KafkaAdminService)
            .GetField("_options", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(svc, options);
        if (adminClient != null)
        {
            typeof(KafkaAdminService)
                .GetField("_adminClient", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(svc, adminClient);
        }
        return svc;
    }

    [Fact]
    public void CreateAdminConfig_Plaintext_ReturnsBasicSettings()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection
            {
                BootstrapServers = "server:9092",
                ClientId = "cid",
                AdditionalProperties = new Dictionary<string, string> { ["p"] = "v" }
            }
        };
        DefaultValueBinder.ApplyDefaults(options);

        var svc = CreateUninitialized(options);
        var config = InvokePrivate<AdminClientConfig>(svc, "CreateAdminConfig", Type.EmptyTypes);

        Assert.Equal("server:9092", config.BootstrapServers);
        Assert.Equal("cid-admin", config.ClientId);
        Assert.Equal(options.Common.MetadataMaxAgeMs, config.MetadataMaxAgeMs);
        Assert.Equal("v", config.Get("p"));
    }

    [Fact]
    public void CreateAdminConfig_WithSecurityOptions()
    {
        var options = new KsqlDslOptions
        {
            Common = new CommonSection
            {
                BootstrapServers = "server:9092",
                ClientId = "cid",
                SecurityProtocol = SecurityProtocol.SaslSsl,
                SaslMechanism = SaslMechanism.Plain,
                SaslUsername = "user",
                SaslPassword = "pass",
                SslCaLocation = "ca.crt",
                SslCertificateLocation = "cert.crt",
                SslKeyLocation = "key.key",
                SslKeyPassword = "pw"
            }
        };
        DefaultValueBinder.ApplyDefaults(options);

        var svc = CreateUninitialized(options);
        var config = InvokePrivate<AdminClientConfig>(svc, "CreateAdminConfig", Type.EmptyTypes);

        Assert.Equal(SecurityProtocol.SaslSsl, config.SecurityProtocol);
        Assert.Equal(SaslMechanism.Plain, config.SaslMechanism);
        Assert.Equal("user", config.SaslUsername);
        Assert.Equal("pass", config.SaslPassword);
        Assert.Equal("ca.crt", config.SslCaLocation);
        Assert.Equal("cert.crt", config.SslCertificateLocation);
        Assert.Equal("key.key", config.SslKeyLocation);
        Assert.Equal("pw", config.SslKeyPassword);
    }

    [Fact]
    public async Task CreateDbTopicAsync_Succeeds_WhenTopicDoesNotExist()
    {
        var options = new KsqlDslOptions();
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var fake = (FakeAdminClient)proxy!;
        var created = false;
        fake.MetadataHandler = _ => CreateMetadata(Array.Empty<TopicMetadata>());
        fake.CreateHandler = (_, __) => { created = true; return Task.CompletedTask; };

        var svc = CreateUninitialized(options, proxy);
        await svc.CreateDbTopicAsync("t", 1, 1);

        Assert.True(created);
    }

    [Fact]
    public async Task CreateDbTopicAsync_NoOp_WhenTopicExists()
    {
        var options = new KsqlDslOptions();
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var fake = (FakeAdminClient)proxy!;
        var meta = (TopicMetadata)RuntimeHelpers.GetUninitializedObject(typeof(TopicMetadata));
        SetMember(meta, "Topic", "t");
        SetMember(meta, "Error", new Error(ErrorCode.NoError));
        fake.MetadataHandler = _ => CreateMetadata(new[] { meta });
        var created = false;
        fake.CreateHandler = (_, __) => { created = true; return Task.CompletedTask; };

        var svc = CreateUninitialized(options, proxy);
        await svc.CreateDbTopicAsync("t", 1, 1);

        Assert.False(created);
    }

    [Fact]
    public async Task CreateDbTopicAsync_Throws_WhenKafkaError()
    {
        var options = new KsqlDslOptions();
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var fake = (FakeAdminClient)proxy!;
        fake.MetadataHandler = _ => CreateMetadata(Array.Empty<TopicMetadata>());
        fake.CreateHandler = (_, __) => throw new KafkaException(new Error(ErrorCode.Local_Transport));

        var svc = CreateUninitialized(options, proxy);

        await Assert.ThrowsAsync<KafkaException>(() => svc.CreateDbTopicAsync("t", 1, 1));
    }

    [Theory]
    [InlineData(null, 1, (short)1)]
    [InlineData("", 1, (short)1)]
    [InlineData("t", 0, (short)1)]
    [InlineData("t", 1, (short)0)]
    public async Task CreateDbTopicAsync_InvalidParameters_Throws(string name, int partitions, short rep)
    {
        var options = new KsqlDslOptions();
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var svc = CreateUninitialized(options, proxy);

        await Assert.ThrowsAsync<ArgumentException>(() => svc.CreateDbTopicAsync(name!, partitions, rep));
    }

    [Fact]
    public async Task TopicConfig_Applies_RetentionMs_For_Monthly()
    {
        var options = new KsqlDslOptions
        {
            Topics =
            {
                ["rate_1mo_final"] = new TopicSection
                {
                    Creation = new TopicCreationSection
                    {
                        Configs = { ["retention.ms"] = "60000" }
                    }
                }
            }
        };
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var fake = (FakeAdminClient)proxy!;
        fake.MetadataHandler = _ => CreateMetadata(Array.Empty<TopicMetadata>());
        TopicSpecification? captured = null;
        fake.CreateHandler = (specs, _) => { captured = specs.Single(); return Task.CompletedTask; };

        var svc = CreateUninitialized(options, proxy);
        await svc.EnsureTopicExistsAsync("rate_1mo_final");

        Assert.Equal("60000", captured!.Configs!["retention.ms"]);
    }

    [Fact]
    public async Task Admin_Uses_ResolvedName_For_Appsettings_Lookup()
    {
        var options = new KsqlDslOptions
        {
            Topics =
            {
                ["resolved"] = new TopicSection
                {
                    Creation = new TopicCreationSection { Configs = { ["x"] = "1" } }
                }
            }
        };
        var proxy = DispatchProxy.Create<IAdminClient, FakeAdminClient>();
        var fake = (FakeAdminClient)proxy!;
        fake.MetadataHandler = _ => CreateMetadata(Array.Empty<TopicMetadata>());
        TopicSpecification? spec = null;
        fake.CreateHandler = (specs, _) => { spec = specs.Single(); return Task.CompletedTask; };

        var svc = CreateUninitialized(options, proxy);
        await svc.EnsureTopicExistsAsync("resolved");

        Assert.Equal("1", spec!.Configs!["x"]);
    }
}
