@echo off

REM ============================================================
REM Consolida — make.bat (Windows)
REM Вызывается как: make <target>
REM Список команд: make help
REM ============================================================

setlocal

set DOTNET=dotnet
set SOLUTION=Consolida.slnx
set WEB_PROJECT=Consolida
set UNIT_TESTS=Tests\Consolida.UnitTests
set INTEGRATION_TESTS=Tests\Consolida.IntegrationTests
set CONFIGURATION=Release
set COVERLET_SETTINGS=coverlet.runsettings
set REPORT_DIR=TestResults\CoverageReport

if "%~1"=="" goto help
if /I "%~1"=="help"             goto help
if /I "%~1"=="restore"          goto restore
if /I "%~1"=="build"            goto build
if /I "%~1"=="run"              goto run
if /I "%~1"=="test"             goto test
if /I "%~1"=="test-unit"        goto test_unit
if /I "%~1"=="test-integration" goto test_integration
if /I "%~1"=="coverage"         goto coverage
if /I "%~1"=="open-coverage"    goto open_coverage
if /I "%~1"=="format"           goto format
if /I "%~1"=="clean"            goto clean
if /I "%~1"=="docker-env"       goto docker_env
if /I "%~1"=="docker-build"     goto docker_build
if /I "%~1"=="docker-up"        goto docker_up
if /I "%~1"=="docker-up-redis"  goto docker_up_redis
if /I "%~1"=="docker-down"      goto docker_down
if /I "%~1"=="docker-restart"   goto docker_restart
if /I "%~1"=="docker-logs"      goto docker_logs
if /I "%~1"=="docker-ps"        goto docker_ps
if /I "%~1"=="docker-shell"     goto docker_shell
if /I "%~1"=="docker-db-shell"  goto docker_db_shell
if /I "%~1"=="docker-clean"     goto docker_clean

echo Неизвестная команда: %~1
echo.
goto help

:help
echo.
echo   Сборка и разработка
echo   -------------------------------------------------------------
echo   make restore             - восстановить NuGet-пакеты и локальные тулы
echo   make build               - собрать решение (Release)
echo   make run                 - запустить приложение локально
echo   make format              - форматирование кода
echo   make clean               - очистить bin/obj и TestResults
echo.
echo   Тесты
echo   -------------------------------------------------------------
echo   make test                - все тесты
echo   make test-unit           - только unit-тесты
echo   make test-integration    - только integration-тесты
echo   make coverage            - тесты с покрытием + HTML-отчёт
echo   make open-coverage       - открыть HTML-отчёт в браузере
echo.
echo   Docker
echo   -------------------------------------------------------------
echo   make docker-env          - создать .env из .env.example (если нет)
echo   make docker-build        - собрать образ
echo   make docker-up           - запустить postgres + app (без Redis)
echo   make docker-up-redis     - запустить postgres + redis + app
echo   make docker-down         - остановить контейнеры
echo   make docker-restart      - перезапустить контейнеры
echo   make docker-logs         - следить за логами
echo   make docker-ps           - статус контейнеров
echo   make docker-shell        - shell внутри контейнера приложения
echo   make docker-db-shell     - psql внутри контейнера postgres
echo   make docker-clean        - остановить и удалить volume'ы и образ
echo.
goto end

REM ============================================================
REM  Сборка и разработка
REM ============================================================

:restore
%DOTNET% restore %SOLUTION%
%DOTNET% tool restore
goto end

:build
call :restore
%DOTNET% build %SOLUTION% --no-restore --configuration %CONFIGURATION%
goto end

:run
%DOTNET% run --project %WEB_PROJECT%
goto end

:format
%DOTNET% format %SOLUTION%
goto end

:clean
%DOTNET% clean %SOLUTION% --configuration %CONFIGURATION%
if exist TestResults rmdir /s /q TestResults
goto end

REM ============================================================
REM  Тесты
REM ============================================================

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
    --settings %COVERLET_SETTINGS% ^
    --collect:"XPlat Code Coverage" ^
    --results-directory ./TestResults
%DOTNET% reportgenerator ^
    "-reports:./TestResults/**/coverage.cobertura.xml" ^
    "-targetdir:./%REPORT_DIR%" ^
    "-reporttypes:Html;MarkdownSummary" ^
    "-title:Consolida - Code Coverage"
echo.
echo HTML-отчёт: %REPORT_DIR%\index.html
echo Открыть:    make open-coverage
goto end

:open_coverage
if exist "%REPORT_DIR%\index.html" (
    start "" "%REPORT_DIR%\index.html"
) else (
    echo Отчёт не найден. Сначала запустите: make coverage
)
goto end

REM ============================================================
REM  Docker
REM ============================================================

:docker_env
if not exist .env (
    copy .env.example .env >nul
    echo Создан .env из .env.example.
    echo Откройте и заполните POSTGRES_PASSWORD ^(и REDIS_PASSWORD, если используете Redis^).
) else (
    echo .env уже существует, ничего не менял.
)
goto end

:docker_build
docker compose build
goto end

:docker_up
call :docker_env
docker compose up -d --build
echo.
echo Приложение:  http://localhost:5000
echo Hangfire:    http://localhost:5000/hangfire ^(только с localhost^)
echo Остановить:  make docker-down
goto end

:docker_up_redis
call :docker_env
set REDIS_HOST=redis
docker compose --profile with-redis up -d --build
set REDIS_HOST=
echo.
echo Запущено с Redis.
echo Приложение:  http://localhost:5000
goto end

:docker_down
docker compose down
goto end

:docker_restart
docker compose restart
goto end

:docker_logs
docker compose logs -f
goto end

:docker_ps
docker compose ps
goto end

:docker_shell
docker compose exec app /bin/sh
goto end

:docker_db_shell
docker compose exec postgres psql -U postgres -d consolida
goto end

:docker_clean
docker compose down -v --rmi local
echo.
echo Контейнеры, volume'ы и локальные образы удалены.
goto end

:end
endlocal