@echo off
del *.nupkg
tools\nuget pack ..\source\NestClientFactory\NestClientFactory.csproj
tools\nuget push *.nupkg -Source https://api.nuget.org/v3/index.json
