# 差分履歴: ddl_sanitization

🗕 2025-08-10 JST
🧐 作業者: assistant

## 差分タイトル
Sanitize DDL names and throw on registration failure

## 変更理由
- hyphenated topic names caused KSQL syntax errors
- EnsureSimpleEntityDdlAsync only logged failures, leaving registrations incomplete

## 追加・修正内容（反映先: oss_design_combined.md）
- replace '-' with '_' in GenerateCreateStream/CreateTable identifiers
- throw InvalidOperationException when ExecuteStatementAsync reports failure

## 参考文書
- `getting-started.md`
- `docs_advanced_rules.md`
