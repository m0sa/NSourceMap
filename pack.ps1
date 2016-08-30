$epoch = [math]::truncate((new-timespan -start (get-date -date "01/01/1970") -end (get-date)).TotalSeconds)
dotnet pack .\src\NSourceMap\project.json -c Release --version-suffix "alpha$epoch"