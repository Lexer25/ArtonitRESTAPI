<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Модуль restdashboard — раздел «Информация по сотрудникам и картам»
 * на данных Artonit REST API v2.
 *
 * Модуль только читает данные через API (GET) и не выполняет SQL-запросов
 * к Firebird. Старая панель /city/index.php/dashboard продолжает работать
 * параллельно.
 */

defined('RESTDASHBOARD_VERSION') OR define('RESTDASHBOARD_VERSION', '1.0.0');

Route::set('restdashboard', 'restdashboard(/<action>)')
    ->defaults(array(
        'controller' => 'Restdashboard',
        'action'     => 'index',
    ));

Kohana::$config->load('menu')
    ->set('restdashboard', array(
        'title'    => 'Панель REST',
        'url'      => 'restdashboard',
        'icon'     => 'fa-tachometer',
        'order'    => 4,
        'disabled' => FALSE,
    ));
