@echo off
echo Demarrage de l'application SalleSense - Configuration ECOLE
echo ============================================================
cd /d "%~dp0"
set ASPNETCORE_ENVIRONMENT=School
echo Variable d'environnement : %ASPNETCORE_ENVIRONMENT%
dotnet run --no-launch-profile
pause
