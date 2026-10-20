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

function New-PrivateSigningDirectory {
    $folder = Join-Path ([IO.Path]::GetTempPath()) ("cvd-signing-" + [Guid]::NewGuid().ToString('N'))
    $null = New-Item -ItemType Directory -Path $folder
    try {
        $identity = [Security.Principal.WindowsIdentity]::GetCurrent().User
        $acl = New-Object Security.AccessControl.DirectorySecurity
        $acl.SetOwner($identity)
        $acl.SetAccessRuleProtection($true, $false)
        $rule = New-Object Security.AccessControl.FileSystemAccessRule(
            $identity, 'FullControl', 'ContainerInherit, ObjectInherit', 'None', 'Allow')
        $acl.AddAccessRule($rule)
        Set-Acl -LiteralPath $folder -AclObject $acl
        return $folder
    }
    catch { Remove-Item -LiteralPath $folder -Recurse -Force; throw "Nao foi possivel proteger a pasta temporaria de assinatura." }
}

function Assert-UploadKeySigns {
    param([string]$JarSigner, [string]$KeyFile, [string]$Alias,
        [string]$StorePassword, [string]$KeyPassword, [string]$PrivateFolder)
    $storeFile = Join-Path $PrivateFolder 'store-password'
    $keyPassFile = Join-Path $PrivateFolder 'key-password'
    $probe = Join-Path $PrivateFolder 'signing-probe.jar'
    $signedProbe = Join-Path $PrivateFolder 'signed-probe.jar'
    $output = Join-Path $PrivateFolder 'probe-output.log'
    $errorOutput = Join-Path $PrivateFolder 'probe-error.log'
    try {
        [IO.File]::WriteAllText($storeFile, $StorePassword)
        [IO.File]::WriteAllText($keyPassFile, $KeyPassword)
        Add-Type -AssemblyName System.IO.Compression.FileSystem
        $archive = [IO.Compression.ZipFile]::Open($probe, 'Create')
        try {
            $entry = $archive.CreateEntry('signing-check.txt')
            $writer = New-Object IO.StreamWriter($entry.Open())
            try { $writer.Write('Cats vs Demons upload-key validation; not a game build.') }
            finally { $writer.Dispose() }
        }
        finally { $archive.Dispose() }
        $arguments = @('-J-Duser.language=en', '-J-Duser.country=US', '-J-Dfile.encoding=UTF-8', '-keystore', ('"{0}"' -f $KeyFile),
            '-storepass:file', ('"{0}"' -f $storeFile), '-keypass:file', ('"{0}"' -f $keyPassFile),
            '-signedjar', ('"{0}"' -f $signedProbe), ('"{0}"' -f $probe), ('"{0}"' -f $Alias))
        $process = Start-Process -FilePath $JarSigner -ArgumentList $arguments -Wait -PassThru `
            -RedirectStandardOutput $output -RedirectStandardError $errorOutput
        if ($process.ExitCode -ne 0 -or -not (Test-Path -LiteralPath $signedProbe -PathType Leaf)) {
            throw "A chave nao assinou o teste. Confira o arquivo selecionado, o alias e as duas senhas. A Unity nao foi iniciada."
        }
        $verifyArguments = @('-J-Duser.language=en', '-J-Duser.country=US', '-verify', ('"{0}"' -f $signedProbe))
        $verified = Start-Process -FilePath $JarSigner -ArgumentList $verifyArguments -Wait -PassThru `
            -RedirectStandardOutput $output -RedirectStandardError $errorOutput
        if ($verified.ExitCode -ne 0 -or (Get-Content -LiteralPath $output -Raw) -notmatch 'jar verified') {
            throw "O Java nao confirmou a assinatura de teste. A Unity nao foi iniciada."
        }
    }
    finally {
        foreach ($file in @($storeFile, $keyPassFile, $probe, $signedProbe, $output, $errorOutput)) {
            if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
        }
    }
}

