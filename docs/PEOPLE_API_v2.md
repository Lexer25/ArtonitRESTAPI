# Сотрудники в REST API v2: ФИО, категории доступа и статистика (версия 1.7.0)

Документ описывает методы v2, добавленные в версиях сервиса **1.6.0** и **1.7.0**:

| Метод | Маршрут | Назначение |
|---|---|---|
| PUT | `/api/v2/Person` | Полностью заменить ФИО сотрудника, найденного по `tabnum` |
| GET | `/api/v2/Person/Access?tabnum=...` | Получить набор категорий доступа сотрудника |
| PUT | `/api/v2/Person/Access` | Полностью заменить набор категорий доступа сотрудника |
| GET | `/api/v2/Person/GetCount` | Количество всех, активных и неактивных сотрудников |
| GET | `/api/v2/Person/GetList?active=...` | Список сотрудников с фильтром по `ACTIVE` |

До 1.6.0 изменить ФИО и снять категорию доступа у сотрудника через API было
нельзя: `PATCH /api/v2/Person` меняет только `id_db` и `id_org`, а
`POST /api/v2/Person/AddAccess` только добавляет одну категорию. Теперь обе
операции доступны и выполняются в той же базе Firebird.

В версии **1.7.0** добавлены статистика по таблице `PEOPLE` и фильтр
активности в списке сотрудников. Статистика и фильтр работают с колонкой
`PEOPLE.ACTIVE`: активный сотрудник — `ACTIVE = 1`, неактивный — `ACTIVE = 0`.

В методах, работающих с одним сотрудником, сотрудник определяется табельным
номером `tabnum` (`PEOPLE.TABNUM`), как и в остальных методах v2. API работает
с базой `ID_DB = 1`; статистика и список сотрудников при этом считают все
записи таблицы `PEOPLE`.

## Авторизация

`PUT`-методы изменяющие, поэтому требуют токен с ролью администратора
(заголовок `Authorization: Bearer <token>`, токен выдаёт
`GET /login?username=ADMIN&pasword=333`). Без токена — `401`, с токеном
обычного пользователя — `403`.

`GET /api/v2/Person/Access` доступен так же, как остальные GET-методы
сотрудников.

## GET /api/v2/Person/GetCount — статистика по таблице PEOPLE

Возвращает три количества одним запросом:

```http
GET /api/v2/Person/GetCount
```

```json
{
  "total": 79,
  "active": 79,
  "inactive": 0
}
```

| Поле | Что означает |
|---|---|
| `total` | Количество всех записей в таблице `PEOPLE` (`COUNT(*)`) |
| `active` | Количество записей, у которых `PEOPLE.ACTIVE = 1` |
| `inactive` | Количество записей, у которых `PEOPLE.ACTIVE = 0` |

Правила:

* Статистика считается по всей таблице `PEOPLE`, без фильтра по `ID_DB`,
  организации и без учёта пагинации.
* Если в базе встретится запись с `ACTIVE IS NULL`, она войдёт в `total`, но
  не войдёт ни в `active`, ни в `inactive`.
* Метод только читает данные и не требует токена, как остальные GET-методы
  сотрудников.
* Ошибка базы данных — `500 INTERNAL_ERROR`.

## GET /api/v2/Person/GetList — список сотрудников

Возвращает страницу сотрудников и поддерживает фильтр по признаку активности:

```http
GET /api/v2/Person/GetList?pageIndex=0&pageSize=10&active=1
```

Параметр `active`:

| Значение | Что возвращается |
|---|---|
| не указан, пусто, `all`, `any` | Все записи таблицы `PEOPLE` |
| `1`, `true`, `yes`, `active` | Только записи с `PEOPLE.ACTIVE = 1` |
| `0`, `false`, `no`, `inactive` | Только записи с `PEOPLE.ACTIVE = 0` |
| любое другое | `400 VALIDATION_ERROR`, поле `active` |

Ответ — та же структура `Pagination`, что и раньше; `pageCount` считается от
количества записей после фильтра:

```json
{
  "context": [
    {
      "tabnum": "T-100",
      "surname": "Петров",
      "name": "Пётр",
      "patronymic": "Петрович"
    }
  ],
  "pageIndex": 0,
  "pageSize": 10,
  "pageCount": 1
}
```

Правила:

* `pageIndex` — номер страницы с нуля; отрицательное значение приводится к 0.
* `pageSize` — от 1 до 1000; `0`, отрицательное значение или отсутствие
  параметра дают 10, больше 1000 — 1000.
