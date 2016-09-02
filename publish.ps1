$y = "yellow"
$projectJson = ".\src\NSourceMap\project.json"
$projectJsonContent = Get-Content $projectJson
$projectJsonData = $projectJsonContent -Join "`n" | ConvertFrom-Json
$oldVersion = $projectJsonData.version
$version = $projectJsonData.version.TrimEnd("-*")


Write-Host "Packing $version" -fo $y
Remove-Item -Force -Recurse pub
dotnet pack .\src\NSourceMap\project.json -c Release -o pub


Write-Host "Publishing to nuget and symbolsource"  -fo $y
nuget push "pub/NSourceMap.$version.nupkg"


$gitTag = "releases-$version"
Write-Host "Creating git tag $gitTag" -fo $y
git tag -d $gitTag
git tag -a $gitTag -m "$version release"


$patchIndex = $version.LastIndexOf(".") + 1
$patchNumber = [Convert]::ToInt32($version.Substring($patchIndex)) + 1
$version = $version.Substring(0, $patchIndex) + $patchNumber
Write-Host "Incrementing package version to $version" -fo $y
$projectJsonContent.Replace('"version": "' + $oldVersion + '"', '"version": "' + $version + '-*"') | Out-File $projectJson -Encoding 'utf8'
