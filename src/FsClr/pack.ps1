dotnet tool restore
dotnet paket restore
dotnet run --project ../FsClr.Tests/FsClr.Tests.fsproj
dotnet build --configuration Release
dotnet paket pack bin/Release --build-config Release