* Записи упорядочены по `ID_PEP` по возрастанию, поэтому страницы не
  пересекаются.
* В элементах `context` возвращаются `tabnum`, `surname`, `name`,
  `patronymic` — как у остальных методов v2; `id_pep` не возвращается.
* Без параметра `active` поведение метода не изменилось: возвращаются все
  сотрудники.
* Метод только читает данные и не требует токена.
* Ошибка базы данных — `500 INTERNAL_ERROR`.

## PUT /api/v2/Person — замена ФИО

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

Правила:

* `tabnum` обязателен, обрезается от пробелов, до 50 символов; сотрудник
  ищется по нему (`404`, если не найден, `409`, если номер неоднозначен).
* `surname` обязательна: пустая фамилия не сохраняется (`400
  VALIDATION_ERROR`, поле `surname`).
* `name` и `patronymic` могут быть пустыми; поля обрезаются от пробелов.
* Все три поля — до 50 символов (как колонки `PEOPLE`).
* Меняются только `SURNAME`, `NAME`, `PATRONYMIC` и `TIME_STAMP`; остальные
  реквизиты сотрудника, его организация, карты и категории не затрагиваются.
* Табельный номер (`tabnum`) неизменяем: это ключ сотрудника.
* Обновление строки `PEOPLE` запускает штатные триггеры базы
  (`PEOPLE_LOGEDIT` пишет событие в журнал, `PEOPLE_BU_SYS` следит за
  `ACTIVE`), поэтому изменение ФИО видно в журнале событий City так же, как
  изменение через `mancard`.

Ошибки:

| Код | Код ошибки | Когда |
|---|---|---|
| 400 | `VALIDATION_ERROR` | пустой `tabnum`, пустая или слишком длинная фамилия, длинные имя/отчество |
| 401 | `UNAUTHORIZED` | нет токена |
| 403 | `FORBIDDEN` | токен без роли администратора |
| 404 | `NOT_FOUND` | сотрудник с таким `tabnum` не найден |
| 409 | `AMBIGUOUS_TABNUM` | в базе несколько сотрудников с одним `tabnum` |
| 500 | `INTERNAL_ERROR` | ошибка базы данных |

## GET /api/v2/Person/Access — набор категорий сотрудника

```http
GET /api/v2/Person/Access?tabnum=T-100
```

Ответ — JSON-массив идентификаторов `ACCESSNAME.ID_ACCESSNAME` по
возрастанию:

```json
[0, 1]
```

Если категорий нет — пустой массив. `401`/`404`/`409`/`500` — как у
`PUT /api/v2/Person`.

Идентификатор `0` — существующая системная категория `Check Time`, поэтому
ноль допустим и возвращается наравне с остальными.

## PUT /api/v2/Person/Access — замена набора категорий

```http
PUT /api/v2/Person/Access
Content-Type: application/json

{
  "tabnum": "T-100",
  "accessnames": [0, 1]
}
```

Ответ — итоговый набор по возрастанию:

```json
[0, 1]
```

Правила:

* `tabnum` обязателен; `accessnames` обязателен, но может быть пустым
  массивом — это очищает набор сотрудника.
* Набор **заменяется целиком**: категории из тела выдаются, отсутствующие
  снимаются, лишние связи удаляются.
* Идентификаторы — целые неотрицательные числа, не больше 1000 штук в
  запросе; дубликаты и порядок не важны.
* Все категории должны существовать в `ACCESSNAME` (`ID_DB = 1`), иначе
  `422 ACCESSNAME_NOT_FOUND` с перечнем отсутствующих в `message`.
* Изменения выполняются одной транзакцией (`READ COMMITTED`): строка
  сотрудника блокируется (`SELECT ... WITH LOCK`), поэтому параллельные
  сохранения не пересекаются. При временном конфликте блокировок операция
  повторяется до 5 раз.
* Строки `SS_ACCESSUSER` не обновляются (BEFORE UPDATE trigger
  `SS_ACCESSUSER_BU` запрещает изменение), а удаляются и вставляются заново.
  Меняются только отличия от текущего набора, поэтому лишних удалений и
  вставок нет.
* `ID_ACCESSUSER` назначает триггер `SS_ACCESSUSER_CREATE_ID` (генератор
  `GEN_SS_ACCESSUSER_ID`).
