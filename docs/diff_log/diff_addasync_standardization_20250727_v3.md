# 差分履歴: addasync_standardization (レビュー対応)

🗕 2025年7月27日（JST）
🧐 作業者: naruse

## 差分タイトル
AddAsync 呼び出しパラメーター修正

## 変更理由
- サンプルおよびテストで `AddAsync(value)` としていた部分が誤りだったため
- 正しくはエンティティ本体を渡す仕様のため修正

## 追加・修正内容（反映先: oss_design_combined.md）
- 各ドキュメントのコード例を `AddAsync(entity)` へ更新
- `AutomaticQueryFlowTests` の呼び出しを修正

## 参考文書
- `docs_advanced_rules.md` テスト方針セクション
