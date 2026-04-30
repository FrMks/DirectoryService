param()

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$skillSource = Join-Path $repoRoot "tools\codex\skills\karpathy-guidelines"
$codexRoot = Join-Path $env:USERPROFILE ".codex"
$codexSkillsRoot = Join-Path $codexRoot "skills"
$skillTarget = Join-Path $codexSkillsRoot "karpathy-guidelines"
$codexConfigPath = Join-Path $codexRoot "config.toml"
$serenaRoot = Join-Path $env:USERPROFILE ".serena"
$serenaProjectConfigPath = Join-Path $repoRoot ".serena\project.yml"

function Ensure-Directory([string]$Path) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType Directory -Path $Path -Force | Out-Null
    }
}

function Find-CommandPath([string]$Name, [string[]]$FallbackPaths) {
    $command = Get-Command $Name -ErrorAction SilentlyContinue
    if ($command) {
        return $command.Source
    }

    foreach ($path in $FallbackPaths) {
        if (Test-Path -LiteralPath $path) {
            return $path
        }
    }

    return $null
}

function Find-UsablePythonPath {
    $candidates = @()
    $pythonCommand = Get-Command python -ErrorAction SilentlyContinue
    if ($pythonCommand) {
        $candidates += $pythonCommand.Source
    }

    $candidates += @(
        (Join-Path $env:LOCALAPPDATA "Programs\Python\Python313\python.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Python\Python312\python.exe"),
        (Join-Path $env:LOCALAPPDATA "Programs\Python\Python311\python.exe"),
        "C:\Program Files\Python313\python.exe",
        "C:\Program Files\Python312\python.exe",
        "C:\Program Files\Python311\python.exe"
    )

    foreach ($candidate in ($candidates | Select-Object -Unique)) {
        if (-not $candidate -or -not (Test-Path -LiteralPath $candidate)) {
            continue
        }

        try {
            $version = & $candidate --version 2>&1
            if ($LASTEXITCODE -eq 0 -and "$version" -match "^Python 3\.") {
                return $candidate
            }
        }
        catch {
            continue
        }
    }

    return $null
}

function Set-ConfigLineBlock([string]$Path, [string]$Header, [string]$Block) {
    if (-not (Test-Path -LiteralPath $Path)) {
        New-Item -ItemType File -Path $Path -Force | Out-Null
    }

    $content = Get-Content -LiteralPath $Path -Raw
    $normalizedBlock = $Block.Trim() + "`r`n"
    $headerPattern = [regex]::Escape($Header)
    $sectionPattern = "(?ms)^$headerPattern\r?\n.*?(?=^\[|\z)"

    if ($content -match $sectionPattern) {
        $content = [regex]::Replace($content, $sectionPattern, $normalizedBlock)
    }
    else {
        if ($content.Length -gt 0 -and -not $content.EndsWith("`n")) {
            $content += "`r`n"
        }
        $content += "`r`n" + $normalizedBlock
    }

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $content, $utf8NoBom)
}

function ConvertTo-TomlString([string]$Value) {
    return $Value.Replace("\", "\\").Replace('"', '\"')
}

function Ensure-SerenaLanguage([string]$Path, [string]$Language) {
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ($content -match "(?m)^-\s*$([regex]::Escape($Language))\s*$") {
        return
    }

    $content = [regex]::Replace(
        $content,
        "(?m)^languages:\r?\n((?:-\s*.+\r?\n)+)",
        "languages:`r`n`$1- $Language`r`n",
        1
    )

    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($Path, $content, $utf8NoBom)
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

Set-ConfigLineBlock -Path $codexConfigPath -Header "[mcp_servers.context7]" -Block $context7Block

$uvFallbackPaths = @(
    (Join-Path $env:USERPROFILE ".local\bin\uv.exe"),
    (Join-Path $env:USERPROFILE ".cargo\bin\uv.exe"),
    (Join-Path $env:APPDATA "Python\Python312\Scripts\uv.exe"),
    (Join-Path $env:APPDATA "Python\Python311\Scripts\uv.exe"),
    (Join-Path $env:APPDATA "Python\Python310\Scripts\uv.exe")
)
$uvExe = Find-CommandPath -Name "uv" -FallbackPaths $uvFallbackPaths

if (-not $uvExe) {
    $pythonExe = Find-UsablePythonPath
    if ($pythonExe) {
        & $pythonExe -m pip install --user uv
    }
    else {
        powershell -ExecutionPolicy Bypass -NoProfile -Command "irm https://astral.sh/uv/install.ps1 | iex"
    }

    $uvExe = Find-CommandPath -Name "uv" -FallbackPaths $uvFallbackPaths
}

if (-not $uvExe -or -not (Test-Path -LiteralPath $uvExe)) {
    throw "uv was installed, but setup could not find uv.exe. Restart PowerShell or add uv to PATH, then run this script again."
}

$serenaFallbackPaths = @(
    (Join-Path $env:USERPROFILE ".local\bin\serena.exe")
)
$serenaExe = Find-CommandPath -Name "serena" -FallbackPaths $serenaFallbackPaths

if (-not $serenaExe) {
    & $uvExe tool install -p 3.12 serena-agent@latest --prerelease=allow
    $serenaExe = Find-CommandPath -Name "serena" -FallbackPaths $serenaFallbackPaths
}

if (-not $serenaExe -or -not (Test-Path -LiteralPath $serenaExe)) {
    throw "Serena was installed, but setup could not find serena.exe. Restart PowerShell or check uv tool installation output, then run this script again."
}

if (-not (Test-Path -LiteralPath (Join-Path $serenaRoot "serena_config.yml"))) {
    & $serenaExe init -b LSP
}

Ensure-SerenaLanguage -Path $serenaProjectConfigPath -Language "typescript"

$serenaBlockTemplate = @'
[mcp_servers.serena]
startup_timeout_sec = 60
command = "USER_SERENA_EXE"
args = ["start-mcp-server", "--project", "USER_REPO_ROOT", "--context=codex", "--open-web-dashboard", "false"]
'@

$serenaBlock = $serenaBlockTemplate.
    Replace("USER_SERENA_EXE", (ConvertTo-TomlString $serenaExe)).
    Replace("USER_REPO_ROOT", (ConvertTo-TomlString $repoRoot))

Set-ConfigLineBlock -Path $codexConfigPath -Header "[mcp_servers.serena]" -Block $serenaBlock

Write-Host ""
Write-Host "Codex setup complete for this repository."
Write-Host "Installed skill: $skillTarget"
Write-Host "Updated config: $codexConfigPath"
Write-Host "Restart Codex to load the new MCP configuration."
