# Паттерны и технические решения

Коротко — что использовано и почему. Архитектура — в
[`ARCHITECTURE.md`](ARCHITECTURE.md), известные компромиссы — в
[`TRADE-OFFS.md`](TRADE-OFFS.md).

---

## 1. Repository
**Где:** `Application/DataAccessLayer/Service/Common/GenericRepository.cs`.

Абстракция над `DbSet<T>`. Сервисы не работают с `AppDbContext` напрямую,
легко мокать в unit-тестах. Soft delete фильтруется в `GetQueryable()`.

`IQueryable` **не скрывается** — это осознанно. Заводить методы вроде
`GetActiveOrdersByCustomer` — плодить десятки методов на каждый фильтр.
Сервисы используют LINQ, тесты — `MockQueryable`.

---

## 2. Unit of Work
**Где:** `UnitOfWork.cs`.

Один `DbContext` на HTTP-запрос, общий для всех репозиториев.
`ExecuteInTransactionAsync` проверяет `HasActiveTransaction` и не создаёт
вложенную транзакцию (Postgres их не поддерживает).

---

## 3. Generic Service
**Где:** `GenericService<TEntity, TDto>`.

Базовый CRUD: `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`,
`GetPagedAsync`. Точки расширения — `virtual`. Устраняет дублирование
в 10+ сервисах справочников.

Пример переопределения (`ManufacturerService.GetPagedAsync`):
работает через кэш вместо прямого запроса в БД.

---

## 4. Service Factory
**Где:** `ServiceFactory.cs`.

Единая точка регистрации сервисов. `Program.cs` не разрастается.
Сгруппировано по доменам: DataAccess, ReferenceServices, OrderServices,
Calculation, Excel, Notification, Consolidation.

Интерфейс `IServiceFactory` не заводился: класс вызывается один раз
в `Program.cs`, DI его не резолвит.

---

## 5. Strategy — кэширование
**Где:** `ICacheStrategy<T>`, `MemoryCacheStrategy<T>`, `RedisCacheStrategy<T>`.

Выбор — на старте приложения через `ServiceExtensions.AddCustomCache`:
- `REDIS_HOST` пусто → `MemoryCacheStrategy`
- иначе → `RedisCacheStrategy`

`CacheService<T>` — обёртка над стратегией, знает `IUnitOfWork`
и умеет инвалидировать кэш после изменения.

---

## 6. Facade — кэш-сервисы
**Где:** `CacheService/CityCacheService.cs`, `CompanyCacheService.cs`, и т.д.

Каждый — тонкий фасад под один ключ и одно время жизни. Не один
`GenericCacheService<T>`, потому что у сущностей разные TTL (города — час,
производители — 12 часов) и разные ключи.

---

## 7. Soft delete
Все сущности реализуют `IEntity` с `IsDeleted`. `GetQueryable()` по умолчанию
фильтрует. Уникальные индексы — составные: `(Name, IsDeleted)`.

---

## 8. Cascade soft delete
**Где:** `CascadeSoftDeleteService.cs`.

Граф: `City → Company → Customer`, `Manufacturer → Product`,
`MeasureUnit → TaxType → OrderTaxProduct`, `Order → OrderProduct/OrderTax/...`.

Транзакция + bulk-update через `ExecuteUpdateAsync` (работает в обход
`ChangeTracker` — в тестах лечится `ChangeTracker.Clear()`).

---

## 9. Optimistic concurrency
`ConsolidationPool.Version` → `IsRowVersion()` → Postgres `xmin`.
При `UPDATE` Postgres меняет `xmin` автоматически; EF ловит конфликт.
Sqlite (тесты) не поддерживает — конфигурация под `Database.IsNpgsql()`.

---

## 10. TTL-локи
`LockService.cs`. Поля `LockedByUserId` + `LockedAt` на `Order` и
`ConsolidationPool`. TTL — 5 минут. `LockCleanupJob` — каждые 15 минут.
Сервер отдаёт 409 при попытке работать с чужой блокировкой.

---

## 11. SignalR Hub + Notifier
`ConsolidationHub` — транспорт. `IConsolidationNotifier` — абстракция
для сервисов, чтобы они не зависели от `IHubContext<>` напрямую
и легко мокались в тестах.

---

## 12. Background jobs
Hangfire + PostgreSQL storage. Два recurring job'а (см. ARCHITECTURE).

---

## 13. DI Composition Root
`Program.cs` + `ServiceExtensions.cs` + `ServiceFactory.cs`.
Всё сгруппировано по расширениям: `AddCustomDatabaseAndIdentity`,
`AddCustomCache`, `AddCustomAuthorization` и т.д.

---

## 14. Audit timestamps
`AppDbContext.SaveChangesAsync` проставляет `CreatedDate` для
`ICreatedAtEntity` при `Added`, если дата не задана.

Ограничение: `ExecuteUpdateAsync` работает в обход — для bulk-операций
проставлять вручную.

---

## 15. FilterService на Expression Trees
**Где:** `FilterService<T>` + `FilterParams` / `FilterCondition`.

Строит `Expression<Func<T, bool>>` по строковому пути свойства
(`"City.Name"`, `"MeasureUnit.Name"`) и оператору (`==`, `>=`, `<`, ...).
Сортировка — тоже через выражения.

Зачем: один сервис обслуживает все справочники и списки, не нужно
писать `switch` по названию сортировки в каждом контроллере.

Для строковых `==` используется регистронезависимый `Contains` через
`ToUpper()`. Для enum поддерживается парсинг по имени и по числу.

---

## 16. Quick Create
**Где:** `IQuickCreateService`, `QuickCreateService.cs`.

Позволяет создать компанию / клиента / производителя «на лету» из формы
заказа, без перехода в справочник. Возвращает DTO с `Success` + `Message`.
