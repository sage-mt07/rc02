# 置換対象のディレクトリ（必要に応じて変更）
$targetPath = "C:\Users\seiji_yfc8940\final\final\docs\claude"  # ← ここを変更

# 置換前と置換後の文字列
$oldText = "github/ai_collaboration"
$newText = "github/repository"  # 任意の置換文字列に変更可能

# 対象ファイルの拡張子（例: txt, md, cs など）
$extensions = @("*.md", "*.txt", "*.cs", "*.json", "*.yaml", "*.yml","*.mhtml")

# 対象ファイルを再帰的に検索し、置換処理を実行
foreach ($ext in $extensions) {
    Get-ChildItem -Path $targetPath -Recurse -Filter $ext -File | ForEach-Object {
        $content = Get-Content $_.FullName  -Encoding UTF8
        if ($content -like "*$oldText*") {
            $content -replace [regex]::Escape($oldText), $newText | Set-Content $_.FullName
            Write-Host "? Replaced in: $($_.FullName)"
        }
    }
}