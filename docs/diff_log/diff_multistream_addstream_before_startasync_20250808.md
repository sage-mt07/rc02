# 差分履歴: multistream_addstream_before_startasync

🗕 2025年8月8日（JST）
🧐 作業者: assistant

## 差分タイトル
AddStream is invoked before KafkaStream.StartAsync

## 変更理由
Ensures MultiStreamizKafkaStreams captures the initial state prior to starting the stream.

## 追加・修正内容（反映先: oss_design_combined.md）
- Create stream without starting and call StartWithRetryAsync only after AddStream.

## 参考文書
- `src/Cache/Extensions/KsqlContextCacheExtensions.cs`

