# 差分レポート（KafkaContext ↔ KsqlContext 命名揺れ）
> **Note:** このレポートには `UnifiedThreeWayJoinResult.cs` 等、現在は削除されたファイルへの言及が含まれています。JOIN 機能は後に2テーブル制限へ変更されました。

🗕 2025年6月27日（JST）
🧐 作業者: 鏡花（品質監査AI）

## セクション1: 混在状況の概要

- `KafkaContext` と `KsqlContext` の名称が設計資料および実装コードで混在
- src 以下では 26 ファイルが `KafkaContext` を参照し、4 ファイルが `KsqlContext` を参照
- ドキュメントでは両名称が複数箇所で登場し、設計書との整合性が取れていない

## セクション2: 対象ファイルと該当箇所の一覧

### `KafkaContext` を含む主なファイル
- src/Application/KsqlContext.cs
- src/Application/KsqlContextBuilder.cs
- src/Configuration/DlqTopicConfiguration.cs
- src/Core/Abstractions/IEntitySet.cs
- src/Core/Abstractions/IKafkaContext.cs
- src/Core/Context/KafkaContextCore.cs
- src/Core/Context/KafkaContextOptions.cs
- src/Core/CoreDependencyConfiguration.cs
- src/Core/CoreLayerValidation.cs
- src/Core/Extensions/LoggerFactoryExtensions.cs
- src/Core/Window/WindowAggregatedEntitySet.cs
- src/Core/Window/WindowedEntitySet.cs
- src/EventSet.cs
- src/Infrastructure/Admin/KafkaAdminService .cs
- src/KafkaContext.cs
- src/Query/Abstractions/IEventSet.cs
- src/Query/Linq/JoinResultEntitySet.cs
- src/Query/Linq/JoinableEntitySet.cs
- src/Query/Linq/UnifiedJoinResult.cs
- src/Query/Linq/UnifiedThreeWayJoinResult.cs
- src/StateStore/EventSetWithStateStore.cs
- src/StateStore/Extensions/KafkaContextStateStoreExtensions.cs
- src/StateStore/Extensions/WindowExtensions.cs
- src/StateStore/Extensions/WindowedEntitySet.cs
- src/StateStore/WindowExtensions.cs
- src/StateStore/WindowedEntitySet.cs

### `KsqlContext` を含む主なファイル
- src/Application/KsqlContext.cs
- src/Application/KsqlContextBuilder.cs
- src/Application/KsqlContextOptions.cs
- src/Application/KsqlContextOptionsExtensions.cs

### ドキュメント内の使用例
- docs/architecture_overview.md (複数箇所で両名称を記載)
- docs/dev_guide.md (`KafkaContext` API 使用例)
- docs/namespaces/application_namespace_doc.md (両名称が混在)
- docs/oss_design_combined.md (`KsqlContextBuilder` の設計説明)
- docs/diff_log/diff_overall_20250626.md (命名揺れを指摘済み)

## セクション3: 対応推奨方針（命名統一先と理由）

- DSL レイヤーの設計では `KsqlContextBuilder` を中心に `KsqlContext` 系名称が採用されているため、コンテキスト周辺クラスを `KsqlContext` 系に統一する方針を推奨
- `KafkaContext` 名は旧実装や抽象層での呼称として残存しており、今後の保守性を考えると `KsqlContext` へリネームしてドキュメントへ反映するのが望ましい
- 併せて `oss_design_combined.md` と `docs_advanced_rules.md` に命名統一ルールを追記し、コードとドキュメント双方の整合性を取ること
