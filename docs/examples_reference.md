# サンプルコードと利用ガイド

## よくある誤用と推奨パターン
- このDSLはKafka/ksqlDB専用であり、Entity Frameworkの`Where`や`GroupBy`チェーンはKSQLには反映されません。集約粒度の指定には`Window(x)`拡張メソッドを使用してください。
- キーに利用できる型は`int`、`long`、`string`、`Guid`のみです。その他の型をキーにする場合はこれらへ変換してください。
- 生成済みのSpecificRecord型は`Confluent.SchemaRegistry.Serdes`の`AvroSerializer`でそのまま送受信できます。

## サンプル一覧
1. **Hello World** — `examples/hello-world`  
   最小構成でメッセージを送受信。`KsqlContextBuilder` と `ForEachAsync` の基礎を確認。
2. **Basic Produce & Consume** — `examples/basic-produce-consume`  
   プロデュース後にコンシュームする基本フロー。`Task.Delay` で送信待ちを挟む。
3. **Configuration** — `examples/configuration`  
   複数環境の `appsettings.json` を読み分ける設定例。
4. **Configuration Mapping** — `examples/configuration-mapping`  
   型付き設定を `KsqlContextBuilder` にマップする。
5. **Manual commit** — `examples/manual-commit`  
   手動コミットで確実に処理を終える。
   ```csharp
   await context.Orders.ForEachAsync((o, h, m) =>
   {
       Console.WriteLine($"Processing {o.OrderId}");
       context.Orders.Commit(o);
       return Task.CompletedTask;
   }, autoCommit: false);
   ```
6. **Kafka headers とメッセージメタデータ**  
   ヘッダーをフィルタに使う。
   ```csharp
   await context.Set<OrderMessage>().ForEachAsync((msg, headers, meta) =>
   {
       if (headers.TryGetValue("is_dummy", out var d) && d == "true")
           return Task.CompletedTask;
       // ...
   });
   ```
7. **Error handling** — `examples/error-handling`  
   `OnError` と `WithRetry` で例外を補足して再試行。
8. **Error handling with DLQ** — `examples/error-handling-dlq`  
   エラー時に `ErrorAction.DLQ` で死信キューへ転送。
9. **SQL Server vs Kafka** — `examples/sqlserver-vs-kafka`  
   SQL Server の書き込みを Kafka に置き換える最小サンプル。
10. **Topic Fluent API Extension** — `samples/topic_fluent_api_extension`  
    トピック設定を Fluent API で拡張するテスト付き例。
11. **Daily comparison** — `examples/daily-comparison`  
    レートを集計し日次比較を行う複合サンプル。
12. **API showcase** — `examples/api-showcase`  
    **コードなし**。`Where`・`GroupBy`・`Select` 等を組み合わせた総合例があると良い。
13. **MappingManager AddAsync** — `examples/naruse/mapping_manager`  
    **コードなし**。`MappingManager` からエンティティを登録し `AddAsync` する例が望まれる。
14. **Window finalization** — `examples/window-finalization`  
    **コードなし**。`Window(...).UseFinalized()` を用いたウィンドウ確定の例が必要。
15. **KSQL offset aggregates** — `samples/ksql_offset_aggregates`  
    **コードなし**。オフセットを基にした集計例があると理解が深まる。
