[CmdletBinding()]
param(
    [string]$UnityPath,
    [string]$KeystorePath,
    [string]$KeyAlias = "catsvsdemons",
    [int]$LastUploadedVersionCode = -1
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Get-NextPlayVersionCode {
    param([int]$ProjectCode, [int]$LastUploaded)
    if ($ProjectCode -lt 1 -or $ProjectCode -gt 2100000000 -or
        $LastUploaded -lt 0 -or $LastUploaded -ge 2100000000) {
        throw "Informe o maior codigo ja enviado ao Play Console (0 se nunca enviou)."
    }
    return [Math]::Max($ProjectCode, $LastUploaded + 1)
}

function Assert-PlayBuildReceipt {
    param([string]$BundlePath, [int]$VersionCode)
    $receiptPath = [IO.Path]::ChangeExtension($BundlePath, ".build.json")
    if (-not (Test-Path -LiteralPath $BundlePath -PathType Leaf) -or
        -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "A Unity nao entregou o AAB e seu relatorio de verificacao."
    }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $hash = (Get-FileHash -LiteralPath $BundlePath -Algorithm SHA256).Hash
    if ($receipt.file -ne [IO.Path]::GetFileName($BundlePath) -or
        $receipt.package -ne "com.qipgames.catsvsdemons" -or
        $receipt.versionCode -ne $VersionCode -or $receipt.targetApi -lt 36 -or
        $receipt.architecture -ne "ARM64 / IL2CPP" -or
        $receipt.nativeValidation -notmatch "ELF64 ARM64" -or
        $receipt.sha256 -ne $hash) {
        throw "O AAB nao corresponde ao relatorio de build esperado."
    }
}

function Read-BuildPassword {
    param([string]$Prompt)
    $secure = Read-Host $Prompt -AsSecureString
    $pointer = [Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure)
    try { return [Runtime.InteropServices.Marshal]::PtrToStringBSTR($pointer) }
    finally { [Runtime.InteropServices.Marshal]::ZeroFreeBSTR($pointer); $secure.Dispose() }
}

function Invoke-PlayConsoleExport {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $versionFile = Join-Path $projectRoot "ProjectSettings\ProjectVersion.txt"
    $settingsFile = Join-Path $projectRoot "ProjectSettings\ProjectSettings.asset"
    $versionMatch = [regex]::Match((Get-Content $versionFile -Raw), "(?m)^m_EditorVersion:\s*(\S+)")
    if (-not $versionMatch.Success) { throw "Versao da Unity ausente no projeto." }
    $editorVersion = $versionMatch.Groups[1].Value
    $settings = Get-Content $settingsFile -Raw
    $codeMatch = [regex]::Match($settings, "(?m)^\s*AndroidBundleVersionCode:\s*(\d+)")
    $releaseMatch = [regex]::Match($settings, "(?m)^\s*bundleVersion:\s*(\S+)")
    if (-not $codeMatch.Success -or -not $releaseMatch.Success) { throw "Configuracao Android incompleta." }

    $editor = $UnityPath
    if ([string]::IsNullOrWhiteSpace($editor)) {
        $roots = @($env:ProgramFiles, ${env:ProgramFiles(x86)}, $env:LOCALAPPDATA)
        foreach ($root in $roots) {
            if ([string]::IsNullOrWhiteSpace($root)) { continue }
            $candidate = Join-Path $root "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $editor = $candidate; break }
        }
    }
    if ([string]::IsNullOrWhiteSpace($editor) -or -not (Test-Path -LiteralPath $editor -PathType Leaf)) {
        throw "Instale Unity $editorVersion no Unity Hub, ou informe -UnityPath com o caminho de Unity.exe."
    }
    $androidFolder = Join-Path (Split-Path $editor -Parent) "Data\PlaybackEngines\AndroidPlayer"
    $jarSigner = Join-Path $androidFolder "OpenJDK\bin\jarsigner.exe"
    if (-not (Test-Path -LiteralPath $jarSigner -PathType Leaf)) {
        throw "Instale Android Build Support, Android SDK & NDK Tools e OpenJDK no Unity Hub."
    }
    $lockPath = Join-Path $projectRoot "Temp\UnityLockfile"
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $lockProbe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $lockProbe.Dispose()
        }
        catch { throw "Feche a Unity deste projeto antes de exportar." }
    }

    $lastCode = $LastUploadedVersionCode
    if ($lastCode -lt 0) {
        $answer = Read-Host "Maior Bundle Version Code ja enviado ao Play Console (0 se nunca enviou)"
        if (-not [int]::TryParse($answer, [ref]$lastCode)) { throw "O codigo precisa ser um numero inteiro." }
    }
    $buildCode = Get-NextPlayVersionCode -ProjectCode ([int]$codeMatch.Groups[1].Value) -LastUploaded $lastCode
    $releaseVersion = $releaseMatch.Groups[1].Value.Trim([char[]]@([char]34, [char]39))
    $safeVersion = -join ($releaseVersion.ToCharArray() | ForEach-Object {
        if ([char]::IsLetterOrDigit($_) -or $_ -eq '-') { $_ } else { '-' }
    })
    $buildFolder = Join-Path $projectRoot "Builds\CatsVsDemons-PlayStore"
    $bundlePath = Join-Path $buildFolder "CatsVsDemons-v$safeVersion-$buildCode.aab"
    if (Test-Path -LiteralPath $bundlePath) {
        throw "Ja existe um AAB com codigo $buildCode. Preserve-o e escolha um codigo novo antes de exportar."
    }
    $keyFile = $KeystorePath
    if ([string]::IsNullOrWhiteSpace($keyFile)) {
        $defaultKey = Join-Path ([Environment]::GetFolderPath('MyDocuments')) "Chaves\catsvsdemons-upload.keystore"
        if (Test-Path -LiteralPath $defaultKey -PathType Leaf) { $keyFile = $defaultKey }
        else {
            Add-Type -AssemblyName System.Windows.Forms
            $picker = New-Object System.Windows.Forms.OpenFileDialog
            try {
                $picker.Title = "Selecione a chave de upload JA usada no Play Console"
                $picker.Filter = "Chave de upload (*.keystore;*.jks)|*.keystore;*.jks|Todos os arquivos|*.*"
                if ($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw "Selecao da chave cancelada." }
                $keyFile = $picker.FileName
            }
            finally { $picker.Dispose() }
        }
    }
    if (-not (Test-Path -LiteralPath $keyFile -PathType Leaf)) { throw "Chave de upload nao encontrada." }
    $keyFile = (Resolve-Path -LiteralPath $keyFile).Path
    if ([string]::IsNullOrWhiteSpace($KeyAlias)) { throw "Informe -KeyAlias com o alias da chave de upload." }
    Write-Host "Unity $editorVersion | versao $releaseVersion | novo codigo $buildCode"
    $storePassword = Read-BuildPassword "Senha do keystore (digitacao oculta)"
    $keyPassword = Read-BuildPassword "Senha do alias $KeyAlias (digitacao oculta)"
    if ([string]::IsNullOrEmpty($storePassword) -or [string]::IsNullOrEmpty($keyPassword)) { throw "As duas senhas sao necessarias." }

    $buildEnvironment = @{
        CVD_KEYSTORE_PATH = $keyFile
        CVD_KEY_ALIAS = $KeyAlias
        CVD_KEYSTORE_PASSWORD = $storePassword
        CVD_KEY_PASSWORD = $keyPassword
        CVD_VERSION_CODE = [string]$buildCode
    }
    $previousEnvironment = @{}
    foreach ($name in $buildEnvironment.Keys) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
    try {
        foreach ($name in $buildEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $buildEnvironment[$name], 'Process') }
        $null = New-Item -ItemType Directory -Path $buildFolder -Force
        $logPath = Join-Path $buildFolder "play-console-build.log"
        Write-Host "Compilando o jogo. Log: $logPath"
        $arguments = @('-batchmode', '-quit', '-buildTarget', 'Android', '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'CatsVsDemons.Editor.PreparePlayableBuild.ExportPlayStoreBundle', '-logFile', ('"{0}"' -f $logPath))
        $process = Start-Process -FilePath $editor -ArgumentList $arguments -Wait -PassThru
        foreach ($name in $buildEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
        if ($process.ExitCode -ne 0) { throw "A Unity falhou (codigo $($process.ExitCode)). Consulte $logPath" }
        Assert-PlayBuildReceipt -BundlePath $bundlePath -VersionCode $buildCode
        $signatureLog = Join-Path $buildFolder "signature-check.log"
        $signatureError = Join-Path $buildFolder "signature-check-error.log"
        $verifyArguments = @('-J-Duser.language=en', '-J-Duser.country=US', '-verify', ('"{0}"' -f $bundlePath))
        $verification = Start-Process -FilePath $jarSigner -ArgumentList $verifyArguments -Wait -PassThru `
            -RedirectStandardOutput $signatureLog -RedirectStandardError $signatureError
        $signatureReport = Get-Content -LiteralPath $signatureLog -Raw
        if ($verification.ExitCode -ne 0 -or $signatureReport -notmatch 'jar verified') {
            throw "O jarsigner nao confirmou a assinatura criptografica do AAB."
        }
        Write-Host "AAB gerado, hash e assinatura verificados: $bundlePath" -ForegroundColor Green
        Write-Host "Envie o .aab para Teste interno no Play Console e teste no Android."
        Start-Process -FilePath 'explorer.exe' -ArgumentList @('/select,', ('"{0}"' -f $bundlePath)) | Out-Null
    }
    finally {
        foreach ($name in $buildEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
        $storePassword = $null; $keyPassword = $null
        $buildEnvironment.Clear(); $previousEnvironment.Clear()
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    try { Invoke-PlayConsoleExport }
    catch { Write-Host ("ERRO: " + $_.Exception.Message) -ForegroundColor Red; exit 1 }
}
