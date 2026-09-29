.DEFAULT_GOAL := help

DOTNET            ?= dotnet
SOLUTION          := Consolida.slnx
WEB_PROJECT       := Consolida
UNIT_TESTS        := Tests/Consolida.UnitTests
INTEGRATION_TESTS := Tests/Consolida.IntegrationTests
CONFIGURATION     := Release

.PHONY: help restore build run test test-unit test-integration coverage \
        format clean docker-build docker-up docker-down docker-logs docker-clean

## Показать список команд
help:
	@echo "Доступные команды:"
	@echo "  make restore             - восстановить NuGet-пакеты"
	@echo "  make build               - собрать решение (Release)"
	@echo "  make run                 - запустить приложение локально"
	@echo "  make test                - все тесты"
	@echo "  make test-unit           - только unit-тесты"
	@echo "  make test-integration    - только integration-тесты"
	@echo "  make coverage            - тесты с покрытием"
	@echo "  make format              - форматирование кода"
	@echo "  make clean               - очистить bin/obj"
	@echo "  make docker-build        - собрать Docker-образ"
	@echo "  make docker-up           - поднять docker compose"
	@echo "  make docker-down         - остановить docker compose"
	@echo "  make docker-logs         - логи docker compose"
	@echo "  make docker-clean        - остановить и удалить volume'ы"

restore:
	$(DOTNET) restore $(SOLUTION)

build: restore
	$(DOTNET) build $(SOLUTION) --no-restore --configuration $(CONFIGURATION)

run:
	$(DOTNET) run --project $(WEB_PROJECT)

test: build
	$(DOTNET) test $(SOLUTION) --no-build --configuration $(CONFIGURATION)

test-unit: build
	$(DOTNET) test $(UNIT_TESTS) --no-build --configuration $(CONFIGURATION)

test-integration: build
	$(DOTNET) test $(INTEGRATION_TESTS) --no-build --configuration $(CONFIGURATION)

coverage: build
	$(DOTNET) test $(SOLUTION) --no-build --configuration $(CONFIGURATION) \
		--collect:"XPlat Code Coverage" \
		--results-directory ./TestResults
	@echo ""
	@echo "Coverage-файлы: ./TestResults/**/coverage.cobertura.xml"
	@echo "HTML-отчёт:     reportgenerator -reports:./TestResults/**/coverage.cobertura.xml -targetdir:./TestResults/CoverageReport -reporttypes:Html"

format:
	$(DOTNET) format $(SOLUTION)

clean:
	$(DOTNET) clean $(SOLUTION) --configuration $(CONFIGURATION)

docker-build:
	docker compose build

docker-up:
	docker compose up -d --build

docker-down:
	docker compose down

docker-logs:
	docker compose logs -f

docker-clean:
	docker compose down -v