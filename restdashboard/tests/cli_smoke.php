<?php

if (PHP_SAPI !== 'cli')
{
    http_response_code(404);
    exit;
}

/**
 * CLI-проверка модуля restdashboard без установки в City.
 *
 * Запуск:
 *   C:\xampp\php\php.exe restdashboard\tests\cli_smoke.php
 *
 * Скрипт подключает bootstrap Artonit City, добавляет каталог модуля
 * из репозитория и вызывает сервис напрямую. Предназначен для разработки
 * и диагностики; на промышленном стенде не используется.
 */

define('EXT', '.php');

$city = 'C:/xampp/htdocs/city/';

define('DOCROOT', $city);
define('APPPATH', $city . 'application/');
define('MODPATH', $city . 'modules/');
define('SYSPATH', $city . 'system/');
define('KOHANA_START_TIME', microtime(TRUE));
define('KOHANA_START_MEMORY', memory_get_usage());

$_SERVER['SERVER_PROTOCOL'] = 'HTTP/1.1';

require APPPATH . 'bootstrap' . EXT;

// Добавляем модуль из репозитория, не копируя его в City.
$modules = Kohana::modules();
$modules['restdashboard'] = dirname(__DIR__) . '/';
Kohana::modules($modules);

function rd_print_metrics($summary)
{
    if ( ! is_array($summary))
    {
        echo "summary: NULL\n";
        return;
    }

    echo 'generated_at: ' . Arr::get($summary, 'generated_at', '') . "\n";
    echo 'stale: ' . (int) Arr::get($summary, 'stale', 0) . "\n";
    echo 'scan_complete: ' . (int) Arr::get($summary, 'scan_complete', 0) . "\n";
    echo 'error: ' . (string) Arr::get($summary, 'error', '') . "\n";

    foreach (Arr::get($summary, 'metrics', array()) as $key => $value)
    {
        echo $key . ' = ' . ($value === NULL ? 'NULL' : $value) . "\n";
    }

    foreach (Arr::get($summary, 'warnings', array()) as $warning)
    {
        echo 'warning: ' . $warning . "\n";
    }
}

try
{
    $service = new Service_Restdashboard();
    $summary = $service->getSummary(TRUE);
    rd_print_metrics($summary);

    echo "\n-- lists --\n";

    foreach ($service->metric_keys() as $metric)
    {
        try
        {
            $list = $service->getList($metric, 1, 5, TRUE);
            echo $metric . ': total=' . $list['total']
                . ' rows=' . count($list['rows'])
                . ' pages=' . $list['page_count']
                . ' kind=' . $list['kind'] . "\n";
        }
        catch (Exception $e)
        {
            echo $metric . ': ERROR ' . $e->getMessage() . "\n";
        }
    }
}
catch (Exception $e)
{
    echo 'FATAL: ' . $e->getMessage() . "\n";
    echo $e->getTraceAsString() . "\n";
    exit(1);
}
