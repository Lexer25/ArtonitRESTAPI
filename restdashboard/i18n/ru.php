<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Русские подписи модуля restdashboard.
 *
 * Ключи с префиксом restdashboard_ уникальны и не пересекаются с basis.
 */
return array(

    'restdashboard_title'              => 'Информация по сотрудникам и картам (REST)',
    'restdashboard_menu'               => 'Панель REST',
    'restdashboard_source'             => 'Источник',
    'restdashboard_updated'            => 'Обновлено',
    'restdashboard_refresh'            => 'Обновить',
    'restdashboard_back'               => 'Назад к панели',
    'restdashboard_retry'              => 'Повторить',
    'restdashboard_scud'               => 'СКУД',
    'restdashboard_total'              => 'Всего',
    'restdashboard_page'               => 'Страница :page из :pages',
    'restdashboard_empty'              => 'Нет данных',
    'restdashboard_no_rows'            => 'Записей не найдено',
    'restdashboard_disabled'           => 'Вывод информации запрещён в настройках.',
    'restdashboard_api_unavailable'    => 'REST API недоступен',
    'restdashboard_stale'              => 'Данные устарели: REST API недоступен.',
    'restdashboard_stale_age'          => 'Возраст данных: :age.',
    'restdashboard_error_title'        => 'Не удалось получить данные',
    'restdashboard_partial'            => 'Часть показателей недоступна.',
    'restdashboard_truncated'          => 'Список ограничен настройкой max_list_rows; показаны не все записи.',
    'restdashboard_scan_incomplete'    => 'Обход карт не завершён; список может быть неполным.',
    'restdashboard_rows'               => 'записей',
    'restdashboard_list_title'         => ':title — список',
    'restdashboard_rfid_empty'         => 'Карт с неправильным форматом не найдено.',
    'restdashboard_cards_empty'        => 'Карт не найдено.',
    'restdashboard_people_empty'       => 'Сотрудников не найдено.',
    'restdashboard_not_found'          => 'Неизвестный показатель.',

    'restdashboard_people_total'       => 'Количество пользователей',
    'restdashboard_people_inactive'    => 'Количество удалённых пользователей',
    'restdashboard_cards_total_type1'  => 'Всего карт у сотрудников',
    'restdashboard_cards_expiring'     => 'Срок действия завершится до :date',
    'restdashboard_cards_expired'      => 'Срок карты закончился',
    'restdashboard_people_without_card'=> 'Сотрудники без карты',
    'restdashboard_cards_inactive'     => 'Количество неактивных карт',
    'restdashboard_rfid_invalid'       => 'Неправильный формат RFID',

    'restdashboard_hint_people_total'  => 'Все записи PEOPLE, включая служебные, гостей и архив. Источник: Person/GetCount.total',
    'restdashboard_hint_people_inactive' => 'PEOPLE.ACTIVE = 0. Источник: Person/GetCount.inactive',
    'restdashboard_hint_cards_total_type1' => 'CARD.ID_CARDTYPE = типа карты сотрудника, все статусы. Карты без владельца API не видит.',
    'restdashboard_hint_cards_expiring' => 'Активные карты всех типов, у которых TIMEEND попадает в интервал [сейчас; конец дня горизонта].',
    'restdashboard_hint_cards_expired' => 'Активные карты всех типов, у которых TIMEEND меньше текущего момента.',
    'restdashboard_hint_people_without_card' => 'Сотрудники без ни одной карты (оценка по TABNUM).',
    'restdashboard_hint_cards_inactive' => 'CARD.ACTIVE = 0, все типы карт.',
    'restdashboard_hint_rfid_invalid'  => 'Карты типа сотрудника, содержащие строчные латинские a–f.',

    'restdashboard_col_num'            => '№',
    'restdashboard_col_tabnum'         => 'Табельный номер',
    'restdashboard_col_surname'        => 'Фамилия',
    'restdashboard_col_name'           => 'Имя',
    'restdashboard_col_patronymic'     => 'Отчество',
    'restdashboard_col_id_card'        => 'ID карты',
    'restdashboard_col_cardtype'       => 'Тип карты',
    'restdashboard_col_timestart'      => 'Действует с',
    'restdashboard_col_timeend'        => 'Действует до',
    'restdashboard_col_active'         => 'Активна',
    'restdashboard_yes'                => 'Да',
    'restdashboard_no'                 => 'Нет',
    'restdashboard_unknown'            => '—',
);
