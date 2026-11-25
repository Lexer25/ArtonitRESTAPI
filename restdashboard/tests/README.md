# Тесты модуля restdashboard

Все проверки выполняются в консоли, на стенде с запущенным Artonit City и
PHP 5.6. Тестовые скрипты подключают bootstrap City и каталог модуля из
репозитория, не изменяя установленный модуль.

## 1. Синтаксис

```bat
for %f in (restdashboard\*.php restdashboard\classes\*\*.php restdashboard\classes\*\*\*.php restdashboard\views\restdashboard\*.php) do C:\xampp\php\php.exe -n -l %f
```

## 2. Smoke на реальном API

```bat
C:\xampp\php\php.exe -n -d extension_dir=C:\xampp\php\ext -d extension=php_curl.dll -d extension=php_mbstring.dll restdashboard\tests\cli_smoke.php
```

Ожидаемые значения стенда 25.11.2026: 79 пользователей, 0 удалённых,
0 карт, 0 истекающих, 0 истёкших, 79 без карты, 0 неактивных, 0 RFID.

## 3. Mock-проверка расчётов (5 сотрудников, 5 карт)

Терминал 1 — mock-сервер:

```bat
C:\xampp\php\php.exe -n -S 127.0.0.1:5099 restdashboard\tests\mock_api.php
```

Терминал 2 — проверка:

```bat
set RESTDASHBOARD_API_URL=http://127.0.0.1:5099
C:\xampp\php\php.exe -n -d extension_dir=C:\xampp\php\ext -d extension=php_curl.dll -d extension=php_mbstring.dll restdashboard\tests\mock_check.php
```

Ожидается `ALL CHECKS PASSED`: 5 пользователей, 4 карты типа 1, 1 истекает,
1 истекла, 1 без карты, 1 неактивная, 1 RFID; все списки содержат нужные
строки.

## 4. HTTP-проверка

Открыть:

* `http://localhost:8080/city/index.php/restdashboard`
* `http://localhost:8080/city/index.php/restdashboard/list?metric=people_total`
* `http://localhost:8080/city/index.php/restdashboard/list?metric=people_without_card`
* `http://localhost:8080/city/index.php/restdashboard/rfid`

Проверить: восемь чисел, ссылки без двойного `/city/index.php/`, страницу 2
списка, `?refresh=1`, отсутствие записей `CRITICAL` в `application/logs`.
