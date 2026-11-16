# Образ Catalog API. Собирается из корня репозитория:
#   docker build -f src/Services/Catalog/Catalog.Api/Dockerfile -t marketplace/catalog-api .
# Многоэтапная сборка: в итоговый образ попадает только опубликованное приложение и рантайм, без SDK и исходников

# ---------- Сборка ----------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Сначала только файлы проектов и общие настройки — восстановление пакетов кэшируется Docker
# и не повторяется, пока не меняются зависимости. global.json не копируем: в образе своя версия SDK 9.0
COPY Directory.Build.props Directory.Packages.props ./
COPY src/Services/Catalog/Catalog.Domain/Catalog.Domain.csproj src/Services/Catalog/Catalog.Domain/
COPY src/Services/Catalog/Catalog.Application/Catalog.Application.csproj src/Services/Catalog/Catalog.Application/
COPY src/Services/Catalog/Catalog.Infrastructure/Catalog.Infrastructure.csproj src/Services/Catalog/Catalog.Infrastructure/
COPY src/Services/Catalog/Catalog.Api/Catalog.Api.csproj src/Services/Catalog/Catalog.Api/
RUN dotnet restore src/Services/Catalog/Catalog.Api/Catalog.Api.csproj

COPY src/ src/
RUN dotnet publish src/Services/Catalog/Catalog.Api/Catalog.Api.csproj \
    --configuration Release --no-restore --output /app/publish /p:UseAppHost=false

# ---------- Запуск ----------
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Не от root: пользователь app (APP_UID) уже есть в официальном образе
USER $APP_UID
# В образах .NET 8+ приложение по умолчанию слушает порт 8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Catalog.Api.dll"]
