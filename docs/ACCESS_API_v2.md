# Категории доступа — REST API v2

Набор команд для управления категориями доступа. Базовый путь: `/api/v2/Access`.
Формат обмена: `application/json; charset=utf-8`.

Идентификатор категории — `id_accessname` (колонка `ACCESSNAME.ID_ACCESSNAME`).
API работает с одной базой: `ID_DB = 1`.

## Маршруты

| № | Команда | Метод | URI | Успех |
|---|---|---|---|---|
| 1 | Добавить категорию доступа | `POST` | `/api/v2/Access` | 200 |
| 2 | Удалить категорию доступа | `DELETE` | `/api/v2/Access/{id_accessname}` | 200 |
| 3 | Получить список категорий | `GET` | `/api/v2/Access/GetList` | 200 |
| 4 | Получить список сотрудников с категорией | `GET` | `/api/v2/Access/GetPeople` | 200 |

Команды используют те же правила доступа, что и остальные методы `/api/v2/Access`:
заголовок `Authorization` не требуется.

## 1. Добавить категорию доступа

```http
POST /api/v2/Access
Content-Type: application/json; charset=utf-8

{
  "name": "Тестовая категория"
}
```

Правила:

* `name` обязателен, обрезается от пробелов по краям;
* длина после обрезки — от 1 до 50 символов (`ACCESSNAME.NAME` — `VARCHAR(50)`);
* уникальность названия не проверяется: в базе допускаются одноимённые категории;
* `id_accessname` назначается генератором `GEN_ACCESSNAME_ID` и возвращается в ответе.

Ответ `200 OK`:

```json
{
  "id_accessname": 2,
  "name": "Тестовая категория"
}
```

## 2. Удалить категорию доступа

```http
DELETE /api/v2/Access/2
```

Ответ `200 OK`:

```json
{
  "id_accessname": 2,
  "name": "Тестовая категория"
}
```

Что происходит при удалении:

* удаляются правила доступа этой категории из таблицы `ACCESS`
  (в базе нет внешнего ключа `ACCESS → ACCESSNAME`, поэтому API делает это явно);
* связи с сотрудниками в `SS_ACCESSUSER` удаляются каскадно внешним ключом
  `FK_SS_ACCESSUSER_ID_ACCESSNAME`;
* связи с организациями в `SS_ACCESSORG` тоже удаляются каскадно
  (`FK_SS_ACCESSORG_1`, `ON DELETE CASCADE`); набор связей организации
  сохраняется методом `PUT /api/v2/organizations/{divcode}/accessnames`;
* категорию `id_accessname = 1` («вездеход») удалить нельзя: её защищает
  триггер `ACCESSNAME_BD` и дополнительная проверка API — ответ `409`;
* если категорию нельзя удалить из-за ограничений базы (например, на неё
  ссылаются карты или организации), возвращается `409` с текстом ошибки;
* категория `id_accessname = 0` (`Check Time`) не защищена API: попытка удаления
  выполняется, а решение принимает база данных.

## 3. Получить список категорий доступа

```http
GET /api/v2/Access/GetList?pageIndex=0&pageSize=10&AccessName=
```

| Параметр | Тип | По умолчанию | Описание |
|---|---|---|---|
| `pageIndex` | integer | 0 | Номер страницы, начиная с 0 |
| `pageSize` | integer | 10 | Размер страницы, от 1 до 1000 |
| `AccessName` | string | — | Подстрока названия; поиск без учёта регистра (`CONTAINING`) |

Ответ `200 OK`:

```json
{
  "context": [
    { "id_accessname": 0, "name": "Check Time" },
    { "id_accessname": 1, "name": "вездеход" }
  ],
  "pageIndex": 0,
  "pageSize": 10,
  "pageCount": 1
}
```

## 4. Получить список сотрудников с категорией

```http
GET /api/v2/Access/GetPeople?id_accessname=1&pageIndex=0&pageSize=10
```

| Параметр | Тип | Обязателен | Описание |
|---|---|---|---|
| `id_accessname` | integer | да | Идентификатор категории доступа |
| `pageIndex` | integer | нет | Номер страницы, начиная с 0 |
| `pageSize` | integer | нет | Размер страницы, от 1 до 1000 |

