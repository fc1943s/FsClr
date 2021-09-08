. ./core.ps1

dotnet tool restore
dotnet paket restore

Invoke-Call -ScriptBlock {
    dotnet run --project ../../FsClr.Tests/FsClr.Tests.fsproj
} -ErrorAction Stop

dotnet build ../FsClr.fsproj --configuration Release
cd ..
dotnet paket pack ./bin/Release
