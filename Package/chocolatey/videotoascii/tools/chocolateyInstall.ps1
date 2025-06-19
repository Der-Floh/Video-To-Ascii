$packageName  = 'videotoascii'
$version      = '{{VERSION}}'
$checksum     = '{{SHA256}}'
$url          = "https://github.com/Der-Floh/Video-To-Ascii/releases/download/v$version/VideoToAscii-v$version.msi"

Install-ChocolateyPackage `
  -PackageName $packageName `
  -FileType 'msi' `
  -SilentArgs '/quiet' `
  -Url $url `
  -Checksum $checksum `
  -ChecksumType 'sha256'
