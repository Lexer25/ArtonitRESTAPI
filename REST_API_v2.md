# REST API v2: идентификация сотрудника по tabnum

Версия v2 работает параллельно с v1. Все методы v1 (`/Person`, `/Identifier`,
`/IdentifierType`, `/Access`, `/api/Event`) сохранены без изменений.

Отличие v2: вместо внутреннего `id_pep` во входных и выходных данных используется
табельный номер сотрудника `tabnum`.

Базовый префикс v2: `/api/v2`.

## Swagger

Swagger разделён на два документа:

| Версия | UI | JSON |
|---|---|---|
| v1 | `http://localhost:5056/swagger/index.html` (выбрать «Artonit REST API v1») | `/swagger/v1/swagger.json` |
| v2 | `http://localhost:5056/swagger/index.html` (выбрать «Artonit REST API v2») | `/swagger/v2/swagger.json` |

В правом верхнем углу Swagger UI есть переключатель между документами v1 и v2.
В документе v1 только маршруты v1, в документе v2 только маршруты `/api/v2/*`.

## Правила работы с tabnum

* `tabnum` — строка, максимум 50 символов; пробелы по краям обрезаются.
* При добавлении сотрудника (`POST /api/v2/Person`) поле `tabnum` необязательно.
  * Если `tabnum` передан, он сохраняется как есть (после обрезки пробелов).
  * Если `tabnum` отсутствует, пустой или состоит из пробелов, API резервирует
    `id_pep` генератором `gen_people_id` и формирует `tabnum` по правилу
    `tn_<id_pep>`. Например, для `id_pep = 123` будет создан `tabnum = "tn_123"`.
* API не позволяет создать второго сотрудника с уже существующим `tabnum`
  (ответ `409 Conflict`).
* Если в базе уже есть несколько сотрудников с одинаковым `tabnum`, методы,
  которым нужен один сотрудник, возвращают `409 Conflict` с просьбой обратиться
  к администратору.
* Уникальность `tabnum` проверяется на уровне API. В таблице `PEOPLE` нет
  уникального индекса по `TABNUM`, поэтому при параллельных запросах возможна
  гонка. Для строгой гарантии и ускорения поиска по `tabnum` добавьте уникальный
  индекс. Перед этим проверьте дубликаты:

  ```sql
  SELECT TABNUM, COUNT(*) FROM PEOPLE GROUP BY TABNUM HAVING COUNT(*) > 1;

  ALTER TABLE PEOPLE ADD CONSTRAINT UNQ_PEOPLE_TABNUM UNIQUE (TABNUM);
  CREATE INDEX PEOPLE_IDX_ID_ORG ON PEOPLE (ID_ORG);
  ```

  `UNQ_PEOPLE_TABNUM` закрывает гонку и ускоряет `WHERE TABNUM = ?`.
  `PEOPLE_IDX_ID_ORG` ускоряет выборки по организации и каскадные операции.
  Если планируется несколько `ID_DB`, уникальность делайте по
  `(TABNUM, ID_DB)`, а в API добавьте фильтр по `ID_DB`.

## Методы

