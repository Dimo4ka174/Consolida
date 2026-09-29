# ============================================================
# Consolida — Makefile
# Использование: make <target>
# На Windows make.bat вызывается так же, как make <target>.
# ============================================================

.DEFAULT_GOAL := help

DOTNET            ?= dotnet
SOLUTION          := Consolida.slnx
WEB_PROJECT       := Consolida
UNIT_TESTS        := Tests/Consolida.UnitTests
INTEGRATION_TESTS := Tests/Consolida.IntegrationTests
CONFIGURATION     := Release
COVERLET_SETTINGS := coverlet.runsettings
REPORT_DIR        := TestResults/CoverageReport

.PHONY: help restore build run test test-unit test-integration coverage \
        open-coverage format clean \
        docker-env docker-build docker-up docker-up-redis docker-down \
        docker-restart docker-logs docker-ps docker-shell docker-db-shell \
        docker-clean

## Показать список команд
help:
	@echo ""
	@echo "  Сборка и разработка"
	@echo "  -------------------------------------------------------------"
	@echo "  make restore             - восстановить NuGet-пакеты и локальные тулы"
	@echo "  make build               - собрать решение (Release)"
	@echo "  make run                 - запустить приложение локально"
	@echo "  make format              - форматирование кода"
	@echo "  make clean               - очистить bin/obj и TestResults"
	@echo ""
	@echo "  Тесты"
	@echo "  -------------------------------------------------------------"
	@echo "  make test                - все тесты"
	@echo "  make test-unit           - только unit-тесты"
	@echo "  make test-integration    - только integration-тесты"
	@echo "  make coverage            - тесты с покрытием + HTML-отчёт"
	@echo "  make open-coverage       - открыть HTML-отчёт в браузере"
	@echo ""
	@echo "  Docker"
	@echo "  -------------------------------------------------------------"
	@echo "  make docker-env          - создать .env из .env.example (если нет)"
	@echo "  make docker-build        - собрать образ"
	@echo "  make docker-up           - запустить postgres + app (без Redis)"
	@echo "  make docker-up-redis     - запустить postgres + redis + app"
	@echo "  make docker-down         - остановить контейнеры"
	@echo "  make docker-restart      - перезапустить контейнеры"
	@echo "  make docker-logs         - следить за логами"
	@echo "  make docker-ps           - статус контейнеров"
	@echo "  make docker-shell        - shell внутри контейнера приложения"
	@echo "  make docker-db-shell     - psql внутри контейнера postgres"
	@echo "  make docker-clean        - остановить и удалить volume'ы и образ"
	@echo ""

# ============================================================
#  Сборка и разработка
# ============================================================

restore:
	$(DOTNET) restore $(SOLUTION)
	-$(DOTNET) tool restore

build: restore
	$(DOTNET) build $(SOLUTION) --no-restore --configuration $(CONFIGURATION)

run:
	$(DOTNET) run --project $(WEB_PROJECT)

format:
	$(DOTNET) format $(SOLUTION)

clean:
	$(DOTNET) clean $(SOLUTION) --configuration $(CONFIGURATION)
	-@if [ -d TestResults ]; then rm -rf TestResults; fi

# ============================================================
#  Тесты
# ============================================================

test: build
	$(DOTNET) test $(SOLUTION) --no-build --configuration $(CONFIGURATION)

test-unit: build
	$(DOTNET) test $(UNIT_TESTS) --no-build --configuration $(CONFIGURATION)

test-integration: build
	$(DOTNET) test $(INTEGRATION_TESTS) --no-build --configuration $(CONFIGURATION)

coverage: build
	$(DOTNET) test $(SOLUTION) --no-build --configuration $(CONFIGURATION) \
		--settings $(COVERLET_SETTINGS) \
		--collect:"XPlat Code Coverage" \
		--results-directory ./TestResults
	$(DOTNET) reportgenerator \
		-reports:"./TestResults/**/coverage.cobertura.xml" \
		-targetdir:"./$(REPORT_DIR)" \
		-reporttypes:"Html;MarkdownSummary" \
		-title:"Consolida — Code Coverage"
	@echo ""
	@echo "HTML-отчёт: $(REPORT_DIR)/index.html"
	@echo "Открыть:    make open-coverage"

open-coverage:
	@if [ -f "$(REPORT_DIR)/index.html" ]; then \
		case "$$(uname)" in \
			Darwin) open "$(REPORT_DIR)/index.html" ;; \
			MINGW*|MSYS*|CYGWIN*) start "" "$(REPORT_DIR)/index.html" ;; \
			*) xdg-open "$(REPORT_DIR)/index.html" ;; \
		esac \
	else \
		echo "Отчёт не найден. Сначала запустите: make coverage"; \
	fi

# ============================================================
#  Docker
# ============================================================

docker-env:
	@if [ ! -f .env ]; then \
		cp .env.example .env; \
		echo "Создан .env из .env.example."; \
		echo "Откройте и заполните POSTGRES_PASSWORD (и REDIS_PASSWORD, если используете Redis)."; \
	else \
		echo ".env уже существует, ничего не менял."; \
	fi

docker-build:
	docker compose build

docker-up: docker-env
	docker compose up -d --build
	@echo ""
	@echo "Приложение:  http://localhost:5000"
	@echo "Hangfire:    http://localhost:5000/hangfire (только с localhost)"
	@echo "Остановить:  make docker-down"

docker-up-redis: docker-env
	REDIS_HOST=redis docker compose --profile with-redis up -d --build
	@echo ""
	@echo "Запущено с Redis."
	@echo "Приложение:  http://localhost:5000"

docker-down:
	docker compose down

docker-restart:
	docker compose restart

docker-logs:
	docker compose logs -f

docker-ps:
	docker compose ps

docker-shell:
	docker compose exec app /bin/sh

docker-db-shell:
	docker compose exec postgres psql -U postgres -d consolida

docker-clean:
	docker compose down -v --rmi local
	@echo ""
	@echo "Контейнеры, volume'ы и локальные образы удалены."