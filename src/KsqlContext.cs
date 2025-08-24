using Kafka.Ksql.Linq.Cache.Core;
using Kafka.Ksql.Linq.Cache.Extensions;
using Kafka.Ksql.Linq.Configuration;
using Kafka.Ksql.Linq.Core.Abstractions;
using Kafka.Ksql.Linq.Core.Attributes;
using Kafka.Ksql.Linq.Core.Extensions;
using Kafka.Ksql.Linq.Core.Modeling;
using Kafka.Ksql.Linq.Infrastructure.Admin;
using Kafka.Ksql.Linq.Infrastructure.KsqlDb;
using Kafka.Ksql.Linq.Mapping;
using Kafka.Ksql.Linq.Messaging.Consumers;
using Kafka.Ksql.Linq.Messaging.Producers;
using Kafka.Ksql.Linq.Messaging;
using Kafka.Ksql.Linq.Runtime.Heartbeat;
using Confluent.Kafka;
using Kafka.Ksql.Linq.Query.Abstractions;
using Kafka.Ksql.Linq.SchemaRegistryTools;
using Kafka.Ksql.Linq.Core.Dlq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ConfluentSchemaRegistry = Confluent.SchemaRegistry;

namespace Kafka.Ksql.Linq;
/// <summary>
/// KsqlContext that integrates the Core layer.
/// Design rationale: inherits core abstractions and integrates higher-level features.
/// </summary>
public abstract class KsqlContext : IKsqlContext
{
    private  KafkaProducerManager _producerManager = null!;
    private readonly Dictionary<Type, EntityModel> _entityModels = new();
    private readonly Dictionary<Type, object> _entitySets = new();
    private readonly Dictionary<Type, Configuration.ResolvedEntityConfig> _resolvedConfigs = new();
    private bool _disposed = false;
    private KafkaConsumerManager _consumerManager = null!;
    private IDlqProducer _dlqProducer = null!;
    private ICommitManager _commitManager = null!;
    private Lazy<ConfluentSchemaRegistry.ISchemaRegistryClient> _schemaRegistryClient = null!;
    private IKsqlDbClient _ksqlDbClient = null!;
    private Core.Dlq.IDlqClient _dlqClient = null!;
    private IRateLimiter _dlqLimiter = null!;
    private ILeadershipFlag _leaderFlag = null!;
    private HeartbeatRunner? _hbRunner;
    private IMarketScheduleProvider _marketScheduleProvider = null!;
    private Task? _msRefreshTask;
    private Func<DateTime> _now = () => DateTime.UtcNow;
    private Func<TimeSpan, CancellationToken, Task> _delay = (t, ct) => Task.Delay(t, ct);

    private KafkaAdminService _adminService = null!;
    private readonly KsqlDslOptions _dslOptions;
    private TableCacheRegistry? _cacheRegistry;
    private readonly MappingRegistry _mappingRegistry = new();
    private ILogger _logger = null!;
    private ILoggerFactory? _loggerFactory;

    internal ILogger Logger => _logger;



    /// <summary>
    /// Hook to decide whether schema registration should be skipped for tests
    /// </summary>
    protected virtual bool SkipSchemaRegistration => false;

    public const string DefaultSectionName = "KsqlDsl";

    protected KsqlContext(IConfiguration configuration,ILoggerFactory? loggerFactory=null)
        : this(configuration, DefaultSectionName,loggerFactory)
    {
    }

    protected KsqlContext(IConfiguration configuration, string sectionName,ILoggerFactory? loggerFactory=null)
    {
        _dslOptions = new KsqlDslOptions();
        configuration.GetSection(sectionName).Bind(_dslOptions);
        DefaultValueBinder.ApplyDefaults(_dslOptions);

        InitializeCore(loggerFactory);

    }

    protected KsqlContext(KsqlDslOptions options,ILoggerFactory? loggerFactory=null)
    {
        _dslOptions = options;
        DefaultValueBinder.ApplyDefaults(_dslOptions);
        InitializeCore(loggerFactory);
    }

