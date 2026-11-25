# Модуль `restdashboard`

Раздел **«Информация по сотрудникам и картам»** панели Artonit City,
работающий на данных **Artonit REST API v2** (`http://localhost:5056`), без
прямых SQL-запросов к Firebird.

Модуль не заменяет старую панель `/city/index.php/dashboard`: обе страницы
работают параллельно.

## Что реализовано

* сводная панель `/city/index.php/restdashboard` с восемью показателями;
* списки записей по клику на число каждого показателя;
* HTTP-клиент REST API v2 (GET, JSON, таймауты, ошибки, один повтор);
* файловый кеш сводки (300 с) и резервный stale-кеш (24 ч);
* кеш наборов данных для списков (120 с);
* частичная деградация: если один агрегат недоступен, остальные показываются;
* понятные ошибки без HTTP 500 и без stack trace;
* русские подписи, экранирование всех строк, PHP 5.6 / Kohana 3.3.

## Маршруты

| URL | Назначение |
|---|---|
| `/city/index.php/restdashboard` | сводка раздела |
| `/city/index.php/restdashboard/list?metric=people_total` | список по показателю |
| `/city/index.php/restdashboard/list?metric=people_inactive` | удалённые сотрудники |
| `/city/index.php/restdashboard/list?metric=cards_total_type1` | карты типа сотрудника |
| `/city/index.php/restdashboard/list?metric=cards_expiring` | срок действия завершится до горизонта |
| `/city/index.php/restdashboard/list?metric=cards_expired` | срок карты закончился |
| `/city/index.php/restdashboard/list?metric=people_without_card` | сотрудники без карты |
| `/city/index.php/restdashboard/list?metric=cards_inactive` | неактивные карты |
| `/city/index.php/restdashboard/list?metric=rfid_invalid` | неверный формат RFID |
| `/city/index.php/restdashboard/rfid` | алиас списка RFID |

Параметры списка: `page` (с 1), `per_page` (10…200), `refresh=1`.
Параметр сводки: `refresh=1` — очистить кеш и пересчитать.

## Установка

1. Скопировать каталог `restdashboard` в `C:\xampp\htdocs\city\modules\restdashboard`.
2. Добавить модуль в `application/bootstrap.php` после `restmancard`:

   ```php
   'restdashboard' => MODPATH.'restdashboard',
   ```

3. Проверить `modules/restdashboard/config/restdashboard.php`:
   `api.base_url` (по умолчанию `http://localhost:5056`), при необходимости
   `api.token`, `dashboard.timezone`, `dashboard.api_timezone`.
4. Убедиться, что каталог `application/cache` доступен на запись.
5. Открыть `/city/index.php/restdashboard`.

Откат: удалить строку из `application/bootstrap.php`, очистить
`application/cache`, удалить каталог модуля. Схема БД не меняется.

## Конфигурация

Файл `modules/restdashboard/config/restdashboard.php`.

| Ключ | По умолчанию | Описание |
|---|---|---|
| `api.base_url` | `http://localhost:5056` | базовый URL API |
| `api.token` | пусто | необязательный Bearer-токен |
| `api.timeout` | 10 | таймаут запроса, с |
| `api.connect_timeout` | 3 | таймаут соединения, с |
| `api.verify_ssl` | `FALSE` | проверка SSL |
| `api.retry_once` | `TRUE` | один повтор при сетевой ошибке/502/503/504 |
| `dashboard.enabled` | `TRUE` | выключатель раздела |
| `dashboard.card_type_employee` | 1 | `ID_CARDTYPE` карты сотрудника |
| `dashboard.expiring_days` | `NULL` | горизонт; `NULL` — `count_day_befor_end_time` (30) |
| `dashboard.timezone` | `NULL` | часовой пояс расчётов |
| `dashboard.api_timezone` | `NULL` | часовой пояс «наивных» дат API |
| `dashboard.cache_lifetime` | 300 | TTL свежей сводки, с |
| `dashboard.stale_lifetime` | 86400 | TTL резервной копии, с |
| `dashboard.list_cache_lifetime` | 120 | TTL набора для списка, с |
| `dashboard.page_size` | 1000 | размер страницы обхода (1…1000) |
| `dashboard.max_pages` | 50 | предохранитель обхода |
| `dashboard.max_scan_seconds` | 30 | предохранитель времени обхода |
| `dashboard.max_list_rows` | 2000 | сколько строк хранить для списка |
| `dashboard.list_page_size` | 50 | размер страницы списка |
| `dashboard.max_rfid_list` | 1000 | предел RFID-списка в кеше сводки |
| `dashboard.rfid_pattern` | `/[a-f]/` | правило «неверного» RFID |

