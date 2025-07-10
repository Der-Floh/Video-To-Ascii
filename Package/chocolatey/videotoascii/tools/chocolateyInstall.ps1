# ─── Chocolatey install script for VideoToAscii ────────────────────────────────
# This script downloads the version-specific ZIP published on GitHub,
# verifies its SHA-256 checksum and unpacks it into the package’s tools folder.
# Any *.exe* (or *.cmd*, *.ps1*) contained directly in the ZIP root is
# automatically shimmed by Chocolatey, so no extra work is required.

$ErrorActionPreference = 'Stop'

$packageName = $env:ChocolateyPackageName
$version     = $env:ChocolateyPackageVersion
$toolsDir    = Split-Path -Parent $MyInvocation.MyCommand.Definition

$url = "https://github.com/Der-Floh/Video-To-Ascii/releases/download/v$version/VideoToAscii-v$version.zip"
$checksum     = '{{SHA256}}'

$packageArgs = @{
    packageName   = $packageName
    unzipLocation = $toolsDir
    url           = $url
    checksum      = $checksum
    checksumType  = 'sha256'
}

Install-ChocolateyZipPackage @packageArgs
