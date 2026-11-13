# MarketPlace

[![CI](https://github.com/R1lix/MarketPlace/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/R1lix/MarketPlace/actions/workflows/ci.yml)

Учебный бэкенд интернет-магазина на микросервисах (.NET 9). Цель проекта — отработать навыки Middle C# Developer: Clean Architecture, DDD, REST API, EF Core, взаимодействие сервисов, тестирование.

## Статус

| Часть | Состояние |
|---|---|
| Catalog.Domain — сущности и бизнес-правила | ✅ готово |
| Unit-тесты Domain (xUnit) | ✅ 78 тестов |
| CI — сборка и тесты в GitHub Actions | ✅ |
| Catalog.Infrastructure — PostgreSQL, EF Core | ⏳ следующий шаг |
| Catalog.Application — сценарии, DTO, валидация | ⏳ |
| Catalog.Api — REST API | ⏳ |
| Остальные сервисы (Identity, Orders, Inventory, Payment, Notifications) | 🗓 запланировано |

## Структура

```
Shop.sln
Directory.Build.props        общие настройки проектов: net9.0, Nullable, ImplicitUsings
Directory.Packages.props     версии всех NuGet-пакетов (Central Package Management)
global.json                  версия .NET SDK
.config/dotnet-tools.json    локальные инструменты: dotnet-ef 9.0.23
docker-compose.yml           PostgreSQL для разработки (порт 5433)
src/Services/Catalog/
  Catalog.Domain/            сущности и бизнес-правила, без зависимостей
  Catalog.Application/       сценарии использования (зависит от Domain)
  Catalog.Infrastructure/    база данных (зависит от Application)
  Catalog.Api/               HTTP API (зависит от Application и Infrastructure)
tests/
  Catalog.UnitTests/         unit-тесты Domain (xUnit)
.github/workflows/ci.yml     CI: сборка и тесты на каждый push и Pull Request
```

Зависимости направлены внутрь: `Api → Infrastructure → Application → Domain`. Domain ничего не знает о базе данных и вебе.

## Модель Catalog

- **Product** — товар и корень агрегата: название, описание, цена, остаток, артикул, категория, картинки. Изменяется только через бизнес-методы (`ChangeDetails`, `ChangePrice`, `ChangeCategory`, `AddStock`, `RemoveStock`, `AddImage`, `RemoveImage`, `Delete`).
- **ProductImage** — картинка товара; создаётся и удаляется только через `Product` (не больше 10 на товар).
- **Category** — категория, отдельный агрегат; товар ссылается на неё по `CategoryId`.
- **DomainException** — нарушение бизнес-правила (в API станет ответом 400).

Основные правила: цена и остаток не бывают отрицательными, артикул хранится в верхнем регистре и не меняется после создания, удалённый товар нельзя изменять, товар с остатком на складе нельзя удалить, удаление мягкое (`IsDeleted`, `DeletedAt`).

## Запуск

Нужен [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) (версия 9.0.312 или новее из линейки 9.0, см. `global.json`).

```bash
dotnet tool restore      # локальные инструменты (dotnet-ef)
docker compose up -d     # PostgreSQL в Docker, нужен Docker Desktop
dotnet build
dotnet test
```

## CI

Workflow [`ci.yml`](.github/workflows/ci.yml) запускается на каждый push и Pull Request в `develop` и `master`: восстанавливает пакеты, собирает решение в Release (предупреждения считаются ошибками) и прогоняет тесты. Результаты — на вкладке **Actions** репозитория.


## План

1. **Catalog** — Domain → Infrastructure (PostgreSQL, EF Core) → Application → Api → тесты.
2. **Identity + API Gateway** — JWT, роли, YARP.
3. **Orders** — синхронные вызовы Catalog по HTTP с Polly, CQRS + MediatR.
4. **RabbitMQ + MassTransit** — события, сервис Notifications.
5. **Saga + Outbox** — Inventory и Payment, согласованность данных между сервисами.
6. **Наблюдаемость и CI** — Serilog, OpenTelemetry, Health Checks, Docker Compose, GitHub Actions.
