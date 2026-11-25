<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Конфигурация модуля restdashboard.
 *
 * Все значения можно переопределить в application/config/restdashboard.php
 * (Kohana сначала читает конфиг приложения, затем — модуля).
 */
return array(

    'api' => array(
        // Базовый URL Artonit REST API v2 без завершающего слэша.
        'base_url'        => getenv('RESTDASHBOARD_API_URL') ?: 'http://localhost:5056',

        // Необязательный Bearer-токен. Используемые GET-методы анонимны,
        // поэтому по умолчанию пусто.
        'token'           => getenv('RESTDASHBOARD_API_TOKEN') ?: '',

        // Таймауты одного запроса, секунды.
        'timeout'         => 10,
        'connect_timeout' => 3,

        // Проверка SSL-сертификата (для https в промышленной эксплуатации — TRUE).
        'verify_ssl'      => FALSE,

        'user_agent'      => 'ArtonitCity-RestDashboard/1.0.0',

        // Один повтор при сетевой ошибке или HTTP 502/503/504.
        'retry_once'      => TRUE,
    ),

    'dashboard' => array(
        // Выключатель раздела без удаления модуля.
        'enabled'            => TRUE,

        // Тип карты, который считается «картой сотрудника».
        'card_type_employee' => 1,

        // Горизонт «Срок действия завершится до»; NULL — взять
        // count_day_befor_end_time из artonitcity_config (по умолчанию 30).
        'expiring_days'      => NULL,

        // Часовой пояс расчётов; NULL — date_default_timezone_get().
        'timezone'           => NULL,

        // Часовой пояс «наивных» дат API; NULL — как timezone.
        'api_timezone'       => NULL,

        // Кеш сводки.
        'cache_lifetime'     => 300,
        'stale_lifetime'     => 86400,

        // Кеш наборов данных для страниц-списков.
        'list_cache_lifetime' => 120,

        // Обход карт.
        'page_size'          => 1000,
        'max_pages'          => 50,
        'max_scan_seconds'   => 30,

        // Сколько строк хранить для страницы детализации.
        'max_list_rows'      => 2000,

        // Размер страницы в списках.
        'list_page_size'     => 50,
        'list_page_size_max' => 200,

        // Предел списка карт с неверным RFID в кеше сводки.
        'max_rfid_list'      => 1000,

        // Неверный формат RFID: строчные латинские a–f (паритет со старой панелью).
        'rfid_pattern'       => '/[a-f]/',
    ),
);
