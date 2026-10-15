[CmdletBinding()]
param([string]$UnityPath)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Invoke-ApkExport {
    $projectRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
    $versionFile = Join-Path $projectRoot "ProjectSettings\ProjectVersion.txt"
    $version = [regex]::Match((Get-Content -LiteralPath $versionFile -Raw), '(?m)^m_EditorVersion:\s*(\S+)')
    if (-not $version.Success) { throw "Versao da Unity ausente no projeto." }
    $editorVersion = $version.Groups[1].Value
    $editor = $UnityPath
    if ([string]::IsNullOrWhiteSpace($editor)) {
        foreach ($programRoot in @($env:ProgramFiles, [Environment]::GetEnvironmentVariable("ProgramFiles(x86)"), $env:LOCALAPPDATA)) {
            if ([string]::IsNullOrWhiteSpace($programRoot)) { continue }
            $candidate = Join-Path $programRoot "Unity\Hub\Editor\$editorVersion\Editor\Unity.exe"
            if (Test-Path -LiteralPath $candidate -PathType Leaf) { $editor = $candidate; break }
        }
    }
    if ([string]::IsNullOrWhiteSpace($editor) -or -not (Test-Path -LiteralPath $editor -PathType Leaf)) {
        throw "Instale Unity $editorVersion pelo Unity Hub ou informe -UnityPath."
    }
    $androidFolder = Join-Path (Split-Path $editor -Parent) "Data\PlaybackEngines\AndroidPlayer"
    $java = Join-Path $androidFolder "OpenJDK\bin\java.exe"
    $buildTools = Join-Path $androidFolder "SDK\build-tools"
    if (-not (Test-Path -LiteralPath $java -PathType Leaf) -or -not (Test-Path -LiteralPath $buildTools -PathType Container)) {
        throw "No Unity Hub, instale Android Build Support, Android SDK & NDK Tools e OpenJDK."
    }
    $signers = @(
        foreach ($folder in (Get-ChildItem -LiteralPath $buildTools -Directory)) {
            $parsedVersion = $null
            $jar = Join-Path $folder.FullName "lib\apksigner.jar"
            if ([version]::TryParse($folder.Name, [ref]$parsedVersion) -and (Test-Path -LiteralPath $jar -PathType Leaf)) {
                [pscustomobject]@{ Version = $parsedVersion; Path = $jar }
            }
        }
    )
    $signer = $signers | Sort-Object Version -Descending | Select-Object -First 1
    if ($null -eq $signer) { throw "apksigner nao encontrado. Reinstale Android SDK & NDK Tools pelo Unity Hub." }
    $aapt = Join-Path (Split-Path (Split-Path $signer.Path -Parent) -Parent) 'aapt.exe'
    if (-not (Test-Path -LiteralPath $aapt -PathType Leaf)) { throw "aapt nao encontrado no Android SDK Build Tools." }

    $lockPath = Join-Path $projectRoot "Temp\UnityLockfile"
    if (Test-Path -LiteralPath $lockPath) {
        try {
            $probe = [IO.File]::Open($lockPath, [IO.FileMode]::Open, [IO.FileAccess]::ReadWrite, [IO.FileShare]::None)
            $probe.Dispose()
        }
        catch { throw "Feche a Unity deste projeto antes de exportar o APK." }
    }
    $buildFolder = Join-Path $projectRoot "Builds\CatsVsDemons-Android"
    $null = New-Item -ItemType Directory -Path $buildFolder -Force
    $fileName = "CatsVsDemons-Teste-{0}-{1}.apk" -f (Get-Date -Format "yyyyMMdd-HHmmss"), ([Guid]::NewGuid().ToString('N').Substring(0, 8))
    $apk = Join-Path $buildFolder $fileName
    $log = Join-Path $buildFolder "apk-build.log"
    $environment = @{
        CVD_APK_OUTPUT = $apk
        CVD_SIGNING_FILE = $null
        CVD_KEYSTORE_PASSWORD = $null
        CVD_KEY_PASSWORD = $null
    }
    $previous = @{}
    foreach ($name in $environment.Keys) { $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
    Write-Host "CATS VS DEMONS - APK DE TESTE" -ForegroundColor Cyan
    Write-Host "Unity $editorVersion | ARM64 | Android 8 ou superior"
    Write-Host "Assinatura de teste da Unity. Nao pede senha da chave do Play Console."
    Write-Host "O aplicativo sera instalado como Cats vs Demons (Teste)."
    Write-Host "Compilando. Log: $log"
    try {
        foreach ($name in $environment.Keys) { [Environment]::SetEnvironmentVariable($name, $environment[$name], 'Process') }
        $arguments = @('-batchmode', '-quit', '-buildTarget', 'Android', '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'CatsVsDemons.Editor.PreparePlayableBuild.ExportLightweightAndroid',
            '-logFile', ('"{0}"' -f $log))
        $process = Start-Process -FilePath $editor -ArgumentList $arguments -Wait -PassThru
        if ($process.ExitCode -ne 0) { throw "A Unity falhou (codigo $($process.ExitCode)). Consulte $log" }
    }
    finally {
        foreach ($name in $environment.Keys) { [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process') }
        $environment.Clear(); $previous.Clear()
    }
    $receiptPath = [IO.Path]::ChangeExtension($apk, ".build.json")
    if (-not (Test-Path -LiteralPath $apk -PathType Leaf) -or -not (Test-Path -LiteralPath $receiptPath -PathType Leaf)) {
        throw "A Unity nao entregou o APK e seu relatorio. Atualize PreparePlayableBuild.cs e PlayStoreBuildValidator.cs junto com este exportador."
    }
    $receipt = Get-Content -LiteralPath $receiptPath -Raw | ConvertFrom-Json
    $hash = (Get-FileHash -LiteralPath $apk -Algorithm SHA256).Hash
    if ($receipt.file -ne $fileName -or $receipt.package -ne 'com.qipgames.catsvsdemons.test' -or
        $receipt.testBuild -ne $true -or $receipt.architecture -ne 'ARM64 / IL2CPP' -or
        $receipt.targetApi -lt 36 -or $receipt.sha256 -ne $hash) {
        throw "O APK nao corresponde ao relatorio de teste esperado."
    }
    $signatureLog = Join-Path $buildFolder "apk-signature-check.log"
    $signatureError = Join-Path $buildFolder "apk-signature-error.log"
    $verify = @('-Duser.language=en', '-Duser.country=US', '-jar', ('"{0}"' -f $signer.Path),
        'verify', '--verbose', '--print-certs', ('"{0}"' -f $apk))
    $check = Start-Process -FilePath $java -ArgumentList $verify -Wait -PassThru -RedirectStandardOutput $signatureLog -RedirectStandardError $signatureError
    if ($check.ExitCode -ne 0) { throw "A assinatura do APK nao foi aprovada pelo apksigner. Consulte $signatureLog" }
    $badgingLog = Join-Path $buildFolder 'apk-package-check.log'
    $badgingError = Join-Path $buildFolder 'apk-package-error.log'
    $badging = Start-Process -FilePath $aapt -ArgumentList @('dump', 'badging', ('"{0}"' -f $apk)) -Wait -PassThru -RedirectStandardOutput $badgingLog -RedirectStandardError $badgingError
    $manifest = Get-Content -LiteralPath $badgingLog -Raw
    if ($badging.ExitCode -ne 0 -or $manifest -notmatch "(?m)^package: name='com\.qipgames\.catsvsdemons\.test'") {
        throw "O manifesto do APK nao corresponde ao aplicativo de teste. Consulte $badgingLog"
    }
    Write-Host "APK gerado; estrutura, hash e assinatura verificados:" -ForegroundColor Green
    Write-Host $apk
    Write-Host "Copie o .apk para o celular e abra para instalar Cats vs Demons (Teste)."
    Start-Process -FilePath "explorer.exe" -ArgumentList ('/select,"{0}"' -f $apk) | Out-Null
}

if ($MyInvocation.InvocationName -ne '.') {
    try { Invoke-ApkExport }
    catch { Write-Host ("ERRO: " + $_.Exception.Message) -ForegroundColor Red; exit 1 }
}
