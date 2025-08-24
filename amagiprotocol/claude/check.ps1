# 置換対象のディレクトリ（必要に応じて変更）
$targetPath = "C:\Users\seiji_yfc8940\final\final\docs\claude"  # ← ここを変更

# 置換前と置換後の文字列
$oldText = "seiji_yfc8940"

# 対象ファイルの拡張子（例: txt, md, cs など）
$extensions = @("*.md", "*.txt", "*.cs", "*.json", "*.yaml", "*.yml","*.mhtml")

# 対象ファイルを再帰的に検索し、置換処理を実行
foreach ($ext in $extensions) {
    Get-ChildItem -Path $targetPath -Recurse -Filter $ext -File | ForEach-Object {
        $content = Get-Content $_.FullName -Raw
        if ($content -like "*$oldText*") {
            Write-Host "? checked in: $($_.FullName)"
        }
    }
}
