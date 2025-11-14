@echo off
echo Demarrage de l'application SalleSense - Configuration MAISON
echo =============================================================
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=Home
echo Variable d'environnement : %ASPNETCORE_ENVIRONMENT%
dotnet run --no-launch-profile
pause
