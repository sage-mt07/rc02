# Getting Started

このプロジェクトをすぐに試すための最短ルートです。詳細は各ドキュメントへのリンクを辿ってください。

## セットアップ
- 前提: .NET 8 SDK, Docker が利用可能
- リポジトリ取得と初期化:
  - `git clone <repository-url>`
  - `cd rc02`
  - `dotnet restore`
  - `docker-compose -f tools/docker-compose.kafka.yml up -d`

## 最初のサンプル実行
- `cd examples/hello-world`
- `dotnet run`
- 参考: `docs/examples_reference.md`

## 次の一歩
- 開発者向けガイド: `docs/dev_guide.md`
- API リファレンス: `docs/api_reference.md`
- 設定リファレンス: `docs/docs_configuration_reference.md`
- トラブル対応: `docs/troubleshooting.md`

## 物理環境での最小検証
- `docs/physical_test_minimum.md` を参照