function Protect-BuildLog {
    param([string]$LogPath, [string[]]$Passwords)
    if ([string]::IsNullOrWhiteSpace($LogPath) -or -not (Test-Path -LiteralPath $LogPath -PathType Leaf)) { return }
    $content = [IO.File]::ReadAllText($LogPath)
    $sanitized = [regex]::Replace($content, '(?im)(CVD_KEY(?:STORE)?_PASSWORD\s*=\s*)[^\r\n]*', '$1[REDACTED]')
    foreach ($password in $Passwords) {
        if (-not [string]::IsNullOrEmpty($password) -and $password.Length -ge 6) {
            $sanitized = $sanitized.Replace($password, '[REDACTED]')
        }
    }
    if ($sanitized -cne $content) { [IO.File]::WriteAllText($LogPath, $sanitized) }
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
        Add-Type -AssemblyName System.Windows.Forms
        $picker = New-Object System.Windows.Forms.OpenFileDialog
        try {
            $picker.Title = "Selecione a chave de upload ATIVA no Play Console"
            $picker.Filter = "Chave de upload (*.keystore;*.jks)|*.keystore;*.jks|Todos os arquivos|*.*"
            $keysFolder = Join-Path ([Environment]::GetFolderPath('MyDocuments')) 'Chaves'
            if (Test-Path -LiteralPath $keysFolder -PathType Container) { $picker.InitialDirectory = $keysFolder }
            if ($picker.ShowDialog() -ne [System.Windows.Forms.DialogResult]::OK) { throw "Selecao da chave cancelada." }
            $keyFile = $picker.FileName
        }
        finally { $picker.Dispose() }
    }
    if (-not (Test-Path -LiteralPath $keyFile -PathType Leaf)) { throw "Chave de upload nao encontrada." }
    $keyFile = (Resolve-Path -LiteralPath $keyFile).Path
    if ([string]::IsNullOrWhiteSpace($KeyAlias)) { throw "Informe -KeyAlias com o alias da chave de upload." }
    if ($KeyAlias.IndexOfAny([char[]]@([char]34, [char]13, [char]10)) -ge 0) { throw "O alias nao pode conter aspas nem quebras de linha." }
    Write-Host "Unity $editorVersion | versao $releaseVersion | novo codigo $buildCode"
    Write-Host "Chave selecionada: $keyFile | alias: $KeyAlias"
    Write-Host "Se houve redefinicao, use a nova chave depois da ativacao pelo Google."
    $storePassword = $null; $keyPassword = $null; $privateFolder = $null; $logPath = $null
    $buildEnvironment = @{}
    $previousEnvironment = @{}
    try {
        $storePassword = Read-BuildPassword "Senha do keystore (digitacao oculta)"
        $keyPassword = Read-BuildPassword "Senha do alias $KeyAlias (digitacao oculta)"
        if ([string]::IsNullOrEmpty($storePassword) -or [string]::IsNullOrEmpty($keyPassword)) { throw "As duas senhas sao necessarias." }
        $privateFolder = New-PrivateSigningDirectory
        Write-Host "Verificando a chave antes de compilar..."
        Assert-UploadKeySigns -JarSigner $jarSigner -KeyFile $keyFile -Alias $KeyAlias `
            -StorePassword $storePassword -KeyPassword $keyPassword -PrivateFolder $privateFolder
        $payload = Join-Path $privateFolder 'signing.json'
        $credentials = @{ keystorePath = $keyFile; keyAlias = $KeyAlias; keystorePassword = $storePassword; keyPassword = $keyPassword }
        [IO.File]::WriteAllText($payload, ($credentials | ConvertTo-Json -Compress))
        $credentials.Clear()
        $buildEnvironment = @{
            CVD_SIGNING_FILE = $payload
            CVD_KEYSTORE_PATH = $keyFile
            CVD_KEY_ALIAS = $KeyAlias
            CVD_KEYSTORE_PASSWORD = $null
            CVD_KEY_PASSWORD = $null
            CVD_VERSION_CODE = [string]$buildCode
        }
        foreach ($name in $buildEnvironment.Keys) { $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }
        foreach ($name in $buildEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $buildEnvironment[$name], 'Process') }
        $null = New-Item -ItemType Directory -Path $buildFolder -Force
        $logPath = Join-Path $buildFolder "play-console-build.log"
        Write-Host "Compilando o jogo. Log: $logPath"
        $arguments = @('-batchmode', '-quit', '-buildTarget', 'Android', '-projectPath', ('"{0}"' -f $projectRoot),
            '-executeMethod', 'CatsVsDemons.Editor.PreparePlayableBuild.ExportPlayStoreBundle', '-logFile', ('"{0}"' -f $logPath))
        $process = Start-Process -FilePath $editor -ArgumentList $arguments -Wait -PassThru
        foreach ($name in $buildEnvironment.Keys) { [Environment]::SetEnvironmentVariable($name, $previousEnvironment[$name], 'Process') }
        if ($process.ExitCode -ne 0) { throw "A Unity falhou (codigo $($process.ExitCode)). Consulte $logPath" }
        if (Test-Path -LiteralPath $payload) { throw "A Unity nao leu as credenciais temporarias. Atualize PreparePlayableBuild.cs junto com este exportador." }
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
        try { Protect-BuildLog -LogPath $logPath -Passwords @($storePassword, $keyPassword) }
        finally {
            if ($null -ne $privateFolder -and (Test-Path -LiteralPath $privateFolder)) { Remove-Item -LiteralPath $privateFolder -Recurse -Force }
            $storePassword = $null; $keyPassword = $null
            $buildEnvironment.Clear(); $previousEnvironment.Clear()
        }
    }
}

if ($MyInvocation.InvocationName -ne '.') {
    try { Invoke-PlayConsoleExport }
    catch { Write-Host ("ERRO: " + $_.Exception.Message) -ForegroundColor Red; exit 1 }
}
