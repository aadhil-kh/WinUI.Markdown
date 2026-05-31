$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$source = Join-Path $root 'node_modules\monaco-editor\min\vs'
$target = Join-Path $root 'Assets\monaco\vs'

if (-not (Test-Path $source)) {
    throw "Monaco source files were not found. Run 'npm install' in samples/WinUI.Markdown.Sample first."
}

if (Test-Path $target) {
    Remove-Item -Path $target -Recurse -Force
}

New-Item -ItemType Directory -Path $target -Force | Out-Null
Copy-Item -Path (Join-Path $source '*') -Destination $target -Recurse -Force

Write-Host "Monaco assets copied to $target"
