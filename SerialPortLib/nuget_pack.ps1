$project = "SerialPortLib"
$root = (split-path -parent $MyInvocation.MyCommand.Definition) + '\..'
#$version = [System.Reflection.Assembly]::LoadFile("$root\$project\bin\Debug\$project.dll").GetName().Version

$versionStr = "{0}" -f ($env:APPVEYOR_REPO_TAG_NAME)

if (-not ([string]::IsNullOrEmpty($versionStr))) {
  Write-Host "Setting $project .nuspec version tag to $versionStr"

  $content = (Get-Content $root\$project\$project.nuspec) 
  $content = $content -replace '\$version\$',$versionStr

  $content | Out-File $root\$project\$project.compiled.nuspec

  & nuget pack $root\$project\$project.compiled.nuspec
}
else {
  Write-Host "Version string is empty, possibly dry run or APPVEYOR_REPO_TAG_NAME environment variable is not set"
}
