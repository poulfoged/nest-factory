@echo off
del *.nupkg
tools\nuget pack ..\source\NestClientFactory\NestClientFactory.csproj
tools\nuget push *.nupkg
