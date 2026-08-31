[CmdletBinding()]
param(
    [ValidateRange(1, 60)]
    [int]$IntervalMinutes = 5,
    [switch]$Remove
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$taskName = "CatsVsDemons Auto Sync"
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$syncScriptPath = Join-Path $PSScriptRoot "Sync-CatsVsDemons.ps1"
$syncScript = (Resolve-Path -LiteralPath $syncScriptPath).Path

if ($Remove) {
    $existingTask = Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue
    if ($null -ne $existingTask) {
        Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
        Write-Host "Sincronizacao automatica removida."
    }
    else {
        Write-Host "A sincronizacao automatica ja estava removida."
    }
    exit 0
}

function Invoke-GitConfig {
    param([Parameter(Mandatory = $true)][string[]]$Arguments)

    $output = @(& git -C $repoRoot @Arguments 2>&1)
    if ($LASTEXITCODE -ne 0) {
        throw "git $($Arguments -join ' ') falhou: $($output -join ' ')"
    }
}

if ($null -eq (Get-Command git -ErrorAction SilentlyContinue)) {
    throw "Git nao encontrado. Instale o Git for Windows antes de continuar."
}

Invoke-GitConfig -Arguments @("config", "--local", "pull.rebase", "true")
Invoke-GitConfig -Arguments @("config", "--local", "rebase.autoStash", "true")
Invoke-GitConfig -Arguments @("config", "--local", "fetch.prune", "true")
Invoke-GitConfig -Arguments @("config", "--local", "core.autocrlf", "false")
Invoke-GitConfig -Arguments @("branch", "--set-upstream-to=origin/main", "main")

$authorName = @(& git -C $repoRoot config user.name 2>$null)
if ([string]::IsNullOrWhiteSpace(($authorName -join ""))) {
    Invoke-GitConfig -Arguments @(
        "config", "--local", "user.name", "Ariel Kuhn Quint")
}

$authorEmail = @(& git -C $repoRoot config user.email 2>$null)
if ([string]::IsNullOrWhiteSpace(($authorEmail -join ""))) {
    Invoke-GitConfig -Arguments @(
        "config", "--local", "user.email",
        "267419390+ArielKuhnQuint88@users.noreply.github.com")
}

if ($null -ne (Get-Command git-lfs -ErrorAction SilentlyContinue)) {
    & git -C $repoRoot lfs install --local | Out-Null
}

$unityEditorRoot = Join-Path $env:ProgramFiles "Unity\Hub\Editor"
$unitySearchParameters = @{
    Path = $unityEditorRoot
    Recurse = $true
    Filter = "UnityYAMLMerge.exe"
    ErrorAction = "SilentlyContinue"
}
$unityMerge = Get-ChildItem @unitySearchParameters |
    Sort-Object FullName -Descending |
    Select-Object -First 1

if ($null -ne $unityMerge) {
    $mergeExecutable = $unityMerge.FullName.Replace("\", "/")
    $mergeDriver = '"{0}" merge -p %O %B %A %A' -f $mergeExecutable
    Invoke-GitConfig -Arguments @(
        "config", "--local", "merge.unityyamlmerge.name", "Unity SmartMerge")
    Invoke-GitConfig -Arguments @(
        "config", "--local", "merge.unityyamlmerge.driver", $mergeDriver)
    Invoke-GitConfig -Arguments @(
        "config", "--local", "merge.unityyamlmerge.recursive", "binary")

    $gitDirectory = @(& git -C $repoRoot rev-parse --git-dir)[0].ToString().Trim()
    if (-not [System.IO.Path]::IsPathRooted($gitDirectory)) {
        $gitDirectory = Join-Path $repoRoot $gitDirectory
    }
    $infoDirectory = Join-Path $gitDirectory "info"
    $attributesPath = Join-Path $infoDirectory "attributes"
    New-Item -Path $infoDirectory -ItemType Directory -Force | Out-Null
    $marker = "# CatsVsDemons Unity SmartMerge"
    $existingAttributes = if (Test-Path $attributesPath) {
        Get-Content -LiteralPath $attributesPath -Raw
    }
    else {
        ""
    }
    if ($existingAttributes -notlike "*$marker*") {
        @(
            $marker,
            "*.meta merge=unityyamlmerge",
            "*.unity merge=unityyamlmerge",
            "*.prefab merge=unityyamlmerge",
            "*.asset merge=unityyamlmerge",
            "*.mat merge=unityyamlmerge",
            "*.anim merge=unityyamlmerge",
            "*.controller merge=unityyamlmerge"
        ) | Add-Content -LiteralPath $attributesPath -Encoding ASCII
    }
}
else {
    Write-Warning "UnityYAMLMerge nao foi encontrado. O restante da sincronizacao funcionara normalmente."
}

$powershellArguments = (
    '-NoProfile -NonInteractive -ExecutionPolicy Bypass ' +
    ('-WindowStyle Hidden -File "{0}"' -f $syncScript)
)
$actionParameters = @{
    Execute = "powershell.exe"
    Argument = $powershellArguments
}
$action = New-ScheduledTaskAction @actionParameters
$triggerParameters = @{
    Once = $true
    At = (Get-Date).AddMinutes(1)
    RepetitionInterval = (New-TimeSpan -Minutes $IntervalMinutes)
    RepetitionDuration = (New-TimeSpan -Days 3650)
}
$trigger = New-ScheduledTaskTrigger @triggerParameters
$settingsParameters = @{
    StartWhenAvailable = $true
    AllowStartIfOnBatteries = $true
    DontStopIfGoingOnBatteries = $true
    MultipleInstances = "IgnoreNew"
}
$settings = New-ScheduledTaskSettingsSet @settingsParameters
$taskParameters = @{
    TaskName = $taskName
    Action = $action
    Trigger = $trigger
    Settings = $settings
    Description = "Commit, pull e push automaticos do Cats Vs Demons."
    Force = $true
}
Register-ScheduledTask @taskParameters | Out-Null

Write-Host "Sincronizacao automatica instalada."
Write-Host "Intervalo: $IntervalMinutes minuto(s)."
Write-Host "O sincronizador espera o Unity fechar antes de versionar arquivos."
Write-Host "Log: .git\catsvsdemons-autosync.log"

& $syncScript
