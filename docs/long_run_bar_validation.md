# 長時間物理試験計画（bar_1d/bar_1wk）

## 目的 / 想定読者 / 成果物
- 目的: bar_1d / bar_1wk（live/final）の素材化・遅延特性を実時間で把握し、安定運用に必要な待機/リトライ方針を明確化する
- 想定読者: 運用担当・SRE・開発リーダー
- 成果物: 24h 観測記録（件数/遅延/失敗事象）と改善提案

## 前提
- ksqlDB: `http://localhost:8088` が安定応答
- Schema Registry: `http://localhost:8081/subjects` が安定応答
- Kafka: `localhost:9092`
- 物理テストプロジェクト: `physicalTests/`

## 手順（TL;DR）
1. 環境リセット: `physicalTests/reset.ps1` 実行（down -v → up → wait）
2. ソース作成: `DEDUPRATES(BROKER KEY, SYMBOL, TS, BID)`, `MSCHED(BROKER KEY, SYMBOL, OPEN_TS, CLOSE_TS)`
3. CSAS作成: `bar_1d_live` / `bar_1wk_final`（必要に応じて final/live 全パターン）
4. データ投入: 10–60秒間隔で `DEDUPRATES` へ INSERT（TS は現在/翌日を加工）
5. 観測: Push（/query-stream）と Pull（/query）で件数/遅延を 1–10分間隔で採取
6. 記録: `Reportsx/physical/<UTC>/` にログ、`docs/changes/YYYYMMDD_progress.md` に所見
7. 終了: `TERMINATE ALL; DROP TABLE/STREAM ... DELETE TOPIC;` → down -v

備考: サンプル実行は既定で約5分で停止する（CancellationToken による自動終了）。長時間観測に切り替える場合は各サンプルの `CancellationTokenSource(TimeSpan.FromMinutes(5))` を調整する。

## 詳細
- Push: `SELECT * FROM bar_1d_live EMIT CHANGES LIMIT 2;`（`auto.offset.reset=earliest`）
- Pull: `SELECT * FROM bar_1wk_final WHERE BROKER='B' AND SYMBOL='S' LIMIT 10;`
- 週アンカー（日/⽉）差分は `MSCHED` の `OPEN_TS/CLOSE_TS` で管理し、WHERE で包含条件を明示
- CSAS直後は素材化に時間がかかるため、2–3秒待機＋最大 120–180 秒の再試行を行う

## 検証 / 成功判定
- `SHOW TABLES;` にて対象テーブルが存在
- Push/Pull のどちらかで 1件以上の行が定期的に観測される
- Final 系は EMIT FINAL/GRACE の意味に応じた遅延内で行が出る
- 週アンカー（日/⽉）差分の両テーブルで >=1 行が観測される

## トラブルシュート
- /info reset や /query-stream 切断が継続 → 環境不安定（ksqlDB/Schema Registry/Kafka）としてリセット・再試行
- CSAS重複 → `DROP TABLE IF EXISTS ... DELETE TOPIC;` 後に再作成
- Pull で 400/0件 → キー（BROKER/SYMBOL）や WindowStart/End を明示、または Push で確認

## 参考
- 物理最小構成: `docs/physical_test_minimum.md`
- サンプル方針: `docs/samples/README.md`
