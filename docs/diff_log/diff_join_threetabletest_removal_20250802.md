# 差分履歴: join

🗕 2025-08-02 10:48 JST
🧐 作業者: 詩音

## 三テーブルJOINテストの削除
3テーブルJOINはサポート外のため、該当テストを削除。

## 変更理由
JOINは最大2テーブルまでサポートされる仕様に合わせるため。

## 追加・修正内容（反映先: oss_design_combined.md）
- `physicalTests/OssSamples/JoinIntegrationTests.cs` から `ThreeTableJoin_Query_ShouldBeValid` テストを削除。

## 参考文書
- なし
