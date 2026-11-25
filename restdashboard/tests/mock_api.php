<?php

if (PHP_SAPI !== 'cli' AND PHP_SAPI !== 'cli-server')
{
    http_response_code(404);
    exit;
}

/**
 * Минимальный mock Artonit REST API v2 для проверки restdashboard.
 *
 * Запуск:
 *   C:\xampp\php\php.exe -S 127.0.0.1:5099 restdashboard/tests/mock_api.php
 *
 * Данные повторяют Приложение В ТЗ: 5 сотрудников, 5 карт, из них
 * одна просроченная, одна истекает в горизонте, одна неактивная,
 * одна без TIMEEND и одна вне горизонта.
 */

$now = time();

date_default_timezone_set('UTC');
error_reporting(0);

$people = array(
    array('tabnum' => 'T1', 'surname' => 'Иванов', 'name' => 'Иван', 'patronymic' => 'Иванович'),
    array('tabnum' => 'T2', 'surname' => 'Петров', 'name' => 'Пётр', 'patronymic' => 'Петрович'),
    array('tabnum' => 'T3', 'surname' => 'Сидоров', 'name' => 'Сидор', 'patronymic' => 'Сидорович'),
    array('tabnum' => 'T4', 'surname' => 'Кузнецов', 'name' => 'Кузьма', 'patronymic' => 'Кузьмич'),
    array('tabnum' => 'T5', 'surname' => 'Безкартов', 'name' => 'Борис', 'patronymic' => 'Борисович'),
);

$cards = array(
    array(
        'id_card' => 'a1b2', 'id_cardtype' => '1', 'tabnum' => 'T1',
        'timestart' => date('Y-m-d\TH:i:s', $now - 30 * 86400),
        'timeend' => date('Y-m-d\TH:i:s', $now + 5 * 86400),
        'active' => 1,
        'createdat' => date('Y-m-d\TH:i:s', $now - 40 * 86400),
    ),
    array(
        'id_card' => 'C2', 'id_cardtype' => '1', 'tabnum' => 'T2',
        'timestart' => date('Y-m-d\TH:i:s', $now - 60 * 86400),
        'timeend' => date('Y-m-d\TH:i:s', $now - 5 * 86400),
        'active' => 1,
        'createdat' => date('Y-m-d\TH:i:s', $now - 70 * 86400),
    ),
    array(
        'id_card' => 'C3', 'id_cardtype' => '1', 'tabnum' => 'T2',
        'timestart' => date('Y-m-d\TH:i:s', $now - 60 * 86400),
        'timeend' => date('Y-m-d\TH:i:s', $now - 5 * 86400),
        'active' => 0,
        'createdat' => date('Y-m-d\TH:i:s', $now - 70 * 86400),
    ),
    array(
        'id_card' => 'C4', 'id_cardtype' => '2', 'tabnum' => 'T3',
        'timestart' => date('Y-m-d\TH:i:s', $now - 10 * 86400),
        'timeend' => NULL,
        'active' => 1,
        'createdat' => date('Y-m-d\TH:i:s', $now - 10 * 86400),
    ),
    array(
        'id_card' => 'C5', 'id_cardtype' => '1', 'tabnum' => 'T4',
        'timestart' => date('Y-m-d\TH:i:s', $now - 10 * 86400),
        'timeend' => date('Y-m-d\TH:i:s', $now + 60 * 86400),
        'active' => 1,
        'createdat' => date('Y-m-d\TH:i:s', $now - 10 * 86400),
    ),
);

$path = parse_url($_SERVER['REQUEST_URI'], PHP_URL_PATH);
$query = array();
parse_str((string) parse_url($_SERVER['REQUEST_URI'], PHP_URL_QUERY), $query);

header('Content-Type: application/json; charset=utf-8');

function mock_out($data)
{
    echo json_encode($data, JSON_UNESCAPED_UNICODE);
    exit;
}

function mock_filter_cards($cards, $query)
{
    $result = array();

    foreach ($cards as $card)
    {
        if (isset($query['id_cardtype']) AND $query['id_cardtype'] !== ''
            AND (string) $card['id_cardtype'] !== (string) $query['id_cardtype'])
        {
            continue;
        }

        if (isset($query['active']) AND $query['active'] !== '')
        {
            $active = strtolower((string) $query['active']);

            if ($active === '0' OR $active === 'false' OR $active === 'inactive')
            {
                if ((int) $card['active'] !== 0)
                {
                    continue;
                }
            }
            elseif ($active === '1' OR $active === 'true' OR $active === 'active')
            {
                if ((int) $card['active'] !== 1)
                {
                    continue;
                }
            }
        }

        if (isset($query['tabnum']) AND $query['tabnum'] !== ''
            AND (string) $card['tabnum'] !== (string) $query['tabnum'])
        {
            continue;
        }

        $result[] = $card;
    }

    return $result;
}

function mock_page($rows, $query)
{
    $page_size = isset($query['pageSize']) ? (int) $query['pageSize'] : 10;

    if ($page_size < 1)
    {
        $page_size = 10;
    }
    if ($page_size > 1000)
    {
        $page_size = 1000;
    }

    $page_index = isset($query['pageIndex']) ? max(0, (int) $query['pageIndex']) : 0;
    $page_count = (int) ceil(count($rows) / $page_size);

    return array(
        'context'   => array_slice($rows, $page_index * $page_size, $page_size),
        'pageIndex' => $page_index,
        'pageSize'  => $page_size,
        'pageCount' => $page_count,
    );
}

if ($path === '/api/v2/Person/GetCount')
{
    mock_out(array('total' => 5, 'active' => 5, 'inactive' => 0));
}

if ($path === '/api/v2/Person/GetList')
{
    $rows = $people;

    if (isset($query['active']) AND $query['active'] !== '')
    {
        $active = strtolower((string) $query['active']);

        if ($active === '0' OR $active === 'false' OR $active === 'inactive')
        {
            $rows = array();
        }
    }

    mock_out(mock_page($rows, $query));
}

if ($path === '/api/v2/Identifier/GetCount')
{
    $filtered = mock_filter_cards($cards, $query);
    $active = 0;
    $inactive = 0;

    foreach ($filtered as $card)
    {
        if ((int) $card['active'] === 1)
        {
            $active++;
        }
        elseif ((int) $card['active'] === 0)
        {
            $inactive++;
        }
    }

    mock_out(array(
        'total'    => count($filtered),
        'active'   => $active,
        'inactive' => $inactive,
    ));
}

if ($path === '/api/v2/Identifier/GetList')
{
    $filtered = mock_filter_cards($cards, $query);

    mock_out(mock_page($filtered, $query));
}

http_response_code(404);
mock_out(array('error' => 'NOT_FOUND', 'message' => 'Mock: unknown path ' . $path));
