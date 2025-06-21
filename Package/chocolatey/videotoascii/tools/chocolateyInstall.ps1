Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1"

$packageName = 'videotoascii'
$version = '{{VERSION}}'
$expectedSha = '{{SHA256}}'

$zipFile = Join-Path $PSScriptRoot "VideoToAscii-$version.zip"
$installDir = Join-Path $env:ChocolateyInstall 'lib' $packageName

# Verify the embedded ZIP's SHA-256
Get-FileHash $zipFile -Algorithm SHA256 | ForEach-Object {
    if ($_.Hash -ne $expectedSha) {
        throw "Checksum mismatch for $zipFile. Expected $expectedSha, got $($_.Hash)."
    }
}

Get-ChocolateyUnzip -FileFullPath $zipFile -Destination $installDir
Install-ChocolateyPath $installDir 'User'
