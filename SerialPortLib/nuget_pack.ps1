$root = (split-path -parent $MyInvocation.MyCommand.Definition) + '\..'
$version = [System.Reflection.Assembly]::LoadFile("$root\SerialPortLib\bin\Release\SerialPortLib.dll").GetName().Version
$versionStr = "{0}.{1}.{2}" -f ($version.Major, $version.Minor, $version.Build)

Write-Host "Setting .nuspec version tag to $versionStr"

$content = (Get-Content $root\SerialPortLib\SerialPortLib.nuspec) 
$content = $content -replace '\$version\$',$versionStr

$content | Out-File $root\SerialPortLib\SerialPortLib.compiled.nuspec

& nuget pack $root\SerialPortLib\SerialPortLib.compiled.nuspec

