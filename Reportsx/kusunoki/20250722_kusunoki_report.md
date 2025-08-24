# 2025-07-22 楠木レポート

## EntitySetからMessagingまでの責務分離・流れドキュメント化

1. **担当範囲と役割分担**
   - 鳴瀬: Core / Messaging の再設計とコード例作成
   - 詩音: テスト観点整理、失敗系パターンの明文化
   - 迅人: テスト自動生成と進捗ログ更新
   - 鏡花: ドキュメントレビューと差分記録
   - 広夢: リリースノート草案と周知
   - 楠木: 進捗記録およびレポート集約（本報告）

2. **スコープ問題と解決策**
   - Query → MappingManager → KsqlContext → Messaging → Serialization → Kafka の一方向依存を採用【docs/structure/shared/key_value_flow.md】
   - `MappingManager` で POCO と Key/Value の対応を集中管理し、Context と Messaging の責務境界を明確化
   - `key_value_flow_naruse.md` サンプルを基に LINQ クエリから Kafka 送信までの手順を整理
   - 運用指針は `oss_migration_guide.md` に沿って旧属性APIからFluent APIへ移行

3. **ベストプラクティス・アンチパターン**
   - ベストプラクティス: LINQ式で Key を定義し、`AvroSerializer` へ全変換を委譲する
   - アンチパターン: Messaging 層でシリアライズ処理を直接行うこと【docs/namespaces/summary.md】
   - 異常系対応: MappingManager で未登録エンティティは例外化し、DLQ送信手順をMessagingで統一

4. **成果が今後の設計・実装・レビューで活きる点**
   - EntitySet → Messaging の流れを共通ドキュメント化したことで、各層の責務境界が明確になりレビュー指針が統一
   - Fluent API 移行時の手順が `oss_migration_guide.md` に整理されているため、新規実装時の迷いが減少
   - 進捗ログとdiff_logを紐付けることで、設計変更履歴を追跡しやすくなった

本レポートは PM ならびに主要担当へ共有済みです。
