# Сотрудники в REST API v2: ФИО и категории доступа (версия 1.6.0)

Документ описывает методы v2, добавленные в версии сервиса **1.6.0**:

| Метод | Маршрут | Назначение |
|---|---|---|
| PUT | `/api/v2/Person` | Полностью заменить ФИО сотрудника, найденного по `tabnum` |
| GET | `/api/v2/Person/Access?tabnum=...` | Получить набор категорий доступа сотрудника |
| PUT | `/api/v2/Person/Access` | Полностью заменить набор категорий доступа сотрудника |

До 1.6.0 изменить ФИО и снять категорию доступа у сотрудника через API было
нельзя: `PATCH /api/v2/Person` меняет только `id_db` и `id_org`, а
`POST /api/v2/Person/AddAccess` только добавляет одну категорию. Теперь обе
операции доступны и выполняются в той же базе Firebird.

Во всех методах сотрудник определяется табельным номером `tabnum`
(`PEOPLE.TABNUM`), как и в остальных методах v2. API работает с базой
`ID_DB = 1`.

## Авторизация

`PUT`-методы изменяющие, поэтому требуют токен с ролью администратора
(заголовок `Authorization: Bearer <token>`, токен выдаёт
`GET /login?username=ADMIN&pasword=333`). Без токена — `401`, с токеном
обычного пользователя — `403`.

`GET /api/v2/Person/Access` доступен так же, как остальные GET-методы
сотрудников.

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
| `PEOPLE` | сотрудник: `ID_PEP`, `ID_DB`, `TABNUM`, `SURNAME`, `NAME`, `PATRONYMIC`, `ID_ORG`, `TIME_STAMP` |
| `ACCESSNAME` | категории доступа: `ID_ACCESSNAME`, `ID_DB`, `NAME` |
| `SS_ACCESSUSER` | связи сотрудника с категориями: `ID_ACCESSUSER`, `ID_DB`, `ID_PEP`, `ID_ACCESSNAME`, `USERNAME` |
| `UNQ_SS_ACCESSUSER` | уникальность `(ID_PEP, ID_DB, ID_ACCESSNAME)` |
| `SS_ACCESSUSER_BU` | запрещает UPDATE строк связи |
| `SS_ACCESSUSER_CREATE_ID` | назначает `ID_ACCESSUSER` из `GEN_SS_ACCESSUSER_ID` |
| `SS_ACCESSUSER_WRITECARDS` / `SS_ACCESSUSER_DELETECARD` | пересчитывают индексы карт при выдаче/снятии категории |

## Примеры (curl)

```bash
TOKEN=$(curl -s "http://localhost:5056/login?username=ADMIN&pasword=333" | jq -r .data.token)

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
* `PATCH /api/v2/Person` по-прежнему меняет только `id_db` и `id_org`;
  `POST /api/v2/Person/AddAccess` по-прежнему добавляет одну категорию.
* В `POST /api/v2/Person/AddAccess` исправлена проверка `id_accessname`:
  раньше `0` отвергался как «не положительное число», хотя категория `0`
  (`Check Time`) существует. Теперь отрицательные значения запрещены, а
  ноль допустим.
* `GET /api/v2/Person/Access` впервые появился в 1.6.0. Клиенты, которые
  рассчитаны на более старый сервис, должны уметь откатываться на обход
  `GET /api/v2/Access/GetPeople` (так делает модуль City `restmancard`).
