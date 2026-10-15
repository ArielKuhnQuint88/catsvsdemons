Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "..\Android\Export-PlayConsole.ps1")

$checks = 0
function Assert-ExportCheck {
    param([bool]$Condition, [string]$Message)
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Assert-ExportRejects {
    param([scriptblock]$Action, [string]$Message)
    $rejected = $false
    try { & $Action | Out-Null } catch { $rejected = $true }
    Assert-ExportCheck $rejected $Message
}

Assert-ExportCheck ((Get-NextPlayVersionCode 2 0) -eq 2) "Preserve current project code for first upload."
Assert-ExportCheck ((Get-NextPlayVersionCode 2 2) -eq 3) "Advance beyond code already uploaded."
Assert-ExportCheck ((Get-NextPlayVersionCode 2 50) -eq 51) "Advance beyond all uploaded tracks."
Assert-ExportCheck ((Get-NextPlayVersionCode 10 2) -eq 10) "Do not reduce project code."
Assert-ExportCheck ((Get-NextPlayVersionCode 2 2099999999) -eq 2100000000) "Accept highest supported code."
Assert-ExportRejects { Get-NextPlayVersionCode 2 -1 } "Reject unknown previous code."
Assert-ExportRejects { Get-NextPlayVersionCode 2 2100000000 } "Reject overflow."
Assert-ExportRejects { Get-NextPlayVersionCode 0 0 } "Reject invalid project code."

$temp = Join-Path ([IO.Path]::GetTempPath()) ("cvd-export-checks-" + [Guid]::NewGuid())
$null = New-Item -ItemType Directory -Path $temp
try {
    $bundle = Join-Path $temp "receipt-contract-test.aab"
    # These bytes only test receipt/hash failures. They are not an Android build.
    [IO.File]::WriteAllBytes($bundle, [byte[]](1, 2, 3, 4))
    $receiptFile = [IO.Path]::ChangeExtension($bundle, ".build.json")
    $receipt = @{
        file = [IO.Path]::GetFileName($bundle)
        package = "com.qipgames.catsvsdemons"
        versionCode = 3
        targetApi = 36
        architecture = "ARM64 / IL2CPP"
        nativeValidation = "ELF64 ARM64: 16 KB"
        sha256 = (Get-FileHash $bundle -Algorithm SHA256).Hash.ToLowerInvariant()
    }
    $receipt | ConvertTo-Json | Set-Content -LiteralPath $receiptFile
    Assert-PlayBuildReceipt $bundle 3
    Assert-ExportCheck $true "Matching receipt and file accepted."
    [IO.File]::WriteAllBytes($bundle, [byte[]](1, 2, 3, 5))
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 3 } "Reject altered file hash."
    [IO.File]::WriteAllBytes($bundle, [byte[]](1, 2, 3, 4))
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 4 } "Reject wrong release code."
    $receipt.package = "wrong.app"
    $receipt | ConvertTo-Json | Set-Content -LiteralPath $receiptFile
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 3 } "Reject wrong app package."
    $receipt.package = "com.qipgames.catsvsdemons"; $receipt.targetApi = 35
    $receipt | ConvertTo-Json | Set-Content -LiteralPath $receiptFile
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 3 } "Reject old API target."
    $receipt.targetApi = 36; $receipt.architecture = "ARMv7"
    $receipt | ConvertTo-Json | Set-Content -LiteralPath $receiptFile
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 3 } "Reject unexpected architecture."
    Remove-Item -LiteralPath $receiptFile
    Assert-ExportRejects { Assert-PlayBuildReceipt $bundle 3 } "Reject missing receipt."
}
finally { Remove-Item -LiteralPath $temp -Recurse -Force }

