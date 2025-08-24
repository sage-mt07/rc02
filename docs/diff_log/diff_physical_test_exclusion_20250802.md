# 差分履歴: physical_test_exclusion

🗕 2025-08-02
🧐 作業者: assistant

## 差分タイトル
Mark physical tests for exclusion

## 変更理由
Kafka and external middleware-dependent tests were failing in the Codex environment. Categorizing them allows unit tests to run without requiring external services.

## 追加・修正内容（反映先: oss_design_combined.md）
- Introduced a reusable `CategoryAttribute` for xUnit tests.
- Tagged Kafka-dependent tests and physical test assembly with `Category("PhysicalTest")`.
- Updated physical test project to include the shared attribute and marked it at the assembly level.

## 参考文書
- None
