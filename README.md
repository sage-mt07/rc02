# <img src="LinqKsql-logo.png" alt="LinqKsql" width="100" height="100" style="vertical-align:middle;margin-right:8px;"/> Kafka.Ksql.Linq &nbsp;<img src="experimental.png" alt="Experimental"  height="30" style="vertical-align:middle;margin-right:8px;"/>

## 特徴
- LINQ ライクに Kafka/ksqlDB を扱える
- Avro + Schema Registry 対応の型安全 DSL
- Window/集約処理や Push/Pull クエリ対応
- DLQ / Retry / Commit を含むエラーハンドリング

## Quick Start
```
git clone <repository-url>
cd rc02
dotnet restore

docker-compose -f tools/docker-compose.kafka.yml up -d

cd examples/hello-world
dotnet run
```
## structure
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

## Examples
- サンプル一覧: [exsamples/index.md](./examples/index.md)

## Reference
- API: docs/api_reference.md
- Configuration: docs/configuration_reference.md
- Advanced: docs/advanced_rules.md

## License
- ソースコードは [MIT License](./LICENSE) の下で公開
- ドキュメントは [Creative Commons Attribution 4.0 International (CC BY 4.0)](https://creativecommons.org/licenses/by/4.0/) の下で公開

## Roadmap
- 2025 Q4
  - Oneshot対応: ksqldbに単発登録を行うPod構成に対応する機能を提供
  - .NET 10 対応: 最新ランタイムでの動作保証


## Acknowledgements
　- [Acknowledgements](./docs/acknowledgements.md)