Ответ `200 OK`:

```json
{
  "context": [
    { "tabnum": "tn_SysAdmin" },
    { "tabnum": "T-100" }
  ],
  "pageIndex": 0,
  "pageSize": 10,
  "pageCount": 1
}
```

Правила:

* источник — `SS_ACCESSUSER`, сотрудник ищется по `ID_PEP` и `ID_DB`;
* в ответе только `tabnum` (`PEOPLE.TABNUM`), без `id_pep`;
* строки с пустым или `NULL` значением `TABNUM` не возвращаются;
* повторяющиеся `tabnum` (если в базе есть дубликаты) схлопываются через `DISTINCT`;
* если категория не найдена — `404`, если категория есть, но сотрудников нет —
  `200` с пустым `context`.

## Коды ответов

| Код | Когда возвращается |
|---|---|
| 200 | Успех |
| 400 | Пустое/слишком длинное название, отрицательный или отсутствующий `id_accessname`, ошибка SQL |
| 404 | Категория с указанным `id_accessname` не найдена |
| 409 | Попытка удалить защищённую категорию или нарушение ограничений базы |
| 500 | Внутренняя ошибка |

Ошибки этих методов возвращаются строкой (как у существующих методов
`/api/v2/Access`), например:

```text
Категория доступа с id_accessname=999 не найдена
```

## Соответствие базе данных

| API | База данных |
|---|---|
| `id_accessname` | `ACCESSNAME.ID_ACCESSNAME`; первичный ключ вместе с `ID_DB` |
| `name` | `ACCESSNAME.NAME` (`VARCHAR(50)`, `NOT NULL`) |
| — | `ACCESSNAME.ID_DB = 1` (одна база, как и в остальном API v2) |
| `tabnum` | `PEOPLE.TABNUM` через `SS_ACCESSUSER.ID_PEP` |
| — | `SS_ACCESSUSER (ID_ACCESSUSER, ID_DB, ID_PEP, ID_ACCESSNAME)`; ФИО и `tabnum` берутся из `PEOPLE` |

`ACCESSNAME.ID_ACCESSNAME` назначается триггером `ACCESSNAME_CREATE_ID`
из генератора `GEN_ACCESSNAME_ID`. API резервирует значение генератора заранее,
чтобы вернуть созданный идентификатор в ответе.

## Примеры команд

### curl.exe (Windows)

```bat
curl.exe -s "http://localhost:5056/api/v2/Access/GetList?pageIndex=0&pageSize=10"

curl.exe -s -X POST "http://localhost:5056/api/v2/Access" ^
  -H "Content-Type: application/json; charset=utf-8" ^
  --data-binary "{\"name\":\"Test access\"}"

curl.exe -s "http://localhost:5056/api/v2/Access/GetPeople?id_accessname=2&pageIndex=0&pageSize=100"

curl.exe -s -X DELETE "http://localhost:5056/api/v2/Access/2"
```

### PowerShell

```powershell
$base = 'http://localhost:5056'

# 1. Добавить категорию
$category = Invoke-RestMethod -Method Post -Uri "$base/api/v2/Access" `
    -ContentType 'application/json; charset=utf-8' `
    -Body ([Text.Encoding]::UTF8.GetBytes('{"name":"Test access"}'))

# 2. Список категорий
Invoke-RestMethod "$base/api/v2/Access/GetList?pageIndex=0&pageSize=10"

# 3. Сотрудники с категорией
Invoke-RestMethod "$base/api/v2/Access/GetPeople?id_accessname=$($category.id_accessname)"

# 4. Удалить категорию
Invoke-RestMethod -Method Delete -Uri "$base/api/v2/Access/$($category.id_accessname)"
```

## Проверка

Дымовой тест контракта — `docs/smoke-tests/access.ps1`:

```powershell
powershell -ExecutionPolicy Bypass -File docs\smoke-tests\access.ps1 `
    -BaseUrl http://localhost:5056
```

Скрипт создаёт и удаляет категорию `T-ACCESS-*` и сотрудника `T-ACC-*`,
поэтому запускать его следует на тестовой базе.
