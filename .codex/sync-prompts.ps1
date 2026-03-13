$ErrorActionPreference = "Stop"
$OutputEncoding = [Console]::OutputEncoding = [System.Text.UTF8Encoding]::new()

$projectRoot = Split-Path -Parent $PSScriptRoot
$sourceDir = Join-Path $projectRoot ".codex\prompts"
$targetDir = Join-Path $HOME ".codex\prompts"

if (-not (Test-Path $sourceDir)) {
    throw "未找到项目 prompts 目录: $sourceDir"
}

New-Item -ItemType Directory -Force -Path $targetDir | Out-Null

$sourceFiles = Get-ChildItem -Path $sourceDir -File -Filter *.md
$managedNames = @()

foreach ($file in $sourceFiles) {
    $destination = Join-Path $targetDir $file.Name
    Copy-Item -Path $file.FullName -Destination $destination -Force
    $managedNames += $file.Name
    Write-Host "已同步 $($file.Name) -> $destination"
}

Write-Host ""
Write-Host "已同步 $($managedNames.Count) 个 Codex prompts。"
Write-Host "请重启 Codex 会话后使用 /prompts:<name> 调用。"
