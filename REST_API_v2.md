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
| GET | `/api/v2/Person/GetList?pageIndex=0&pageSize=10` | Список сотрудников (в ответе tabnum) |
| POST | `/api/v2/Person` | Добавить сотрудника |
| PATCH | `/api/v2/Person` | Изменить сотрудника, найденного по tabnum |
| DELETE | `/api/v2/Person?tabnum=tn_123` | Удалить сотрудника |
| POST | `/api/v2/Person/AddAccess` | Добавить сотруднику категорию доступа |
| POST | `/api/v2/Person/AddIdentifier` | Добавить сотруднику идентификатор |
| DELETE | `/api/v2/Person/DeleteIdentifier?idCard=...` | Удалить идентификатор по номеру карты |
| GET | `/api/v2/Identifier/GetList?pageIndex=0&pageSize=10&identifire=` | Список идентификаторов (в ответе tabnum) |
| POST | `/api/v2/Identifier` | Добавить идентификатор сотруднику |
| DELETE | `/api/v2/Identifier/DeleteIdentifier?idCard=...` | Удалить идентификатор |
| GET | `/api/v2/IdentifierType/GetList` | Список типов идентификаторов |
| GET | `/api/v2/Access/GetList` | Список категорий доступа |
| POST | `/api/v2/Access` | Добавить категорию доступа |
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

Организации описаны отдельно: [docs/ORGANIZATIONS_API_v2.md](docs/ORGANIZATIONS_API_v2.md).

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

## Коды ответов

| Код | Когда возвращается |
|---|---|
| 200 | Успех |
| 400 | Ошибка валидации, пустой/слишком длинный tabnum, неверный период дат |
| 404 | Сотрудник с указанным tabnum не найден |
| 409 | Дубликат tabnum или несколько сотрудников с одним tabnum |
| 500 | Ошибка базы данных или внутренняя ошибка |

## Совместимость

* v1 продолжает работать по прежним адресам и по-прежнему использует `id_pep`.
* В ответах v2 поле `id_pep` не возвращается.
* В событиях v2 вместо `ess1` (id_pep) возвращается `tabnum`.
