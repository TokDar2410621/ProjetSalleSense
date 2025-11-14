@echo off
echo Demarrage de l'application SalleSense - Configuration DEVELOPPEMENT
echo ===================================================================
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Development
echo Variable d'environnement : %ASPNETCORE_ENVIRONMENT%
dotnet run --no-launch-profile
pause
