# 差分履歴: windowfilter

🗕 2025-07-19
🧐 作業者: assistant

## 差分タイトル
Window() 拡張でバー集合をウィンドウ幅で絞り込み

## 変更理由
ユーザからの要望で、Set<T>().Window(x) 呼び出し時に指定幅のバーのみ取得できるようにするため。

## 追加・修正内容（反映先: oss_design_combined.md）
- WindowFilteredEntitySet を新規追加し、WindowMinutes でのフィルタ処理を実装
- IEntitySet 拡張 Window(x) を追加
- 単体テスト WindowFilterExtensionsTests を作成

## 参考文書
- docs_advanced_rules.md B.1.2
