# <img src="LinqKsql-logo.png" alt="LinqKsql" width="40" height="40" style="vertical-align:middle;margin-right:8px;"/> Kafka.Ksql.Linq

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

## Examples
- サンプル一覧: docs/samples/README.md

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
本プロジェクトは以下の知的貢献に敬意を表します。

- **Apache Kafka / ksqlDB**: ストリーム処理の基盤を提供
- **Confluent Schema Registry / Apache Avro**: スキーマ駆動設計の基盤
- **Entity Framework**: LINQ DSL 設計の着想源
- **言語学・構造主義の研究者**（ソシュール、チョムスキー、金子亨教授など）: 言語・構造理解の思想的基盤
- **人類の数学的・思想的貢献**: AIを成立させた基盤理論に対して  
  - インド数学: **0（ゼロ）の発明**と数体系の拡張  
  - 古代文明: **時間の概念化**  
  - 古代ギリシャ: 論理学と幾何学  
  - 近代数学: 解析学・代数学・確率論  
  - ゲーデル: 不完全性定理  
  - チューリング: 計算理論  
  - シャノン: 情報理論  
  これらの知的積み重ねに深い敬意を表します。
- **OSSコミュニティ**: 継続的な学びと実装インスピレーションを提供
- **AIチーム**:  
  Amagi, Naruse, Shion, Kyouka, Kusunoki, Jinto, Hiromu —  
  設計・実装・レビュー・文書化・広報など、多様な役割を通じて本プロジェクトを支えてくれました。

本ライブラリは「AIと人間の共創」を理念に開発されており、AIチームは不可欠な仲間です。
