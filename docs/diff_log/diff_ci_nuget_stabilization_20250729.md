# 差分履歴: ci_nuget_stabilization

🗕 2025-07-29 (JST)
🧐 作業者: 詩音

## 差分タイトル
CI の NuGet キャッシュ追加によるビルド安定化

## 変更理由
- CI で `dotnet restore` が 503 エラーを返すことがあり、復旧に時間がかかっていた
- NuGet フィードへのネットワーク不安定性を緩和するためキャッシュポリシーを導入

## 追加・修正内容（反映先: oss_design_combined.md）
- `.github/workflows/ci.yml` に `actions/cache` を追加し `~/.nuget/packages` を保存
- `DOTNET_SKIP_FIRST_TIME_EXPERIENCE` と `NUGET_PACKAGES` を環境変数として設定

## 参考文書
- docs/troubleshooting.md セクション "CI エラー対策"
