param(
  [string]$Solution = "Kafka.Ksql.Linq.sln",
  [string]$Results = "reports\physical"
)

$ErrorActionPreference = "Stop"
New-Item -ItemType Directory -Force -Path $Results | Out-Null

dotnet test $Solution `
  -c Release `
  --filter "Category=Physical" `
  --logger "trx;LogFileName=physical.trx" `
  --results-directory $Results

# 追加で xUnit の xml や cobertura が必要なら logger を増やす
