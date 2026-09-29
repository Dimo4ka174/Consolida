FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Сначала только csproj — restore кэшируется отдельно от исходников.
COPY Consolida.slnx ./
COPY Consolida/Consolida.csproj Consolida/
COPY Application/Application.csproj Application/
COPY DB/DB.csproj DB/
COPY Tests/Consolida.UnitTests/Consolida.UnitTests.csproj Tests/Consolida.UnitTests/
COPY Tests/Consolida.IntegrationTests/Consolida.IntegrationTests.csproj Tests/Consolida.IntegrationTests/

RUN dotnet restore Consolida.slnx

COPY . .

WORKDIR /src/Consolida
RUN dotnet publish Consolida.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

# curl для healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

# Непривилегированный пользователь.
RUN groupadd -r app && useradd -r -g app app

WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

RUN mkdir -p /app/logs /app/DataProtection-Keys \
    && chown -R app:app /app/logs /app/DataProtection-Keys

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "Consolida.dll"]