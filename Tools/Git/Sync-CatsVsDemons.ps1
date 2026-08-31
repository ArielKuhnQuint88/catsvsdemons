[CmdletBinding()]
param(
    [switch]$Force,
    [switch]$NoPush
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$gitDirectoryValue = @(& git -C $repoRoot rev-parse --git-dir 2>&1)
if ($LASTEXITCODE -ne 0) {
    throw "Nao foi possivel localizar o repositorio Git."
}

$gitDirectory = ($gitDirectoryValue | Select-Object -First 1).ToString().Trim()
if (-not [System.IO.Path]::IsPathRooted($gitDirectory)) {
    $gitDirectory = Join-Path $repoRoot $gitDirectory
}

$logPath = Join-Path $gitDirectory "catsvsdemons-autosync.log"
$mutex = [System.Threading.Mutex]::new(
    $false, "Local\CatsVsDemonsGitSync")
$hasLock = $false

function Write-SyncLog {
    param([Parameter(Mandatory = $true)][string]$Message)

    $line = "{0} {1}" -f (Get-Date -Format "yyyy-MM-dd HH:mm:ss"), $Message
    Add-Content -LiteralPath $logPath -Value $line -Encoding UTF8
    Write-Host $line
}

function Invoke-Git {
    param(
        [Parameter(Mandatory = $true)][string[]]$Arguments,
        [switch]$Quiet
    )

    $output = @(& git -C $repoRoot @Arguments 2>&1)
    $exitCode = $LASTEXITCODE
    if (-not $Quiet) {
        foreach ($line in $output) {
            if (-not [string]::IsNullOrWhiteSpace($line.ToString())) {
                Write-SyncLog ("git: {0}" -f $line)
            }
        }
    }
    if ($exitCode -ne 0) {
        throw "git $($Arguments -join ' ') falhou (codigo $exitCode): $($output -join ' ')"
    }
    return $output
}

function Test-RebaseInProgress {
    $mergeRebase = Test-Path (Join-Path $gitDirectory "rebase-merge")
    $applyRebase = Test-Path (Join-Path $gitDirectory "rebase-apply")
    return $mergeRebase -or $applyRebase
}

try {
    try {
        $hasLock = $mutex.WaitOne(0, $false)
    }
    catch [System.Threading.AbandonedMutexException] {
        $hasLock = $true
    }

    if (-not $hasLock) {
        exit 0
    }

    if ((Test-Path $logPath) -and
        (Get-Item $logPath).Length -gt 2MB) {
        $recentLog = @(Get-Content -LiteralPath $logPath -Tail 500)
        Set-Content -LiteralPath $logPath -Value $recentLog -Encoding UTF8
    }

    if (-not $Force -and
        @(Get-Process -Name "Unity" -ErrorAction SilentlyContinue).Count -gt 0) {
        Write-SyncLog "Unity esta aberto; sincronizacao adiada para proteger os arquivos."
        exit 0
    }

    foreach ($stateName in @("MERGE_HEAD", "CHERRY_PICK_HEAD")) {
        if (Test-Path (Join-Path $gitDirectory $stateName)) {
            throw "Existe uma operacao Git incompleta ($stateName). Resolva-a antes da sincronizacao."
        }
    }
    if (Test-RebaseInProgress) {
        throw "Existe um rebase incompleto. Resolva-o antes da sincronizacao."
    }

    $branch = @(Invoke-Git -Arguments @("branch", "--show-current") -Quiet |
        Select-Object -First 1)
    if ($branch.Count -eq 0 -or $branch[0].ToString().Trim() -ne "main") {
        throw "A sincronizacao automatica funciona somente na branch main."
    }

    Write-SyncLog "Sincronizacao iniciada."

    $changes = @(Invoke-Git -Arguments @(
            "status", "--porcelain=v1", "--untracked-files=all") -Quiet)
    if (-not [string]::IsNullOrWhiteSpace(($changes -join ""))) {
        $null = Invoke-Git -Arguments @("add", "-A")

        & git -C $repoRoot diff --cached --quiet
        $diffExitCode = $LASTEXITCODE
        if ($diffExitCode -eq 1) {
            $message = "Sincronizacao automatica: {0}" -f (Get-Date -Format
                "yyyy-MM-dd HH:mm:ss")
            $null = Invoke-Git -Arguments @("commit", "-m", $message)
        }
        elseif ($diffExitCode -gt 1) {
            throw "Nao foi possivel verificar as alteracoes preparadas."
        }
    }

    $null = Invoke-Git -Arguments @("fetch", "--prune", "origin", "main")
    try {
        $null = Invoke-Git -Arguments @("pull", "--rebase", "origin", "main")
    }
    catch {
        if (Test-RebaseInProgress) {
            & git -C $repoRoot rebase --abort 2>&1 | Out-Null
        }
        throw "Conflito ao baixar o GitHub. Os commits locais foram preservados. $($_.Exception.Message)"
    }

    if (-not $NoPush) {
        try {
            $null = Invoke-Git -Arguments @("push", "origin", "main")
        }
        catch {
            Write-SyncLog "O GitHub mudou durante o envio; tentando novamente."
            $null = Invoke-Git -Arguments @("pull", "--rebase", "origin", "main")
            $null = Invoke-Git -Arguments @("push", "origin", "main")
        }
    }

    Write-SyncLog "Sincronizacao concluida."
}
catch {
    if ($hasLock) {
        Write-SyncLog ("ERRO: {0}" -f $_.Exception.Message)
    }
    Write-Error $_.Exception.Message
    exit 1
}
finally {
    if ($hasLock) {
        $mutex.ReleaseMutex()
    }
    $mutex.Dispose()
}
