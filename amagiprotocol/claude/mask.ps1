# �u���Ώۂ̃f�B���N�g���i�K�v�ɉ����ĕύX�j
$targetPath = "C:\Users\seiji_yfc8940\final\final\docs\claude"  # �� ������ύX

# �u���O�ƒu����̕�����
$oldText = "github/ai_collaboration"
$newText = "github/repository"  # �C�ӂ̒u��������ɕύX�\

# �Ώۃt�@�C���̊g���q�i��: txt, md, cs �Ȃǁj
$extensions = @("*.md", "*.txt", "*.cs", "*.json", "*.yaml", "*.yml","*.mhtml")

# �Ώۃt�@�C�����ċA�I�Ɍ������A�u�����������s
foreach ($ext in $extensions) {
    Get-ChildItem -Path $targetPath -Recurse -Filter $ext -File | ForEach-Object {
        $content = Get-Content $_.FullName  -Encoding UTF8
        if ($content -like "*$oldText*") {
            $content -replace [regex]::Escape($oldText), $newText | Set-Content $_.FullName
            Write-Host "? Replaced in: $($_.FullName)"
        }
    }
}