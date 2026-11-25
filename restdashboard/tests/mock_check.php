<?php

if (PHP_SAPI !== 'cli')
{
    http_response_code(404);
    exit;
}

/**
 * Проверка расчётов restdashboard на mock-API.
 *
 * Запуск (из консоли, при запущенном mock_api.php на 127.0.0.1:5099):
 *   set RESTDASHBOARD_API_URL=http://127.0.0.1:5099
 *   C:\xampp\php\php.exe restdashboard\tests\mock_check.php
 *
 * Ожидаемые значения соответствуют Приложению В ТЗ.
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

$modules = Kohana::modules();
$modules['restdashboard'] = dirname(__DIR__) . '/';
Kohana::modules($modules);

$failures = 0;

function check($name, $expected, $actual)
{
    global $failures;

    if ($expected === $actual)
    {
        echo "PASS $name = " . var_export($actual, TRUE) . "\n";
        return;
    }

    $failures++;
    echo "FAIL $name: expected " . var_export($expected, TRUE)
        . ", actual " . var_export($actual, TRUE) . "\n";
}

try
{
    $service = new Service_Restdashboard();
    $summary = $service->getSummary(TRUE);
    $metrics = $summary['metrics'];

    check('people_total', 5, $metrics['people_total']);
    check('people_inactive', 0, $metrics['people_inactive']);
    check('cards_total_type1', 4, $metrics['cards_total_type1']);
    check('cards_expiring', 1, $metrics['cards_expiring']);
    check('cards_expired', 1, $metrics['cards_expired']);
    check('people_without_card', 1, $metrics['people_without_card']);
    check('cards_inactive', 1, $metrics['cards_inactive']);
    check('rfid_invalid', 1, $metrics['rfid_invalid']);
    check('scan_complete', TRUE, $summary['scan_complete']);
    check('cards_total_all', 5, $summary['derived']['cards_total_all']);
    check('distinct_tabnums', 4, $summary['derived']['distinct_tabnums']);

    $expectations = array(
        'people_total'        => array(5, 'tabnum', 'T1'),
        'people_inactive'     => array(0, NULL, NULL),
        'cards_total_type1'   => array(4, 'id_card', 'a1b2'),
        'cards_expiring'      => array(1, 'id_card', 'a1b2'),
        'cards_expired'       => array(1, 'id_card', 'C2'),
        'people_without_card' => array(1, 'tabnum', 'T5'),
        'cards_inactive'      => array(1, 'id_card', 'C3'),
        'rfid_invalid'        => array(1, 'id_card', 'a1b2'),
    );

    foreach ($expectations as $metric => $expectation)
    {
        list($total, $field, $value) = $expectation;

        $list = $service->getList($metric, 1, 50, TRUE);

        check($metric . '_total', $total, $list['total']);
        check($metric . '_rows', $total, count($list['rows']));

        if ($field !== NULL AND $total > 0)
        {
            check($metric . '_first_' . $field, $value, $list['rows'][0][$field]);
        }
    }
}
catch (Exception $e)
{
    echo 'FATAL: ' . $e->getMessage() . "\n";
    echo $e->getTraceAsString() . "\n";
    exit(1);
}

if ($failures > 0)
{
    echo "\nFAILURES: $failures\n";
    exit(1);
}

echo "\nALL CHECKS PASSED\n";
