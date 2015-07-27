@echo off
cd %1
cd ..\build\
del *.nupkg
tools\nuget.exe pack ..\source\NestClientFactory\NestClientFactory.csproj
