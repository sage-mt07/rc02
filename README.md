# <img src="LinqKsql-logo.png" alt="LinqKsql" width="100" height="100" style="vertical-align:middle;margin-right:8px;"/> &nbsp; &nbsp; Kafka.Ksql.Linq &nbsp;&nbsp;<img src="experimental.png" alt="Experimental"  height="30" style="vertical-align:middle;margin-right:8px;"/>

## 概要
- LINQ で Kafka/ksqlDB を操作する C# ライブラリ
- Avro + Schema Registry を前提とした型安全な DSL
- Streams/Tables, Pull/Push をサポート（実行モードは自動推論）
- エラー処理（DLQ）/ リトライ / コミットの運用補助

## クイックスタート（ローカルで10分）
```
git clone <repository-url>
cd rc02
dotnet restore

docker-compose -f tools/docker-compose.kafka.yml up -d

# 実行例（examples は順次追加中）
# cd examples/hello-world && dotnet run

public class HelloMessage
{
    public int Id { get; set; }
    public string Text { get; set; } = string.Empty;
}

public class HelloKafkaContext : KsqlContext
{
    public HelloKafkaContext(KsqlContextOptions options) : base(options.Configuration!, options.LoggerFactory) { }
    public HelloKafkaContext(Microsoft.Extensions.Configuration.IConfiguration configuration, Microsoft.Extensions.Logging.ILoggerFactory? loggerFactory = null) : base(configuration, loggerFactory) { }
    public EventSet<HelloMessage> HelloMessages { get; set; }
    protected override void OnModelCreating(IModelBuilder modelBuilder)
    {
    }
}

class Program
{
    static async Task Main(string[] args)
    {
        var configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        await using var context = new HelloKafkaContext(configuration, LoggerFactory.Create(b => b.AddConsole()));

        var message = new HelloMessage
        {
            Id = Random.Shared.Next(),
            Text = "Hello World"
        };
        await context.HelloMessages.AddAsync(message);
        await context.HelloMessages.ForEachAsync(m =>
        {
            Console.WriteLine($"Received: {m.Text}");
            return Task.CompletedTask;
        });
    }
}
```


## 構成イメージ
1) DSL 全体アーキテクチャ図
``` mermaid
flowchart TB
    subgraph App["C# アプリケーション"]
        A[LINQ / DSL 呼び出し]
    end

    A --> B[DSL]
    B --> C[Query Builder]
    C --> D[KSQL Generator]
    D -->|DDL/CSAS/CTAS| E[KsqlDB]
    E -->|Read/Write| F[(Kafka Topics)]

    %% 補助コンポーネント
    subgraph Schema["Schema Management"]
        SR[(Schema Registry)]
        AV[Avro Serializer/Deserializer]
    end

    D --> SR
    SR --- AV
    AV --- F

    %% 運用・モード
    subgraph Ops["運用機能"]
        EH[DLQ / Retry / Commit]
        MODE[Streaming Mode\nPush / Pull]
    end

    E ---> EH
    E ---> MODE

    %% キャッシュ層
    subgraph Cache["ローカルキャッシュ"]
        ST[Streamiz]
        RDB[(RocksDB)]
    end
    ST --- RDB
    ST -. 状態ストア .- E
``` 
2) Produce / Consume と API（Stream・Table・RocksDB）
``` mermaid
flowchart TB
    %% Stream API
    subgraph STREAM["Stream"]
        SAdd["AddAsync(payload)"]
        SFor["ForEachAsync(handler, token)"]
    end

    %% Table API
    subgraph TABLE["Table"]
        TAdd["AddAsync(entity)"]
        TList["ToListAsync()"]
    end

    %% ksqlDB / Topics / StateStore
    KSQLS["ksqlDB STREAM"]
    KSQLT["ksqlDB TABLE (changelog)"]
    TOPIC[(Kafka Topic)]
    STATE["State Store\n(Streamiz)"]
    ROCKS[(RocksDB)]

    %% Stream: produce & consume
    SAdd -->|produce| TOPIC
    TOPIC -->|source| KSQLS
    SFor <-->|push consume| KSQLS

    %% Table: upsert & fast read
    TAdd -->|upsert| KSQLT
    KSQLT -->|materialized store| STATE
    STATE --- ROCKS
    TList -->|read via local store| STATE

    %% 説明ラベル
    classDef dim fill:#f6f8fa,stroke:#d0d7de,color:#24292f;
    class STREAM,TABLE,STATE,ROCKS,KSQLS,KSQLT,TOPIC dim;
```

## 例（Examples）
- 目次: `docs/examples/index.md`
- OnModelCreating サンプル集: `docs/onmodelcreating_samples.md`

## ドキュメント（リファレンス）
- 関数/型対応表: `docs/ksql-function-type-mapping.md`
- SQLServer→ksqlDB ガイド: `docs/sqlserver-to-kafka-guide.md`
- API: `docs/api_reference.md`
- Configuration: `docs/configuration_reference.md`
- Advanced: `docs/advanced_rules.md`

## ライセンス / ロードマップ
- License: [MIT License](./LICENSE)
- Docs: 一部 CC BY 4.0 を想定
- Roadmap（例）
  - 安定化と examples 追加
  - .NET 10 対応


## Acknowledgements
- [Acknowledgements](./docs/acknowledgements.md)
