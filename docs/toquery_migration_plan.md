# ToQuery導入に伴う機能変更の進め方

## 1. 🎯 目的（What & Why）

### 目的：
Kafka DSL において、LINQスタイルで KSQL クエリ（View）を定義可能にする `ToQuery()` 機能を導入する。

### 背景と狙い：
- Entity Framework に近い DSL 操作感の提供
- View（SELECTベースのStream/Table）を型安全に定義・登録できる構造の整備
- クエリ構造とCREATE文の責務分離、旧DSL構造の整理

---

## 2. 🧭 全体の進め方（ステップ構成）

### Step 1️⃣：ToQuery構文と中間モデル（KsqlQueryModel）構築
- DSLチェーン（From → Where → Select）の構造確定
- Window構文（Tumblingなど）にも対応

### Step 2️⃣：EventSet に ToQuery 対応（HasQuery）＋ Key/Value 構造チェック導入
- EventSet が View 定義にも対応
- Key/Value チェックに失敗した場合は即例外スロー

### Step 3️⃣：KsqlContext における View/Stream/TABLE 発行切替対応
- 登録された内容に応じて CREATE 文 or View 文を出し分け
- OnModelCreating 内の構成を統一

### Step 4️⃣：旧DSL構造の削除・整理
- FromXxx / JoinXxx などの旧Builder群を削除
- 古いテストの移行または廃止

---

## 3. 👥 メンバー間の責任と確認点

| 担当 | 役割 |
|------|------|
| 司令（人間） | 設計方針決定、責務確認、削除判断 |
| 鳴瀬 | ToQuery・EventSet の実装と整理 |
| Codex | DSL拡張やKSQL文生成テスト対応 |
| 鏡花 | 削除対象の妥当性チェック、責務逸脱の検出 |
| 天城 | 進行管理、記録整備、共有文書の提示 |

---

## 4. ✅ 進行中のルール

- Key/Value構造チェックは EventSet に責任を持たせ、エラー時は即終了。
- KsqlContext は構造チェック後に発行を担当。責務は EventSet → Context へ明確に移譲。
- 機能追加が完了したら、不要コードの削除までを含めて「実装完了」とする。