$private = New-PrivateSigningDirectory
try {
    $acl = Get-Acl -LiteralPath $private
    Assert-ExportCheck $acl.AreAccessRulesProtected "Signing folder must not inherit access from temp."
    $owner = [Security.Principal.WindowsIdentity]::GetCurrent().User
    foreach ($rule in $acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier])) {
        Assert-ExportCheck ($rule.IdentityReference -eq $owner) "Signing folder is restricted to the current account."
    }
    $log = Join-Path $private 'redaction-test.log'
    $fakeStorePassword = 'test-only-store-password'
    $fakeKeyPassword = 'test-only-key-password'
    [IO.File]::WriteAllText($log, "CVD_KEYSTORE_PASSWORD = hidden-value`r`nCVD_KEY_PASSWORD = x`r`n$fakeStorePassword / $fakeKeyPassword`r`nkeep diagnostic")
    Protect-BuildLog -LogPath $log -Passwords @($fakeStorePassword, $fakeKeyPassword)
    $redacted = [IO.File]::ReadAllText($log)
    Assert-ExportCheck ($redacted -notmatch 'hidden-value| = x|test-only-store-password|test-only-key-password') "Redact values and legacy environment assignments."
    Assert-ExportCheck ($redacted -match 'keep diagnostic') "Keep unrelated error diagnostics."

    $javaRoot = $env:JAVA_HOME
    if ([string]::IsNullOrWhiteSpace($javaRoot)) { throw "JAVA_HOME is required for the real signing checks." }
    $keytool = Join-Path $javaRoot 'bin\keytool.exe'
    $jarsigner = Join-Path $javaRoot 'bin\jarsigner.exe'
    $testKeystore = Join-Path $private 'throwaway-test.jks'
    # Synthetic credentials only. These are never used for a game or account.
    $storePass = 'test-only-' + [Guid]::NewGuid().ToString('N')
    $keyPass = 'test-key-' + [Guid]::NewGuid().ToString('N')
    $storePassFile = Join-Path $private 'fixture-store-password'
    $keyPassFile = Join-Path $private 'fixture-key-password'
    [IO.File]::WriteAllText($storePassFile, $storePass)
    [IO.File]::WriteAllText($keyPassFile, $keyPass)
    $arguments = @('-genkeypair', '-keystore', ('"{0}"' -f $testKeystore), '-storetype', 'JKS',
        '-storepass:file', ('"{0}"' -f $storePassFile), '-keypass:file', ('"{0}"' -f $keyPassFile),
        '-alias', 'test-upload', '-keyalg', 'RSA', '-keysize', '2048', '-validity', '10000', '-dname', '"CN=Throwaway Test"')
    $generation = Start-Process -FilePath $keytool -ArgumentList $arguments -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $private 'fixture-output.log') -RedirectStandardError (Join-Path $private 'fixture-error.log')
    Assert-ExportCheck ($generation.ExitCode -eq 0) "Create throwaway Java key for real signing checks."
    Assert-UploadKeySigns $jarsigner $testKeystore 'test-upload' $storePass $keyPass $private
    Assert-ExportCheck $true "Accept correct keystore, alias, and distinct key password."
    Assert-ExportRejects { Assert-UploadKeySigns $jarsigner $testKeystore 'test-upload' 'incorrect-test-password' $keyPass $private } "Reject wrong store password before Unity."
    Assert-ExportRejects { Assert-UploadKeySigns $jarsigner $testKeystore 'test-upload' $storePass 'incorrect-test-password' $private } "Reject wrong private-key password before Unity."
    Assert-ExportRejects { Assert-UploadKeySigns $jarsigner $testKeystore 'missing-alias' $storePass $keyPass $private } "Reject missing alias before Unity."
    Assert-ExportCheck (-not (Test-Path -LiteralPath (Join-Path $private 'store-password'))) "Remove temporary store password after failure."
    Assert-ExportCheck (-not (Test-Path -LiteralPath (Join-Path $private 'key-password'))) "Remove temporary key password after failure."
    Assert-ExportCheck (-not (Test-Path -LiteralPath (Join-Path $private 'signing-probe.jar'))) "Remove temporary probe after failure."

    $pkcsFile = Join-Path $private 'throwaway-test.keystore'
    $pkcsPassword = 'test-only-' + [char]0xE1 + '-quotes-"-&-spaces '
    [IO.File]::WriteAllText($storePassFile, $pkcsPassword)
    $pkcsArguments = @('-J-Dfile.encoding=UTF-8', '-genkeypair', '-keystore', ('"{0}"' -f $pkcsFile),
        '-storetype', 'PKCS12', '-storepass:file', ('"{0}"' -f $storePassFile), '-alias', 'catsvsdemons',
        '-keyalg', 'RSA', '-keysize', '2048', '-validity', '10000', '-dname', '"CN=Throwaway PKCS12 Test"')
    $pkcsGeneration = Start-Process -FilePath $keytool -ArgumentList $pkcsArguments -Wait -PassThru `
        -RedirectStandardOutput (Join-Path $private 'fixture-output.log') -RedirectStandardError (Join-Path $private 'fixture-error.log')
    Assert-ExportCheck ($pkcsGeneration.ExitCode -eq 0) "Create PKCS12 fixture with Unicode and punctuation."
    Assert-UploadKeySigns $jarsigner $pkcsFile 'catsvsdemons' $pkcsPassword $pkcsPassword $private
    Assert-ExportCheck $true "Accept new-key PKCS12 format and Unicode password."
    Assert-ExportRejects { Assert-UploadKeySigns $jarsigner $pkcsFile 'catsvsdemons' $pkcsPassword 'incorrect-test-password' $private } "Reject wrong PKCS12 private-key password."
}
finally { Remove-Item -LiteralPath $private -Recurse -Force }
Write-Host "$checks export launcher checks passed. Unity compilation and device testing remain separate."
