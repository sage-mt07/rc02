## テスト安定化パッチ (2025-09-14)

目的: 物理テストでの DDL 404 (ksqlDB 未設定) と Windows 環境での Docker 操作失敗を解消し、再実行可能にする。

変更概要

- KsqlDbUrl 追加（不足箇所）
  - physicalTests/OssSamples/AdvancedDataTypeTests.cs
  - physicalTests/OssSamples/DefaultAndBoundaryValueTests.cs
  - physicalTests/OssSamples/PrimingBehaviorTests.cs
  - physicalTests/OssSamples/RocksDbToListAsyncTests.cs (+ Env に KsqlDbUrl 定数追加)
  - physicalTests/OssSamples/ManualCommitIntegrationTests.cs (+ Env に KsqlDbUrl 定数追加)
  - physicalTests/OssSamples/NoKeyPocoTests.cs
  - physicalTests/OssSamples/SchemaNameCaseSensitivityTests.cs

- DockerHelper の Windows 対応
  - physicalTests/DockerHelper.cs
    - Windows では bash 経由をやめ、`cmd.exe /c` で `docker compose` を実行
    - compose ファイルパスを明示クォート
    - 標準出力/標準エラーの取り回しを改善

影響と結果

- 以前の失敗（DDL 404）は解消。対象テストが合格へ。
- Kafka ダウン系（BigBang*/KafkaServiceDown*/KsqlDbServiceDown*）は Windows 環境でも停止/開始が成功。
- 一部テストは引き続き要調整（例: ManualCommitIntegrationTests の NullReference）。

再現/確認手順

```
dotnet build -c Release
# 代表テスト
dotnet test -c Release --no-build --no-restore physicalTests/Kafka.Ksql.Linq.Tests.Integration.csproj \
  --filter FullyQualifiedName~AdvancedDataTypeTests --results-directory reports/physical \
  --logger "trx;LogFileName=verify_AdvancedDataType.trx"
```

