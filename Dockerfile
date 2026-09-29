FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Consolida/Consolida.csproj Consolida/
COPY Application/Application.csproj Application/
COPY DB/DB.csproj DB/

RUN dotnet restore Consolida/Consolida.csproj

COPY . .

WORKDIR /src/Consolida
RUN dotnet publish Consolida.csproj \
    -c Release \
    -o /app/publish \
    --no-restore \
    /p:UseAppHost=false

# ---------- Stage 2: Runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime

RUN apt-get update \
    && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

RUN groupadd -f -r app \
    && id -u app >/dev/null 2>&1 || useradd -r -g app app

WORKDIR /app
COPY --from=build --chown=app:app /app/publish .

RUN mkdir -p /app/logs /app/DataProtection-Keys \
    && chown -R app:app /app/logs /app/DataProtection-Keys

USER app
EXPOSE 8080

HEALTHCHECK --interval=30s --timeout=10s --start-period=60s --retries=3 \
    CMD curl -f http://localhost:8080/ || exit 1

ENTRYPOINT ["dotnet", "Consolida.dll"]