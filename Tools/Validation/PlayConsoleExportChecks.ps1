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
Write-Host "$checks export launcher checks passed. Unity compilation and device testing remain separate."