| Метод | Маршрут v2 | Назначение |
|---|---|---|
| GET | `/api/v2/Person?tabnum=tn_123` | Получить сотрудника по tabnum |
| GET | `/api/v2/Person/GetList?pageIndex=0&pageSize=10&active=` | Список сотрудников (в ответе tabnum); `active` фильтрует по `ACTIVE` (с версии 1.7.0) |
| GET | `/api/v2/Person/GetCount` | Количество всех, активных и неактивных сотрудников (с версии 1.7.0) |
| POST | `/api/v2/Person` | Добавить сотрудника |
| PATCH | `/api/v2/Person` | Изменить сотрудника, найденного по tabnum |
| PUT | `/api/v2/Person` | Заменить ФИО сотрудника (с версии 1.6.0) |
| DELETE | `/api/v2/Person?tabnum=tn_123` | Удалить сотрудника |
| POST | `/api/v2/Person/AddAccess` | Добавить сотруднику категорию доступа |
| GET | `/api/v2/Person/Access?tabnum=tn_123` | Набор категорий доступа сотрудника (с версии 1.6.0) |
| PUT | `/api/v2/Person/Access` | Заменить набор категорий доступа сотрудника (с версии 1.6.0) |
| POST | `/api/v2/Person/AddIdentifier` | Добавить сотруднику идентификатор |
| DELETE | `/api/v2/Person/DeleteIdentifier?idCard=...` | Удалить идентификатор по номеру карты |
| GET | `/api/v2/Identifier/GetCount` | Количество записей `CARD` по фильтрам (всего/активных/неактивных, с версии 1.8.0) |
| GET | `/api/v2/Identifier/GetList?pageIndex=0&pageSize=10&identifire=` | Список идентификаторов по фильтрам (в ответе tabnum, с версии 1.8.0) |
| POST | `/api/v2/Identifier` | Добавить идентификатор сотруднику |
| DELETE | `/api/v2/Identifier/DeleteIdentifier?idCard=...` | Удалить идентификатор |
| GET | `/api/v2/IdentifierType/GetList` | Список типов идентификаторов |
| GET | `/api/v2/Access/GetList` | Список категорий доступа |
| POST | `/api/v2/Access` | Добавить категорию доступа |
| DELETE | `/api/v2/Access/{id_accessname}` | Удалить категорию доступа |
| GET | `/api/v2/Access/GetPeople?id_accessname=1` | Сотрудники с категорией доступа (tabnum) |
| GET | `/api/v2/Event/passIdDg` | События сотрудника по группе устройств |
| GET | `/api/v2/Event/passIdAp` | События сотрудника по точке прохода |
| GET | `/api/v2/Event/passAllDG` | Все события по группе устройств |
| GET | `/api/v2/Event/passAllAp` | Все события по точке прохода |
| POST | `/api/v2/organizations` | Добавить организацию |
| GET | `/api/v2/organizations/{divcode}` | Получить организацию |
| GET | `/api/v2/organizations` | Список организаций с фильтрами и пагинацией |
| PUT | `/api/v2/organizations/{divcode}` | Полностью заменить изменяемые поля организации |
| PATCH | `/api/v2/organizations/{divcode}` | Частично изменить организацию |
| DELETE | `/api/v2/organizations/{divcode}` | Удалить организацию |
| GET | `/api/v2/organizations/{divcode}/accessnames` | Список id_accessname категорий доступа организации |
| PUT | `/api/v2/organizations/{divcode}/accessnames` | Сохранить (полностью заменить) набор категорий доступа организации |

Организации описаны отдельно: [docs/ORGANIZATIONS_API_v2.md](docs/ORGANIZATIONS_API_v2.md).
Категории доступа описаны отдельно: [docs/ACCESS_API_v2.md](docs/ACCESS_API_v2.md).
ФИО сотрудника и его набор категорий доступа: [docs/PEOPLE_API_v2.md](docs/PEOPLE_API_v2.md).
Идентификаторы, статистика и фильтры по таблице CARD: [docs/IDENTIFIERS_API_v2.md](docs/IDENTIFIERS_API_v2.md).

## Примеры

### Добавить сотрудника без tabnum

```http
POST /api/v2/Person
Content-Type: application/json

{
  "surname": "Иванов",
  "name": "Иван",
  "patronymic": "Иванович"
}
```

Ответ:

```json
{
  "tabnum": "tn_123"
}
```

### Добавить сотрудника с явным tabnum

```http
POST /api/v2/Person
Content-Type: application/json

{
  "surname": "Петров",
  "name": "Пётр",
  "patronymic": "Петрович",
  "tabnum": "T-100"
}
```

Ответ:

```json
{
  "tabnum": "T-100"
}
```

### Получить сотрудника

```http
GET /api/v2/Person?tabnum=T-100
```

Ответ (массив, как в v1):

```json
[
  {
    "tabnum": "T-100",
    "surname": "Петров",
    "name": "Пётр",
    "patronymic": "Петрович"
  }
]
```

### Получить статистику по сотрудникам (1.7.0)

```http
GET /api/v2/Person/GetCount
```

Ответ:

```json
{
  "total": 79,
  "active": 79,
  "inactive": 0
}
```

`total` — все записи `PEOPLE`, `active` — записи с `ACTIVE = 1`,
`inactive` — записи с `ACTIVE = 0`.

### Получить только активных или только неактивных сотрудников (1.7.0)

```http
GET /api/v2/Person/GetList?pageIndex=0&pageSize=10&active=1
GET /api/v2/Person/GetList?pageIndex=0&pageSize=10&active=0
```

`active` принимает `1`/`true`/`active` (только `ACTIVE = 1`),
`0`/`false`/`inactive` (только `ACTIVE = 0`) и `all`/пусто (все записи).
Структура ответа не изменилась: `context`, `pageIndex`, `pageSize`, `pageCount`.

### Добавить идентификатор

```http
POST /api/v2/Identifier
Content-Type: application/json

{
  "id_card": "A1B2C3D4",
  "tabnum": "T-100",
  "id_cardtype": 1
}
```

### Получить статистику по идентификаторам (1.8.0)

```http
GET /api/v2/Identifier/GetCount?id_cardtype=1&active=1
```

```json
{
  "total": 12,
  "active": 10,
  "inactive": 2
}
```

`total` — все записи `CARD` после фильтров, `active` — из них с
`ACTIVE = 1`, `inactive` — с `ACTIVE = 0`.

