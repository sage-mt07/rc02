# diff: test_adjustments (2025-09-13)

目的: ログポリシー変更 (ksql コマンドを Information で出力) に伴う既存UTの期待値ズレを解消。

変更概要:
- EnsureQueryEntityDdlAsyncTests: ログ件数の評価を `"ksql execute:"` を除外したフィルタで実施するよう修正。
- KafkaConsumerAutoCommitTests: EnableAutoCommit の優先度ポリシーに合わせ期待値を更新（トピック設定優先で true）。
- ToQueryEndToEndTests: 生成DDLの比較で改行差分 (CRLF/LF) を正規化して比較するよう修正。
- KafkaConsumerMappingErrorTests: 参照名前空間の不足によりビルド失敗していた箇所に `using` を追加。

影響範囲:
- テストコードのみ。プロダクションコードのロジック変更なし。

補足:
- 追加提案として、KafkaConsumerManager のマッピング例外時の Error ログ本文検証 (Topic/Partition/Offset/ErrorType/Message を含む) のテスト追加が可能。必要であれば追って実装する。

