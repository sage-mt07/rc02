# 長時間物理試験計画（bar_1d/bar_1wk）

## 試験目的
- bar_1d / bar_1wk（live/final）における素材化（materialization）と遅延特性の実態把握。
- MarketSchedule（営業日/営業時間）適用時の日次・週次ロールアップの安定性と一貫性を検証。
- 週のアンカー（日曜/⽉曜）差分でテーブル生成・可視化が期待どおりに振る舞うことを確認。

## 試験方式（概要）
- 環境（Kafka/Schema Registry/ksqlDB）を起動し、1日（実時間）データ投入を継続。
- KSQLOBJ（CSAS/CTAS）を維持しつつ、Push（/query-stream）と Pull（/query）の両経路で件数・遅延を定期観測。
- 生成物（bar_1d_live/final、bar_1wk_live/final）の命名規約を守り、MarketSchedule JOIN 条件で包含を担保。

## 前提条件・環境
- ksqlDB:  が安定応答
- Schema Registry:  が安定応答
- Kafka: 
- 物理テストプロジェクト: 
- 既定Docker/Compose（）で単一ブローカ・RF=1

## セットアップ手順
1) 環境リセット（コンテナ + 一時ストレージ）
   - PowerShell: 
   - Bash（WSL）:  を相当実行
2) ヘルス確認
   - {"KsqlServerInfo":{"version":"0.29.0","kafkaClusterId":"ySy_eKa4Qwutm6RhkdVqcQ","ksqlServiceId":"ksql_service_1","serverStatus":"RUNNING"}}
   - []
3) ベースStream作成（JSON, TIMESTAMP='TS'）
   - Rates: 
   - Schedule: 
4) CSAS/CTASの作成（必要に応じてDROP IF EXISTS → CREATE）
   - 日次Live: 
   - 日次Final（任意）: 
   - 週次Live: 
   - 週次Final: 

参考実装
- ソース作成・CSAS: , 
- アンカー差分: , 

## データ投入計画
- 10〜60秒間隔で  に INSERT（ は現在時刻/翌日など加工）
-  に日次（OPEN〜CLOSE）と週次（アンカー〜翌週アンカー）の区間をINSERT
- 週アンカー差分（任意）:
  - 日曜アンカー: OPEN_TS = Sun 00:00Z, CLOSE_TS = next Sun 00:00Z
  - 月曜アンカー: OPEN_TS = Mon 00:00Z, CLOSE_TS = next Mon 00:00Z

## 監視・確認方法
- Push（/query-stream; EMIT CHANGES + LIMIT）
  - 例: 
  - クライアント: 
  - streamsProperties:  を付与（テストクライアント済）
- Pull（/query; TABLE向け）
  - 例: 
  - クライアント: 
  - windowed TABLE の場合は WINDOWSTART/END 指定が必要なことに留意
- リトライ/待機
  - CSAS直後に素材化完了まで 2〜3秒スリープ + 最大 120〜180秒間の再試行（テスト内実装済）

## 成功判定基準
- CSAS作成が成功し、 に各テーブル名が現れる
- Push/Pull いずれかで、日次/週次テーブルにおいて定期的に件数>0を観測
- 週アンカー差（Sun vs Mon）それぞれで少なくとも1件（>=1）の行出力がある
- Final系は  の意味に応じた遅延（GRACE含む）で行が出ること（観測ログに反映）

## 記録・成果物
- 実行ログ: （per-test ランナー出力）
  - , , , 
- 日次の進捗:  に観測値（件数/遅延/失敗事象）を追記

## 想定リスクとハンドリング
- 環境不安定（/info reset, /query-stream 切断など）
  - リトライで吸収。繰返す場合は「環境要因（ksqlDB/Schema Registry/Kafka）」と明記して記録
- CSAS衝突
  -  のうえ再作成（テストに実装）
- windowed TABLE への Pull が 400/0件
  - キーとウィンドウ境界の明示（または Push で確認）

## 実施スケジュール（例）
- T0（開始）: 環境リセット、CSAS作成、投入開始
- T0+1h: SHOW/EXPLAIN と件数確認（Push/Pull）
- T0+6h: 中間観測（件数/遅延/失敗事象）
- T0+12h: アンカー差分の追加/切替観測（任意）
- T0+24h: 終了観測・後片付け（TERMINATE, DROP, down -v）

## 役割（例）
- 天城: 全体進捗・調整、異常時の切り分け指示
- 詩音: 観測ポイント設計、ログ解析、結果サマリ
- 迅人: 簡易Pull/Pushクエリの補助ツール・スクリプト整備
- 鳴瀬: 生成DDL/DSLの差分/改善（必要時）
- 鏡花: 基準適合性（命名/含意/可視化）レビュー
- 楠木/広夢: 進捗ログ・ドキュメント更新

## 付録: 代表クエリ
- 日次Live（Push）: 
- 週次Final（Push）: 
- 週次Live（Pull, 参考）: 
