dotnet tool restore
dotnet paket restore
dotnet run --project ../../FsClr.Tests/FsClr.Tests.fsproj
dotnet build ../FsClr.fsproj --configuration Release
cd ..
dotnet paket pack ./bin/Release
