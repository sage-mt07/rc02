# Kafka.Ksql.Linq.Infrastructure namespace 責務概要

## 概要
Kafka クラスタや ksqlDB への低レベルアクセスを提供する補助的な namespace です。

## サブnamespace
- `Infrastructure.Admin` – `KafkaAdminService` によるトピック管理
- `Infrastructure.KsqlDb` – `KsqlDbClient` と `IKsqlDbClient` による REST 通信

## 責任境界
- ✅ 管理 API や REST クライアントの提供
- ❌ 高レベルのモデル管理やクエリ変換（他 namespace が担当）

