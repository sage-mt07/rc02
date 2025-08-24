# ? 設定：置換対象のディレクトリと文字列
$targetPath = "C:\Users\seiji_yfc8940\final\final\docs\claude"
$oldPath = "C:\dev\Refactor\src"
$newPath = "src"

# ? 正規表現用にエスケープ（\ → \\）
$escapedOldPath = [regex]::Escape($oldPath)

# ? 処理対象の拡張子（必要に応じて追加）
$extensions = @("*.md", "*.txt", "*.cs", "*.json", "*.yaml", "*.yml", "*.mhtml")

# ? 拡張子ごとに対象ファイルを検索・置換
$extensions | ForEach-Object {
    Get-ChildItem -Path $targetPath -Recurse -Filter $_ | ForEach-Object {
        $filePath = $_.FullName
        Write-Host "? Scanning: $filePath"

        try {
            $content = Get-Content $filePath -Raw -ErrorAction Stop
            $newContent = $content -replace $escapedOldPath, $newPath

            if ($newContent -ne $content) {
                Set-Content -Path $filePath -Value $newContent -Encoding UTF8
                Write-Host "? Replaced path in: $filePath"
            }
        }
        catch {
            Write-Warning "?? Failed to read or write file: $filePath"
        }
    }
}
