@echo off

setlocal

set DOTNET=dotnet
set SOLUTION=Consolida.slnx
set WEB_PROJECT=Consolida
set UNIT_TESTS=Tests\Consolida.UnitTests
set INTEGRATION_TESTS=Tests\Consolida.IntegrationTests
set CONFIGURATION=Release

if "%~1"=="" goto help
if /I "%~1"=="help"             goto help
if /I "%~1"=="restore"          goto restore
if /I "%~1"=="build"            goto build
if /I "%~1"=="run"              goto run
if /I "%~1"=="test"             goto test
if /I "%~1"=="test-unit"        goto test_unit
if /I "%~1"=="test-integration" goto test_integration
if /I "%~1"=="coverage"         goto coverage
if /I "%~1"=="format"           goto format
if /I "%~1"=="clean"            goto clean
if /I "%~1"=="docker-build"     goto docker_build
if /I "%~1"=="docker-up"        goto docker_up
if /I "%~1"=="docker-down"      goto docker_down
if /I "%~1"=="docker-logs"      goto docker_logs
if /I "%~1"=="docker-clean"     goto docker_clean

echo Неизвестная команда: %~1
echo.
goto help

:help
echo Доступные команды:
echo   make.bat restore             - восстановить NuGet-пакеты
echo   make.bat build               - собрать решение (Release)
echo   make.bat run                 - запустить приложение локально
echo   make.bat test                - все тесты
echo   make.bat test-unit           - только unit-тесты
echo   make.bat test-integration    - только integration-тесты
echo   make.bat coverage            - тесты с покрытием
echo   make.bat format              - форматирование кода
echo   make.bat clean               - очистить bin/obj
echo   make.bat docker-build        - собрать Docker-образ
echo   make.bat docker-up           - поднять docker compose
echo   make.bat docker-down         - остановить docker compose
echo   make.bat docker-logs         - логи docker compose
echo   make.bat docker-clean        - остановить и удалить volume'ы
goto end

:restore
%DOTNET% restore %SOLUTION%
goto end

:build
call :restore
%DOTNET% build %SOLUTION% --no-restore --configuration %CONFIGURATION%
goto end

:run
%DOTNET% run --project %WEB_PROJECT%
goto end

:test
call :build
%DOTNET% test %SOLUTION% --no-build --configuration %CONFIGURATION%
goto end

:test_unit
call :build
%DOTNET% test %UNIT_TESTS% --no-build --configuration %CONFIGURATION%
goto end

:test_integration
call :build
%DOTNET% test %INTEGRATION_TESTS% --no-build --configuration %CONFIGURATION%
goto end

:coverage
call :build
%DOTNET% test %SOLUTION% --no-build --configuration %CONFIGURATION% ^
    --collect:"XPlat Code Coverage" ^
    --results-directory ./TestResults
echo.
echo Coverage-файлы: ./TestResults/**/coverage.cobertura.xml
goto end

:format
%DOTNET% format %SOLUTION%
goto end

:clean
%DOTNET% clean %SOLUTION% --configuration %CONFIGURATION%
goto end

:docker_build
docker compose build
goto end

:docker_up
docker compose up -d --build
goto end

:docker_down
docker compose down
goto end

:docker_logs
docker compose logs -f
goto end

:docker_clean
docker compose down -v
goto end

:end
endlocal