### Получить список идентификаторов с фильтрами (1.8.0)

```http
GET /api/v2/Identifier/GetList?pageIndex=0&pageSize=10&tabnum=T-100&id_cardtype=1&active=1
```

Поддерживаются фильтры `tabnum`, `timestart`, `timeend`, `active`,
`id_cardtype`, `createdat` и прежний `identifire` (номер карты).
Если передана только дата, фильтр по `timestart`/`timeend`/`createdat`
охватывает весь день; дата-время сравнивается точно. Подробности и примеры —
в [docs/IDENTIFIERS_API_v2.md](docs/IDENTIFIERS_API_v2.md).

### Добавить категорию доступа

```http
POST /api/v2/Access
Content-Type: application/json

{
  "name": "Тестовая категория"
}
```

Ответ:

```json
{
  "id_accessname": 2,
  "name": "Тестовая категория"
}
```

### Удалить категорию доступа

```http
DELETE /api/v2/Access/2
```

Ответ:

```json
{
  "id_accessname": 2,
  "name": "Тестовая категория"
}
```

### Получить сотрудников с категорией доступа

```http
GET /api/v2/Access/GetPeople?id_accessname=1&pageIndex=0&pageSize=10
```

Ответ (в `context` только `tabnum`):

```json
{
  "context": [
    { "tabnum": "tn_SysAdmin" }
  ],
  "pageIndex": 0,
  "pageSize": 10,
  "pageCount": 1
}
```

### Получить события сотрудника по группе устройств

```http
GET /api/v2/Event/passIdDg?tabnum=T-100&fromDate=2026-01-01%2000:00:00&toDate=2026-12-31%2023:59:59&deviceGroupName=Все%20устройства
```

Ответ:

```json
{
  "context": [
    {
      "tabnum": "T-100",
      "timestamp": "сб 21.11.2026 21:49:27"
    }
  ]
}
```

### Изменить сотрудника

```http
PATCH /api/v2/Person
Content-Type: application/json

{
  "tabnum": "T-100",
  "id_org": 2
}
```

### Удалить сотрудника

```http
DELETE /api/v2/Person?tabnum=T-100
```

### Заменить ФИО сотрудника (1.6.0)

```http
PUT /api/v2/Person
Content-Type: application/json

{
  "tabnum": "T-100",
  "surname": "Петров-Водкин",
  "name": "Алексей",
  "patronymic": "Иванович"
}
```

Ответ — сохранённые значения:

```json
{
  "tabnum": "T-100",
  "surname": "Петров-Водкин",
  "name": "Алексей",
  "patronymic": "Иванович"
}
```

Фамилия обязательна, имя и отчество могут быть пустыми. Табельный номер
неизменяем; организация, карты и категории не затрагиваются.

### Получить набор категорий доступа сотрудника (1.6.0)

```http
GET /api/v2/Person/Access?tabnum=T-100
```

Ответ — массив идентификаторов `id_accessname` по возрастанию:

```json
[0, 1]
```

### Заменить набор категорий доступа сотрудника (1.6.0)

```http
PUT /api/v2/Person/Access
Content-Type: application/json

{
  "tabnum": "T-100",
  "accessnames": [0, 1]
}
```

Набор заменяется целиком: лишние связи снимаются, недостающие выдаются,
пустой массив очищает набор. Ответ — итоговый массив `id_accessname`.
Подробности — в [docs/PEOPLE_API_v2.md](docs/PEOPLE_API_v2.md).

## Коды ответов

| Код | Когда возвращается |
|---|---|
| 200 | Успех |
| 400 | Ошибка валидации, пустой/слишком длинный tabnum, неверный период дат |
| 401 | Нет или просрочен токен |
| 403 | Нет роли администратора для изменяющей команды |
| 404 | Сотрудник с указанным tabnum не найден |
| 409 | Дубликат tabnum или несколько сотрудников с одним tabnum |
| 422 | Категория доступа из набора отсутствует в ACCESSNAME |
| 500 | Ошибка базы данных или внутренняя ошибка |

## Совместимость

* v1 продолжает работать по прежним адресам и по-прежнему использует `id_pep`.
* В ответах v2 поле `id_pep` не возвращается.
* В событиях v2 вместо `ess1` (id_pep) возвращается `tabnum`.
* С версии 1.7.0 `GET /api/v2/Person/GetList` без `active` работает как раньше,
  а `GET /api/v2/Person/GetCount` возвращает статистику по `PEOPLE.ACTIVE`.
* С версии 1.8.0 `GET /api/v2/Identifier/GetList` без новых фильтров работает
  как раньше, а `GET /api/v2/Identifier/GetCount` возвращает статистику по
  `CARD`; в элементах списка добавлены `timestart`, `timeend`, `active` и
  `createdat`.
