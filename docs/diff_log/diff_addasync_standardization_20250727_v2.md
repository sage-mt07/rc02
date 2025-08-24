# 差分履歴: addasync_standardization (追記)

🗕 2025年7月27日（JST）
🧐 作業者: naruse

## 差分タイトル
AutomaticQueryFlowTests に key/value それぞれの検証を追加

## 変更理由
- StubProducer の送信確認のみでは不十分だったため、MappingManager から取得した key と value を個別に評価するテストを追加

## 追加・修正内容（反映先: oss_design_combined.md）
- `AutomaticQueryFlowTests` に `Assert.Equal(user.Id, key)` と `Assert.Same(user, value)` を追記

## 参考文書
- `docs_advanced_rules.md` テスト方針セクション