Переменные окружения `RESTDASHBOARD_API_URL` и `RESTDASHBOARD_API_TOKEN`
имеют приоритет над значениями по умолчанию.

## Используемые методы API

| Метод | Назначение |
|---|---|
| `GET /api/v2/Person/GetCount` | всего/активных/неактивных сотрудников |
| `GET /api/v2/Person/GetList` | страницы сотрудников (все, `active=0`) |
| `GET /api/v2/Identifier/GetCount` | всего карт, карт типа, неактивных |
| `GET /api/v2/Identifier/GetList` | страницы карт для списков и обхода |

Все методы — анонимные GET, токен не требуется. Модуль требует API **не ниже
1.8.0** (в 1.7.0 нет `Identifier/GetCount`).

## Правила расчёта

| Показатель | Правило |
|---|---|
| Количество пользователей | `Person/GetCount.total` |
| Удалённые пользователи | `Person/GetCount.inactive` |
| Всего карт у сотрудников | `Identifier/GetCount?id_cardtype=T.total` |
| Срок действия завершится до | активные карты всех типов, `TIMEEND` в `[сейчас; конец дня горизонта]` |
| Срок карты закончился | активные карты всех типов, `TIMEEND < сейчас` |
| Сотрудники без карты | `people_total − количество уникальных TABNUM с картой` |
| Неактивные карты | `Identifier/GetCount?active=0.inactive` |
| Неверный формат RFID | карты типа `T`, `ID_CARD` содержит строчные `a–f` |

`now` фиксируется один раз в начале расчёта. `TIMEEND = NULL` не истекает.
`ACTIVE = NULL` не считается активной картой. При неполном обходе
производные показатели равны `null` и в интерфейсе показывается «—».

## Отличия от старой панели

* «Удалённые пользователи» считаются по `PEOPLE` без `LEFT JOIN CARD`, поэтому
  не завышаются при нескольких картах у сотрудника (дефект D-2 старой панели).
* «Срок действия завершится до» учитывает только активные карты и включает
  последний день горизонта (дефект D-3).
* API не возвращает карты без владельца (`CARD` без строки `PEOPLE`), поэтому
  счётчики карт могут отличаться от прямого SQL.
* При дублях `TABNUM` показатель «Сотрудники без карты» приблизительный.

## Кеш и обновление

* Свежая сводка — 300 с; повторная загрузка страницы не обращается к API.
* Резервная копия — 24 ч; при недоступности API показывается она с пометкой
  «Данные устарели».
* Списки, требующие обхода карт (срок, RFID, «без карты»), кешируются на 120 с.
  Для RFID дополнительно действует ограничение `max_rfid_list`.
* `?refresh=1` очищает соответствующий кеш и пересчитывает данные.
* Кеш хранится в `application/cache` средствами `Cache::instance('file')`.

## Ограничения

* нет anti-join в API: «без карты» считается по множеству `TABNUM`;
* `Identifier/GetList` не отдаёт `id_pep`, связь только по `TABNUM`;
* фильтр `timeend` в API — точная дата/день, а не диапазон, поэтому сроки
  считаются обходом карт;
* при количестве карт больше `max_pages × page_size` обход не начинается,
  производные показатели не рассчитываются;
* часовой пояс API должен совпадать с City или задаваться `api_timezone`.

## Диагностика

| Симптом | Проверить |
|---|---|
| «REST API недоступен» | запущен ли сервис на `localhost:5056`, `api.base_url` |
| «API требует авторизации» | не закрыт ли API авторизацией, `api.token` |
| «Некорректный JSON» | версию API и ответ `swagger/v2/swagger.json` |
| Все производные «—» | `max_pages`, `max_scan_seconds`, лог `application/logs` |
| Цифры не меняются | `cache_lifetime`, запрос `?refresh=1` |

Полный текст ошибок пишется в `application/logs`; токен и номера карт в лог
не попадают.

## Тесты

В каталоге `tests/`:

* `cli_smoke.php` — проверка на реальном API (`Person/GetCount`,
  `Identifier/GetCount`, списки);
* `mock_api.php` + `mock_check.php` — проверка расчётов на синтетическом
  наборе из 5 сотрудников и 5 карт (сроки, RFID, «без карты», неактивные);
* `README.md` — команды запуска.

Пример mock-проверки:

```bat
C:\xampp\php\php.exe -n -S 127.0.0.1:5099 restdashboard\tests\mock_api.php
set RESTDASHBOARD_API_URL=http://127.0.0.1:5099
C:\xampp\php\php.exe -n -d extension_dir=C:\xampp\php\ext -d extension=php_curl.dll -d extension=php_mbstring.dll restdashboard\tests\mock_check.php
```

## Версия

`RESTDASHBOARD_VERSION = 1.0.0` (константа в `init.php`).
