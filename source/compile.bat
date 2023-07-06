@echo off
set path=%windir%\Microsoft.NET\Framework64\v4.0.30319;%path%
msbuild near-msg.csproj /p:Configuration=Release