* Триггеры `SS_ACCESSUSER_WRITECARDS` и `SS_ACCESSUSER_DELETECARD`
  пересчитывают индексы карт на устройствах, поэтому выдача и снятие
  категории сразу учитываются в индексах.

Ошибки:

| Код | Код ошибки | Когда |
|---|---|---|
| 400 | `VALIDATION_ERROR` | нет `accessnames`, больше 1000 идентификаторов, отрицательный идентификатор, пустой `tabnum` |
| 401 | `UNAUTHORIZED` | нет токена |
| 403 | `FORBIDDEN` | токен без роли администратора |
| 404 | `NOT_FOUND` | сотрудник с таким `tabnum` не найден |
| 409 | `AMBIGUOUS_TABNUM` | несколько сотрудников с одним `tabnum` |
| 422 | `ACCESSNAME_NOT_FOUND` | категории из `accessnames` отсутствуют в `ACCESSNAME` |
| 500 | `INTERNAL_ERROR` | ошибка базы данных |

## Таблицы и триггеры

| Объект | Назначение |
|---|---|
| `PEOPLE` | сотрудник: `ID_PEP`, `ID_DB`, `TABNUM`, `SURNAME`, `NAME`, `PATRONYMIC`, `ID_ORG`, `TIME_STAMP`, `ACTIVE` |
| `ACCESSNAME` | категории доступа: `ID_ACCESSNAME`, `ID_DB`, `NAME` |
| `SS_ACCESSUSER` | связи сотрудника с категориями: `ID_ACCESSUSER`, `ID_DB`, `ID_PEP`, `ID_ACCESSNAME`, `USERNAME` |
| `UNQ_SS_ACCESSUSER` | уникальность `(ID_PEP, ID_DB, ID_ACCESSNAME)` |
| `SS_ACCESSUSER_BU` | запрещает UPDATE строк связи |
| `SS_ACCESSUSER_CREATE_ID` | назначает `ID_ACCESSUSER` из `GEN_SS_ACCESSUSER_ID` |
| `SS_ACCESSUSER_WRITECARDS` / `SS_ACCESSUSER_DELETECARD` | пересчитывают индексы карт при выдаче/снятии категории |

## Примеры (curl)

```bash
TOKEN=$(curl -s "http://localhost:5056/login?username=ADMIN&pasword=333" | jq -r .data.token)

# Статистика по сотрудникам
curl -s "http://localhost:5056/api/v2/Person/GetCount"

# Только активные сотрудники
curl -s "http://localhost:5056/api/v2/Person/GetList?pageIndex=0&pageSize=10&active=1"

# Только неактивные сотрудники
curl -s "http://localhost:5056/api/v2/Person/GetList?pageIndex=0&pageSize=10&active=0"

# ФИО
curl -s -X PUT "http://localhost:5056/api/v2/Person" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"tabnum":"T-100","surname":"Петров","name":"Пётр","patronymic":"Петрович"}'

# Набор категорий
curl -s "http://localhost:5056/api/v2/Person/Access?tabnum=T-100" \
  -H "Authorization: Bearer $TOKEN"

curl -s -X PUT "http://localhost:5056/api/v2/Person/Access" \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"tabnum":"T-100","accessnames":[0,1]}'
```

Проверить ФИО в базе:

```sql
SELECT TABNUM, SURNAME, NAME, PATRONYMIC, ID_ORG, TIME_STAMP
FROM PEOPLE
WHERE TABNUM = 'T-100';
```

## Совместимость

* Методы v1 (`/Person`, `/Person/AddAccess`) не изменялись.
* `GET /api/v2/Person/GetList` без параметра `active` возвращает те же данные,
  что и до 1.7.0; добавлен только необязательный фильтр.
* `GET /api/v2/Person/GetCount` впервые появился в 1.7.0.
* `PATCH /api/v2/Person` по-прежнему меняет только `id_db` и `id_org`;
  `POST /api/v2/Person/AddAccess` по-прежнему добавляет одну категорию.
* В `POST /api/v2/Person/AddAccess` исправлена проверка `id_accessname`:
  раньше `0` отвергался как «не положительное число», хотя категория `0`
  (`Check Time`) существует. Теперь отрицательные значения запрещены, а
  ноль допустим.
* `GET /api/v2/Person/Access` впервые появился в 1.6.0. Клиенты, которые
  рассчитаны на более старый сервис, должны уметь откатываться на обход
  `GET /api/v2/Access/GetPeople` (так делает модуль City `restmancard`).
