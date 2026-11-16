# MarketPlace

[![CI](https://github.com/R1lix/MarketPlace/actions/workflows/ci.yml/badge.svg?branch=develop)](https://github.com/R1lix/MarketPlace/actions/workflows/ci.yml)

Учебный бэкенд интернет-магазина на микросервисах (.NET 9). Цель проекта — отработать навыки Middle C# Developer: Clean Architecture, DDD, REST API, EF Core, взаимодействие сервисов, тестирование.

## Статус

| Часть | Состояние |
|---|---|
| **Сервис Catalog** — товары, категории, остатки, картинки | ✅ готов |
| ├ Domain — сущности и бизнес-правила | ✅ |
| ├ Infrastructure — PostgreSQL, EF Core, миграции, оптимистическая блокировка | ✅ |
| ├ Application — сценарии, DTO, валидация (FluentValidation) | ✅ |
| └ Api — REST API, ProblemDetails, OpenAPI + Swagger UI | ✅ |
| Тесты: unit (xUnit + NSubstitute) | ✅ 140 |
| Тесты: интеграционные и HTTP (Testcontainers + PostgreSQL + WebApplicationFactory) | ✅ 76 |
| Docker: API + база одной командой | ✅ |
| CI — сборка, тесты, сборка Docker-образа в GitHub Actions | ✅ |
| Остальные сервисы (Identity, Orders, Inventory, Payment, Notifications) | 🗓 запланировано |

## Быстрый старт

Нужен [Docker Desktop](https://www.docker.com/products/docker-desktop/). Поднять весь backend — базу и Catalog API:

```bash
docker compose --profile api up -d --build
```

- Swagger UI: http://localhost:5080/swagger
- Здоровье: http://localhost:5080/health → `Healthy`
- Миграции применяются автоматически при старте контейнера

Остановить: `docker compose --profile api down` (данные сохраняются; `down -v` удалит и их).

## Разработка

Нужен [.NET SDK 9](https://dotnet.microsoft.com/download/dotnet/9.0) (9.0.312 или новее из линейки 9.0, см. `global.json`) и Docker Desktop.

```bash
dotnet tool restore      # локальные инструменты (dotnet-ef)
docker compose up -d     # только PostgreSQL (порт 5433)
dotnet ef database update -p src/Services/Catalog/Catalog.Infrastructure -s src/Services/Catalog/Catalog.Api
dotnet run --project src/Services/Catalog/Catalog.Api --launch-profile http
```

API: http://localhost:5203/swagger. Готовые запросы — в [`Catalog.Api.http`](src/Services/Catalog/Catalog.Api/Catalog.Api.http) (в Visual Studio: «Send request»).

Тесты:

```bash
dotnet test                               # все; интеграционные сами поднимают PostgreSQL в Docker
dotnet test tests/Catalog.UnitTests       # только быстрые unit-тесты, Docker не нужен
```

## API

| Метод | Адрес | Что делает | Ответ |
|---|---|---|---|
| GET | `/api/products?page&pageSize&categoryId&search` | список: страницы, фильтр по категории, поиск по названию и артикулу | 200 |
| GET | `/api/products/{id}` | карточка товара с картинками | 200 / 404 |
| POST | `/api/products` | создать товар | 201 + `Location` |
| PUT | `/api/products/{id}` | изменить название, описание, цену, категорию | 204 |
| POST | `/api/products/{id}/stock/add` | приход на склад `{ "quantity": 5 }` | 204 |
| POST | `/api/products/{id}/stock/remove` | списание со склада | 204 |
| POST | `/api/products/{id}/images` | добавить картинку (не больше 10) | 201 |
| DELETE | `/api/products/{id}/images/{imageId}` | удалить картинку | 204 |
| DELETE | `/api/products/{id}` | удалить товар (мягко; с остатком — нельзя) | 204 |
| GET | `/api/categories` | все категории | 200 |
| GET | `/api/categories/{id}` | категория | 200 / 404 |
| POST | `/api/categories` | создать категорию | 201 + `Location` |
| PUT | `/api/categories/{id}` | переименовать | 204 |
| DELETE | `/api/categories/{id}` | удалить (если нет товаров) | 204 / 409 |
| GET | `/health` | приложение и база работают | 200 / 503 |

Ошибки возвращаются в формате [ProblemDetails](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`):

| Код | Когда |
|---|---|
| 400 | неверный формат запроса (`errors` — ошибки по полям) или нарушено бизнес-правило (например, списать больше, чем есть) |
| 404 | товара, категории или картинки с таким Id нет |
| 409 | артикул занят, в категории есть товары, данные одновременно изменил другой запрос |
| 500 | неожиданная ошибка; подробности только в логах сервера |

## Структура

```
Shop.sln
Directory.Build.props        общие настройки проектов: net9.0, Nullable, ImplicitUsings
Directory.Packages.props     версии всех NuGet-пакетов (Central Package Management)
global.json                  версия .NET SDK
.config/dotnet-tools.json    локальные инструменты: dotnet-ef 9.0.23
docker-compose.yml           PostgreSQL (порт 5433) и, в профиле api, Catalog API (порт 5080)
src/Services/Catalog/
  Catalog.Domain/            сущности и бизнес-правила, без зависимостей
  Catalog.Application/       сценарии, DTO, валидация, интерфейсы репозиториев
  Catalog.Infrastructure/    EF Core, PostgreSQL, миграции, репозитории, запросы
  Catalog.Api/               контроллеры, обработка ошибок, OpenAPI, Dockerfile
tests/
  Catalog.UnitTests/         unit-тесты Domain и Application (без базы)
  Catalog.IntegrationTests/  интеграционные и HTTP-тесты: настоящий PostgreSQL в Docker
.github/workflows/ci.yml     CI: сборка, тесты, сборка Docker-образа
```

Зависимости направлены внутрь: `Api → Infrastructure → Application → Domain`. Domain ничего не знает о базе данных и вебе, Application — о EF Core.

## Как устроен Catalog

- **Product** — товар и корень агрегата: название, описание, цена, остаток, артикул, категория, картинки. Меняется только через бизнес-методы (`ChangeDetails`, `ChangePrice`, `ChangeCategory`, `AddStock`, `RemoveStock`, `AddImage`, `RemoveImage`, `Delete`).
- **ProductImage** — картинка товара; создаётся и удаляется только через `Product`, порядок задаёт `Position`.
- **Category** — отдельный агрегат; товар ссылается на неё по `CategoryId`.

Основные правила: цена и остаток не бывают отрицательными, у цены не больше 2 знаков после запятой, артикул хранится в верхнем регистре, уникален среди неудалённых товаров и не меняется после создания, удалённый товар нельзя изменять, товар с остатком на складе нельзя удалить, удаление мягкое.

Запрос проходит так: **контроллер** → **валидатор** (формат, все ошибки по полям сразу) → **сервис** (загрузить агрегат, проверить то, чего домен не знает: категория существует, артикул свободен) → **домен** (бизнес-правила) → **Unit of Work** (одна транзакция; ошибки базы — в ошибки приложения). Чтение списков идёт отдельно: сразу в DTO, без отслеживания изменений.

Одновременные изменения защищены оптимистической блокировкой (`xmin` в PostgreSQL): второй запрос получает 409, а не молча перезаписывает первый. `UpdatedAt` проставляется автоматически при сохранении.

## CI

Workflow [`ci.yml`](.github/workflows/ci.yml) запускается на каждый push и Pull Request в `develop` и `master`: сборка в Release (предупреждения считаются ошибками), все тесты (интеграционные — с PostgreSQL в Docker) и сборка Docker-образа API.

## План

1. ✅ **Catalog** — Domain → Infrastructure → Application → Api → тесты → Docker.
2. **Identity + API Gateway** — JWT, роли, YARP.
3. **Orders** — синхронные вызовы Catalog по HTTP с Polly, CQRS + MediatR.
4. **RabbitMQ + MassTransit** — события, сервис Notifications.
5. **Saga + Outbox** — Inventory и Payment, согласованность данных между сервисами.
6. **Наблюдаемость и CD** — Serilog, OpenTelemetry, публикация образов.
