# ? �ݒ�F�u���Ώۂ̃f�B���N�g���ƕ�����
$targetPath = "C:\Users\seiji_yfc8940\final\final\docs\claude"
$oldPath = "C:\dev\Refactor\src"
$newPath = "src"

# ? ���K�\���p�ɃG�X�P�[�v�i\ �� \\�j
$escapedOldPath = [regex]::Escape($oldPath)

# ? �����Ώۂ̊g���q�i�K�v�ɉ����Ēǉ��j
$extensions = @("*.md", "*.txt", "*.cs", "*.json", "*.yaml", "*.yml", "*.mhtml")

# ? �g���q���ƂɑΏۃt�@�C���������E�u��
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