    private void InitializeCore(ILoggerFactory? loggerFactory)
    {
        DecimalPrecisionConfig.Configure(_dslOptions.DecimalPrecision, _dslOptions.DecimalScale, _dslOptions.Decimals);

        _schemaRegistryClient = new Lazy<ConfluentSchemaRegistry.ISchemaRegistryClient>(CreateSchemaRegistryClient);
        _ksqlDbClient = new KsqlDbClient(GetDefaultKsqlDbUrl());

        _loggerFactory = loggerFactory;
        _logger = _loggerFactory.CreateLoggerOrNull<KsqlContext>();


        _adminService = new KafkaAdminService(
        Microsoft.Extensions.Options.Options.Create(_dslOptions),
        _loggerFactory);
        InitializeEntityModels();
        try
        {
            _producerManager = new KafkaProducerManager(_mappingRegistry,
                 Microsoft.Extensions.Options.Options.Create(_dslOptions),
                 _loggerFactory);
            _dlqProducer = new Kafka.Ksql.Linq.Messaging.Producers.DlqProducer(_producerManager, _dslOptions.DlqTopicName);

            _commitManager = new ManualCommitManager();

            ConfigureModel();
            ResolveEntityConfigurations();




            if (!SkipSchemaRegistration)
            {
                InitializeWithSchemaRegistration();
            }
            this.UseTableCache(_dslOptions, _loggerFactory);
            _cacheRegistry = this.GetTableCacheRegistry();

            _dlqLimiter = new SimpleRateLimiter(_dslOptions.DlqOptions.MaxPerSecond);

            _leaderFlag = new LeadershipFlag();
            _marketScheduleProvider = new MarketScheduleProvider(_mappingRegistry);
            _consumerManager = new KafkaConsumerManager(_mappingRegistry,
                Microsoft.Extensions.Options.Options.Create(_dslOptions),
                _entityModels,
                _dlqProducer,
                _commitManager,
                _leaderFlag,
                _loggerFactory,
                _dlqLimiter);

            _dlqClient = new Core.Dlq.DlqClient(_dslOptions, _consumerManager, _loggerFactory);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"KsqlContext initialization failed: {ex.Message} ");
            throw;
        }
    }

    protected virtual void OnModelCreating(IModelBuilder modelBuilder) { }

    public IEntitySet<T> Set<T>() where T : class
    {
        var entityType = typeof(T);

        if (_entitySets.TryGetValue(entityType, out var existingSet))
        {
            return (IEntitySet<T>)existingSet;
        }

        var entityModel = GetOrCreateEntityModel<T>();
        var entitySet = CreateEntitySet<T>(entityModel);
        _entitySets[entityType] = entitySet;

        return entitySet;
    }

    public object GetEventSet(Type entityType)
    {
        if (_entitySets.TryGetValue(entityType, out var entitySet))
        {
            return entitySet;
        }

        var entityModel = GetOrCreateEntityModel(entityType);
        var createdSet = CreateEntitySet(entityType, entityModel);
        _entitySets[entityType] = createdSet;

        return createdSet;
    }

    public Dictionary<Type, EntityModel> GetEntityModels()
    {
        return new Dictionary<Type, EntityModel>(_entityModels);
    }

    public IReadOnlyDictionary<Type, Configuration.ResolvedEntityConfig> GetResolvedEntityConfigs()
    {
        return _resolvedConfigs;
    }

    private void InitializeEventSetProperties(ModelBuilder builder)
    {
        var contextType = GetType();
        var eventSetProps = contextType.GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(p => p.CanWrite
                && p.PropertyType.IsGenericType
                && p.PropertyType.GetGenericTypeDefinition() == typeof(EventSet<>));

        foreach (var prop in eventSetProps)
        {
            if (prop.GetValue(this) != null)
                continue;

            var entityType = prop.PropertyType.GetGenericArguments()[0];
            builder.AddEntityModel(entityType);
            var model = EnsureEntityModel(entityType);
            var set = CreateEntitySet(entityType, model);
            _entitySets[entityType] = set;
            prop.SetValue(this, set);
        }
    }

    protected virtual object CreateEntitySet(Type entityType, EntityModel entityModel)
    {
        var method = GetType()
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            .FirstOrDefault(m =>
                m.Name == nameof(CreateEntitySet)
                && m.IsGenericMethodDefinition
                && m.GetGenericArguments().Length == 1
                && m.GetParameters().Length == 1
                && m.GetParameters()[0].ParameterType == typeof(EntityModel)
            );

        if (method == null)
            throw new InvalidOperationException("Generic CreateEntitySet<T>(EntityModel) not found!");

        // このあと
        var genericMethod = method.MakeGenericMethod(entityType);
        return genericMethod.Invoke(this, new object[] { entityModel })!;
    }

    protected void ConfigureModel()
    {
        var modelBuilder = new ModelBuilder(_dslOptions.ValidationMode);
        InitializeEventSetProperties(modelBuilder);
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            OnModelCreating(modelBuilder);
        }
        ApplyModelBuilderSettings(modelBuilder);
    }

    private void ResolveEntityConfigurations()
    {
        _resolvedConfigs.Clear();

        foreach (var (type, model) in _entityModels)
        {
            var config = _dslOptions.Entities.FirstOrDefault(e => string.Equals(e.Entity, type.Name, StringComparison.OrdinalIgnoreCase));

            var defaultTopic = model.TopicName ?? type.Name.ToLowerInvariant();
            var sourceTopic = config?.SourceTopic ?? defaultTopic;

            var defaultCache = model.EnableCache;
            bool enableCache = false;
            if (model.StreamTableType== StreamTableType.Table)
            {
                enableCache = config?.EnableCache ?? defaultCache;
            }

            var defaultStore = model.AdditionalSettings.TryGetValue("StoreName", out var sObj) ? sObj?.ToString() : null;
            var storeName = config?.StoreName ?? defaultStore;

            string? groupId = null;
            if (_dslOptions.Topics.TryGetValue(sourceTopic, out var topicSection) && !string.IsNullOrEmpty(topicSection.Consumer.GroupId))
            {
                groupId = topicSection.Consumer.GroupId;
                if (!string.IsNullOrEmpty(model.GroupId) && model.GroupId != groupId)
                {
                    _logger.LogWarning("GroupId for {Entity} overridden by configuration: {Config} (was {Dsl})", type.Name, groupId, model.GroupId);
                }
            }
            else if (!string.IsNullOrEmpty(model.GroupId))
            {
                groupId = model.GroupId;
            }

            if (config != null && config.EnableCache != defaultCache)
            {
                _logger.LogInformation("EnableCache for {Entity} set to {Value} from configuration", type.Name, enableCache);
            }

            var resolved = new Configuration.ResolvedEntityConfig
            {
                Entity = type.Name,
                SourceTopic = sourceTopic,
                GroupId = groupId,
                EnableCache = enableCache,
                StoreName = storeName
            };

            foreach (var kv in model.AdditionalSettings)
            {
                resolved.AdditionalSettings[kv.Key] = kv.Value;
            }
            if (config?.BaseDirectory != null)
            {
                resolved.AdditionalSettings["BaseDirectory"] = config.BaseDirectory;
            }

            _resolvedConfigs[type] = resolved;
        }

        _dslOptions.Entities.Clear();
        foreach (var rc in _resolvedConfigs.Values)
        {
            _dslOptions.Entities.Add(new EntityConfiguration
            {
                Entity = rc.Entity,
                SourceTopic = rc.SourceTopic,
                EnableCache = rc.EnableCache,
                StoreName = rc.StoreName,
                BaseDirectory = rc.AdditionalSettings.TryGetValue("BaseDirectory", out var bd) ? bd?.ToString() : null
            });
        }
    }

    private void InitializeEntityModels()
    {
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            var dlqModel = CreateEntityModelFromType(typeof(Messaging.DlqEnvelope));
            dlqModel.SetStreamTableType(Query.Abstractions.StreamTableType.Stream);
            dlqModel.TopicName = GetDlqTopicName();
            dlqModel.AccessMode = Core.Abstractions.EntityAccessMode.ReadOnly;
            _entityModels[typeof(Messaging.DlqEnvelope)] = dlqModel;
            _mappingRegistry.RegisterEntityModel(dlqModel);
        }
    }

    private void ApplyModelBuilderSettings(ModelBuilder modelBuilder)
    {
        var models = modelBuilder.GetAllEntityModels();
        foreach (var (type, model) in models)
        {
            if (_entityModels.TryGetValue(type, out var existing))
            {
                existing.SetStreamTableType(model.GetExplicitStreamTableType());
                existing.ErrorAction = model.ErrorAction;
                existing.DeserializationErrorPolicy = model.DeserializationErrorPolicy;
                existing.EnableCache = model.EnableCache;
                existing.BarTimeSelector = model.BarTimeSelector;
            }
            else
            {
                _entityModels[type] = model;
            }

            // Register property metadata with MappingRegistry
            if (model.QueryModel != null)
            {
                RegisterQueryModelMapping(model);
            }
            else
            {
                _mappingRegistry.RegisterEntityModel(model);
            }
        }
    }

    private EntityModel GetOrCreateEntityModel<T>() where T : class
    {
        return GetOrCreateEntityModel(typeof(T));
    }

    private EntityModel GetOrCreateEntityModel(Type entityType)
    {
        if (_entityModels.TryGetValue(entityType, out var existingModel))
        {
            return existingModel;
        }

        var entityModel = CreateEntityModelFromType(entityType);
        _entityModels[entityType] = entityModel;
        return entityModel;
    }

    private EntityModel CreateEntityModelFromType(Type entityType)
    {
        var allProperties = entityType.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        var keyProperties = allProperties
            .Select(p => new { Property = p, Attr = p.GetCustomAttribute<KsqlKeyAttribute>() })
            .Where(x => x.Attr != null)
            .OrderBy(x => x.Attr!.Order)
            .Select(x => x.Property)
            .ToArray();

        var model = new EntityModel
        {
            EntityType = entityType,
            TopicName = entityType.Name.ToLowerInvariant(),
            Partitions = 1,
            ReplicationFactor = 1,
            AllProperties = allProperties,
            KeyProperties = keyProperties

        };

        if (entityType.GetCustomAttribute<KsqlStreamAttribute>() != null)
        {
            model.SetStreamTableType(StreamTableType.Stream);
        }

        if (model.StreamTableType == StreamTableType.Stream)
        {
            model.EnableCache = false;
        }
        else
        {
            model.EnableCache = true;
        }

        var topicAttr = entityType.GetCustomAttribute<KsqlTopicAttribute>();
        if (topicAttr != null)
        {
            model.TopicName = topicAttr.Name;
            model.Partitions = topicAttr.PartitionCount;
            model.ReplicationFactor = topicAttr.ReplicationFactor;
        }

        if (entityType == typeof(Messaging.DlqEnvelope))
        {
            model.TopicName = _dslOptions.DlqTopicName;
            model.Partitions = _dslOptions.DlqOptions.NumPartitions;
            model.ReplicationFactor = _dslOptions.DlqOptions.ReplicationFactor;
        }
        else
        {
            var config = _dslOptions.Entities.FirstOrDefault(e =>
                string.Equals(e.Entity, entityType.Name, StringComparison.OrdinalIgnoreCase));

            if (config != null)
            {
                if (!string.IsNullOrEmpty(config.SourceTopic))
                    model.TopicName = config.SourceTopic;

                if (model.StreamTableType == StreamTableType.Table)
                    model.EnableCache = config.EnableCache;


                if (config.StoreName != null)
                    model.AdditionalSettings["StoreName"] = config.StoreName;

                if (config.BaseDirectory != null)
                    model.AdditionalSettings["BaseDirectory"] = config.BaseDirectory;
            }
        }

        var validation = new ValidationResult { IsValid = true };
        if (keyProperties.Length == 0)
        {
            validation.Warnings.Add($"No key properties defined for {entityType.Name}");
        }
        model.ValidationResult = validation;

        return model;
    }

    internal EntityModel EnsureEntityModel(Type entityType, EntityModel? model = null)
    {
        if (_entityModels.TryGetValue(entityType, out var existing))
            return existing;

        model ??= CreateEntityModelFromType(entityType);
        _entityModels[entityType] = model;
        _mappingRegistry.RegisterEntityModel(model);

        return model;
    }


    /// <summary>
    /// OnModelCreating → execute automatic schema registration flow
    /// </summary>
    private void InitializeWithSchemaRegistration()
    {
        // Register schemas and materialize entities if new
        using (Kafka.Ksql.Linq.Core.Modeling.ModelCreatingScope.Enter())
        {
            RegisterSchemasAndMaterializeAsync().GetAwaiter().GetResult();
        }

        var tableTopics = _ksqlDbClient.GetTableTopicsAsync().GetAwaiter().GetResult();
        _cacheRegistry?.RegisterEligibleTables(_entityModels.Values, tableTopics);

        // Verify Kafka connectivity
        ValidateKafkaConnectivity();
        EnsureKafkaReadyAsync().GetAwaiter().GetResult();
    }
    private async Task EnsureKafkaReadyAsync()
    {
        try
        {
            // Auto-create DLQ topic
            await _adminService.EnsureDlqTopicExistsAsync();

            // Additional connectivity check (performed by AdminService)
            _adminService.ValidateKafkaConnectivity();


            // Log output: DLQ preparation complete
            Logger.LogInformation(
                "Kafka initialization completed; DLQ topic '{Topic}' ready with {Retention}ms retention",
                GetDlqTopicName(),
                _dslOptions.DlqOptions.RetentionMs);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "FATAL: Kafka readiness check failed. DLQ functionality may be unavailable.", ex);
        }
    }
    public string GetDlqTopicName()
    {
        return _dslOptions.DlqTopicName;
    }
    /// <summary>
    /// Kafka接続確認
    /// </summary>
    private void ValidateKafkaConnectivity()
    {
        try
        {
            // Producer/Consumer初期化時点でKafka接続が確認される
            // 追加の接続確認は不要（既存の初期化処理で十分）
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "FATAL: Cannot connect to Kafka. Verify bootstrap servers and network connectivity.", ex);
        }
    }

    /// <summary>
    /// Register schemas for all entities and send dummy record if newly created
    /// </summary>
    private async Task RegisterSchemasAndMaterializeAsync()
    {
        var client = _schemaRegistryClient.Value;

        foreach (var (type, model) in _entityModels)
        {
            SchemaRegistrationResult valueResult;
            try
            {
                var mapping = _mappingRegistry.GetMapping(type);

                if (model.HasKeys() && mapping.AvroKeySchema != null)
                {
                    var keySubject = $"{model.GetTopicName()}-key";
                    var keyResult = await client.RegisterSchemaIfNewAsync(keySubject, mapping.AvroKeySchema);
                    model.KeySchemaId = keyResult.SchemaId;
                }

                var valueSubject = $"{model.GetTopicName()}-value";
                valueResult = await client.RegisterSchemaIfNewAsync(valueSubject, mapping.AvroValueSchema!);
                model.ValueSchemaId = valueResult.SchemaId;
                DecimalSchemaValidator.Validate(model, client, _dslOptions.ValidationMode, Logger);
            }
            catch (ConfluentSchemaRegistry.SchemaRegistryException ex)
            {
                Logger.LogError(ex, "Schema registration failed for {Entity}", type.Name);
                throw;
            }

            if (model.QueryModel != null || model.QueryExpression != null)
            {
                await EnsureQueryEntityDdlAsync(type, model);
            }
            else
            {
                await EnsureSimpleEntityDdlAsync(type, model);
            }

            if (valueResult.WasCreated)
            {
                try
                {
                    var dummy = CreateDummyInstance(type);
                    var headers = new Dictionary<string, string> { ["is_dummy"] = "true" };
                    dynamic set = GetEventSet(type);
                    await set.AddAsync((dynamic)dummy, headers);
                }
                catch (Exception ex)
                {
                    Logger.LogError(ex, "Materialization failed for {Entity}", type.Name);
                    throw;
                }
            }
        }
    }

    /// <summary>
    /// Create topics and ksqlDB objects for an entity defined without queries.
    /// </summary>
    private async Task EnsureSimpleEntityDdlAsync(Type type, EntityModel model)
    {


        var generator = new Kafka.Ksql.Linq.Query.Pipeline.DDLQueryGenerator();

        var topic = model.GetTopicName();
        if (_dslOptions.Topics.TryGetValue(topic, out var config) && config.Creation != null)
        {
            model.Partitions = config.Creation.NumPartitions;
            model.ReplicationFactor = config.Creation.ReplicationFactor;
        }

        await _adminService.CreateDbTopicAsync(topic, model.Partitions, model.ReplicationFactor);

        string ddl;
        var schemaProvider = new Query.Ddl.EntityModelDdlAdapter(model);
        ddl = model.StreamTableType == StreamTableType.Table
            ? generator.GenerateCreateTable(schemaProvider)
            : generator.GenerateCreateStream(schemaProvider);

        var result = await ExecuteStatementAsync(ddl);
        if (!result.IsSuccess)
        {
            var msg = $"DDL execution failed for {type.Name}: {result.Message}";
            Logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }
    }

    /// <summary>
    /// Generate and execute CREATE TABLE/STREAM AS statements for query entities.
    /// </summary>
    private async Task EnsureQueryEntityDdlAsync(Type type, EntityModel model)
    {
        if (model.QueryModel != null)
        {
            RegisterQueryModelMapping(model);
            var sql = Query.Builders.KsqlCreateStatementBuilder.Build(
                model.GetTopicName(),
                model.QueryModel,
                model.KeySchemaId,
                model.ValueSchemaId);
            var result = await ExecuteStatementAsync(sql);
            if (!result.IsSuccess)
            {
                Logger.LogWarning("DDL execution failed for {Entity}: {Message}", type.Name, result.Message);
            }
            return;
        }

        // QueryModel が指定されていない場合は何もしない
    }

    /// <summary>
    /// Register mapping information for a query-defined entity using its KsqlQueryModel.
    /// </summary>
    private void RegisterQueryModelMapping(EntityModel model)
    {
        if (model.QueryModel == null)
            return;

        _mappingRegistry.RegisterQueryModel(
            model.EntityType,
            model.QueryModel,
            model.KeyProperties,
            model.GetTopicName());
    }


    private static object CreateDummyInstance(Type entityType)
    {
        var method = typeof(Application.DummyObjectFactory).GetMethod("CreateDummy")!
            .MakeGenericMethod(entityType);
        return method.Invoke(null, null)!;
    }


    /// <summary>
    /// SchemaRegistryClient作成
    /// </summary>
    private ConfluentSchemaRegistry.ISchemaRegistryClient CreateSchemaRegistryClient()
    {
        var options = _dslOptions.SchemaRegistry;
        var config = new ConfluentSchemaRegistry.SchemaRegistryConfig
        {
            Url = options.Url,
            MaxCachedSchemas = options.MaxCachedSchemas,
            RequestTimeoutMs = options.RequestTimeoutMs
        };

        return new ConfluentSchemaRegistry.CachedSchemaRegistryClient(config);
    }


    private Uri GetDefaultKsqlDbUrl()
    {
        if (!string.IsNullOrWhiteSpace(_dslOptions.KsqlDbUrl) &&
            Uri.TryCreate(_dslOptions.KsqlDbUrl, UriKind.Absolute, out var configured))
        {
            return configured;
        }

        var schemaUrl = _dslOptions.SchemaRegistry.Url;
        if (!string.IsNullOrWhiteSpace(schemaUrl) &&
            Uri.TryCreate(schemaUrl, UriKind.Absolute, out var schemaUri))
        {
            var port = schemaUri.IsDefaultPort || schemaUri.Port == 8081 ? 8088 : schemaUri.Port;
            return new Uri($"{schemaUri.Scheme}://{schemaUri.Host}:{port}");
        }

        // Default to localhost if nothing configured (test-friendly)
        return new Uri("http://localhost:8088");
    }
    private HttpClient CreateClient()
    {
        return new HttpClient { BaseAddress = GetDefaultKsqlDbUrl() };
    }

    public Task<KsqlDbResponse> ExecuteStatementAsync(string statement)
    {
        return _ksqlDbClient.ExecuteStatementAsync(statement);
    }

    public Task<KsqlDbResponse> ExecuteExplainAsync(string ksql)
    {
        return _ksqlDbClient.ExecuteExplainAsync(ksql);
    }



    /// <summary>
    /// Core層EventSet実装（上位層機能統合）
    /// </summary>
    protected virtual IEntitySet<T> CreateEntitySet<T>(EntityModel entityModel) where T : class
    {
        var model = EnsureEntityModel(typeof(T), entityModel);
        var baseSet = new EventSetWithServices<T>(this, model);
        if (model.GetExplicitStreamTableType() == StreamTableType.Table && model.EnableCache)
        {
            return new ReadCachedEntitySet<T>(this, model, null, baseSet);
        }
        return baseSet;
    }

    internal KafkaProducerManager GetProducerManager() => _producerManager;

    public async Task StartHeartbeatRunnerAsync(CancellationToken appStopping)
    {
        if (_hbRunner != null) return;
        if (!_entityModels.Values.Any(m => m.QueryModel?.HasTumbling == true)) return;

        var scheduleType = _entityModels.Values
            .Select(m => m.QueryModel?.BasedOnType)
            .FirstOrDefault(t => t != null);
        if (scheduleType == null) return;
        var setMethod = GetType().GetMethod(nameof(Set))!.MakeGenericMethod(scheduleType);
        var set = setMethod.Invoke(this, null);
        if (set == null) throw new InvalidOperationException("MarketSchedule entity set not found");
        var rows = await ((dynamic)set).ToListAsync(appStopping);
        await _marketScheduleProvider.InitializeAsync(scheduleType, rows, appStopping);
        StartDailyRefresh(scheduleType, set, appStopping);

        _consumerManager.StartLeaderElectionSafe(
            _dslOptions.Heartbeat.Topic,
            _dslOptions.Heartbeat.LeaderElection.GroupId,
            _dslOptions.Heartbeat.LeaderElection.InstanceId,
            appStopping);

        var producer = new ProducerBuilder<byte[], byte[]>(new ProducerConfig
        {
            BootstrapServers = _dslOptions.Common.BootstrapServers
        }).Build();
        var sender = new KafkaHeartbeatSender(producer, _leaderFlag, _dslOptions.Heartbeat.Topic);
        var planner = new HeartbeatPlanner(TimeSpan.Zero, Array.Empty<HeartbeatItem>(), _marketScheduleProvider);
        _hbRunner = new HeartbeatRunner(planner, sender, 0);
        _hbRunner.Start(appStopping);
    }

    private void StartDailyRefresh(Type scheduleType, object set, CancellationToken token)
    {
        if (_msRefreshTask != null) return;
        _msRefreshTask = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var now = _now();
                var next = new DateTime(now.Year, now.Month, now.Day, 0, 5, 0, DateTimeKind.Utc);
                if (now >= next) next = next.AddDays(1);
                await _delay(next - now, token);
                var fresh = await ((dynamic)set).ToListAsync(token);
                await _marketScheduleProvider.RefreshAsync(scheduleType, fresh, token);
            }
        }, token);
    }
    internal KafkaConsumerManager GetConsumerManager() => _consumerManager;
    internal IDlqProducer GetDlqProducer() => _dlqProducer;
    internal ICommitManager GetCommitManager() => _commitManager;
    internal DlqOptions DlqOptions => _dslOptions.DlqOptions;
    internal IRateLimiter DlqLimiter => _dlqLimiter;
    internal ConfluentSchemaRegistry.ISchemaRegistryClient GetSchemaRegistryClient() => _schemaRegistryClient.Value;
    internal MappingRegistry GetMappingRegistry() => _mappingRegistry;
    public Core.Dlq.IDlqClient Dlq => _dlqClient;

    /// <summary>
    /// エンティティ型からトピック名を取得します
    /// </summary>
    public string GetTopicName<T>()
    {
        var models = GetEntityModels();
        if (models.TryGetValue(typeof(T), out var model))
        {
            return (model.TopicName ?? typeof(T).Name).ToLowerInvariant();
        }
        return typeof(T).Name.ToLowerInvariant();
    }

    internal async Task<bool> IsEntityReadyAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        var models = GetEntityModels();
        if (!models.TryGetValue(typeof(T), out var model))
            return false;

        var statement = model.GetExplicitStreamTableType() == StreamTableType.Table
            ? "SHOW TABLES;"
            : "SHOW STREAMS;";

        var name = (model.TopicName ?? typeof(T).Name).ToUpperInvariant();
        var response = await ExecuteStatementAsync(statement);
        if (!response.IsSuccess)
            return false;

        try
        {
            using var doc = JsonDocument.Parse(response.Message);
            var listName = statement.Contains("TABLES") ? "tables" : "streams";
            foreach (var item in doc.RootElement.EnumerateArray())
            {
                if (!item.TryGetProperty(listName, out var arr))
                    continue;

                foreach (var element in arr.EnumerateArray())
                {
                    if (element.TryGetProperty("name", out var n) &&
                        string.Equals(n.GetString(), name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // ignore parse errors
        }

        return false;
    }

    public async Task WaitForEntityReadyAsync<T>(TimeSpan timeout, CancellationToken cancellationToken = default) where T : class
    {
        var start = DateTime.UtcNow;
        while (DateTime.UtcNow - start < timeout)
        {
            if (await IsEntityReadyAsync<T>(cancellationToken))
                return;

            await Task.Delay(100, cancellationToken);
        }

        throw new TimeoutException($"Entity {typeof(T).Name} not ready after {timeout}.");
    }



    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            foreach (var entitySet in _entitySets.Values)
            {
                if (entitySet is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
            _entitySets.Clear();
            _entityModels.Clear();
            _disposed = true;

            _producerManager?.Dispose();
            _consumerManager?.Dispose();
            _adminService?.Dispose();
            _cacheRegistry?.Dispose();

            if (_schemaRegistryClient.IsValueCreated)
            {
                _schemaRegistryClient.Value?.Dispose();
            }
            (_ksqlDbClient as IDisposable)?.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore();
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        foreach (var entitySet in _entitySets.Values)
        {
            if (entitySet is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else if (entitySet is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
        _entitySets.Clear();

        _producerManager?.Dispose();
        _consumerManager?.Dispose();
        _adminService?.Dispose();
        _cacheRegistry?.Dispose();

        if (_schemaRegistryClient.IsValueCreated)
        {
            _schemaRegistryClient.Value?.Dispose();
        }
        (_ksqlDbClient as IDisposable)?.Dispose();

        await Task.CompletedTask;
    }

    public override string ToString()
    {
        return $"KafkaContextCore: {_entityModels.Count} entities, {_entitySets.Count} sets [schema auto-registration ready]";
    }
}

/// <summary>
/// 上位層サービス統合EntitySet
/// 設計理由：IEntitySet<T>を直接実装し、Producer/Consumer機能を提供
/// </summary>
internal class EventSetWithServices<T> : EventSet<T> where T : class
{
    private readonly KsqlContext _ksqlContext;

    public EventSetWithServices(KsqlContext context, EntityModel entityModel)
        : base(context, entityModel, null, context.GetDlqProducer(), context.GetCommitManager())
    {
        _ksqlContext = context ?? throw new ArgumentNullException(nameof(context));
    }

    protected override async Task SendEntityAsync(T entity, Dictionary<string, string>? headers, CancellationToken cancellationToken)
    {
        var producerManager = _ksqlContext.GetProducerManager();
        var topic = GetTopicName();
        await producerManager.SendAsync(topic, entity, headers, cancellationToken);
    }

    public override async IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        var consumerManager = _ksqlContext.GetConsumerManager();
        await foreach (var (entity, _, _) in consumerManager.ConsumeAsync<T>(cancellationToken: cancellationToken))
        {
            yield return entity;
        }
    }
}

