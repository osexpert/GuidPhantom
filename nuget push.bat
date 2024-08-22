REM set myKey=
set ver=0.0.3

nuget push bin\Packages\Release\NuGet\GuidPhantom.%ver%.nupkg -src https://api.nuget.org/v3/index.json -ApiKey %myKey%
