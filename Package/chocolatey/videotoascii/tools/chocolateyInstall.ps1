Import-Module "$env:ChocolateyInstall\helpers\chocolateyInstaller.psm1"

$packageName = 'videotoascii'
$version = '{{VERSION}}'
$checksum = '{{SHA256}}'
$url = "https://github.com/Der-Floh/Video-To-Ascii/releases/download/v$version/VideoToAscii-v$version.msi"

$packageArgs = @{
  packageName    = $env:ChocolateyPackageName
  unzipLocation  = $toolsDir
  fileType       = 'MSI'
  url            = $url
  softwareName   = $packageName
  checksum       = $checksum
  checksumType   = 'sha256'
  silentArgs     = "/qn /norestart /l*v `"$($env:TEMP)\$($packageName).$($env:chocolateyPackageVersion).MsiInstall.log`""
  validExitCodes = @(0, 3010, 1641)
}

Install-ChocolateyPackage @packageArgs
