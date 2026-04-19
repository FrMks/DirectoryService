param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$skillSource = Join-Path $repoRoot "tools\codex\skills\karpathy-guidelines"
$codexRoot = Join-Path $env:USERPROFILE ".codex"
$codexSkillsRoot = Join-Path $codexRoot "skills"
$skillTarget = Join-Path $codexSkillsRoot "karpathy-guidelines"
$codexConfigPath = Join-Path $codexRoot "config.toml"
$serenaRoot = Join-Path $env:USERPROFILE ".serena"
$serenaExe = Join-Path $env:USERPROFILE ".local\bin\serena.exe"
$uvExe = Join-Path $env:APPDATA "Python\Python312\Scripts\uv.exe"
$npxCmd = "C:\Program Files\nodejs\npx.cmd"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Ensure-ConfigLineBlock([string]$Path, [string]$Header, [string]$Block) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType File -Path $Path -Force | Out-Null
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -notmatch [regex]::Escape($Header)) {
        if ($content.Length -gt 0 -and -not $content.EndsWith("`n")) {
            $content += "`r`n"
        }
        $content += "`r`n" + $Block.Trim() + "`r`n"
        $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
        [System.IO.File]::WriteAllText($Path, $content, $utf8NoBom)
    }
}

Ensure-Directory $codexRoot
Ensure-Directory $codexSkillsRoot
Ensure-Directory $serenaRoot
Ensure-Directory (Join-Path $serenaRoot "memories\global")

if (Test-Path -LiteralPath $skillTarget) {
    Remove-Item -LiteralPath $skillTarget -Recurse -Force
}

Copy-Item -LiteralPath $skillSource -Destination $skillTarget -Recurse -Force

$context7Block = @'
[mcp_servers.context7]
command = "C:\\Program Files\\nodejs\\npx.cmd"
args = ["-y", "@upstash/context7-mcp"]
startup_timeout_ms = 40000
'@

Ensure-ConfigLineBlock -Path $codexConfigPath -Header "[mcp_servers.context7]" -Block $context7Block

if (-not (Test-Path -LiteralPath $uvExe)) {
    python -m pip install --user uv
}

if (-not (Test-Path -LiteralPath $serenaExe)) {
    & $uvExe tool install -p 3.12 serena-agent@latest --prerelease=allow
}

if (-not (Test-Path -LiteralPath (Join-Path $serenaRoot "serena_config.yml"))) {
    & $serenaExe init -b LSP
}

$serenaBlock = @'
[mcp_servers.serena]
startup_timeout_sec = 15
command = "USER_SERENA_EXE"
args = ["start-mcp-server", "--project-from-cwd", "--context=codex"]
'@.Replace("USER_SERENA_EXE", $serenaExe.Replace("\", "\\"))

Ensure-ConfigLineBlock -Path $codexConfigPath -Header "[mcp_servers.serena]" -Block $serenaBlock

Write-Host ""
Write-Host "Codex setup complete for this repository."
Write-Host "Installed skill: $skillTarget"
Write-Host "Updated config: $codexConfigPath"
Write-Host "Restart Codex to load the new MCP configuration."
