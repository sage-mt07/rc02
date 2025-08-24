param(
  [string]$ComposeFile = "$(Split-Path $PSCommandPath)\docker-compose.yaml"
)

$ErrorActionPreference = "Stop"
Write-Host "[up] using compose: $ComposeFile"

docker compose -f $ComposeFile up -d

# ここで必要ならヘルスチェック待機（例）
# Start-Sleep -Seconds 5
# TODO: ksqlDB, Kafka のヘルスエンドポイントを叩いてリトライ待機を入れる
