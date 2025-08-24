# 進捗管理ガイド

## 記録ルール
進捗・設計・課題は日付別・時系列で記録します。日時はOSから取得したJST時刻を用い、タイムゾーンを明記します。

### 保存場所
作業報告は `docs/changes/` に `YYYYMMDD_progress.md` を日ごとに作成し、時系列で追記します。

### 取得例
- Windows: `echo %date% %time%`
- Linux/Mac: `date '+%Y-%m-%d %H:%M:%S %Z'`
- C#: `DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " JST"`
- Python: `datetime.now(timezone(timedelta(hours=9))).strftime('%Y-%m-%d %H:%M:%S JST')`

### フォーマット
```markdown
## YYYY-MM-DD HH:mm JST [担当名]
進捗や議事要旨
- 箇条書きで具体的な作業・判断・相談・次アクション
- 関連ファイル・参照資料もあれば明記
- 特記事項や背景も必要に応じて
```

### 記入例
```markdown
## 2025-07-11 21:20 JST [naruse]
EntityBuilder実装のPRを開始。削除対象の属性クラス棚卸し中。
- KsqlStreamAttribute, TopicAttribute など依存コード洗い出し進行
- 削除対象の一覧と依存箇所マッピングを進行中
```

---

## 運用補足・改定履歴
2025-07-12 PM指示・codex案の採用
- 進捗ログ（docs/changes/）運用の明確化
- diff_log（docs/diff_log/）の記録ルール統一
- features/{機能名}/ディレクトリの作業・管理ルール
- ドキュメント・テストの同期運用
- “わからない”即共有・証跡文化の強調
