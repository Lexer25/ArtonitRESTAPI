<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Сервис модуля restdashboard.
 *
 * Собирает восемь показателей раздела «Информация по сотрудникам и картам»
 * через Artonit REST API v2 и готовит наборы данных для страниц-списков.
 *
 * Прямых SQL-запросов к Firebird модуль не выполняет.
 */
class Service_Restdashboard
{
    /** Префикс ключей файлового кеша. */
    const CACHE_PREFIX = 'restdashboard_';

    /** @var Config_Group */
    protected $_config;

    /** @var Config_Group */
    protected $_api_config;

    /** @var Model_Restdashboard_Person */
    protected $_person;

    /** @var Model_Restdashboard_Card */
    protected $_card;

    /** @var DateTimeZone */
    protected $_tz;

    /** @var DateTimeZone */
    protected $_api_tz;

    /** @var string */
    protected $_tz_name = 'UTC';

    /** @var string */
    protected $_api_tz_name = 'UTC';

    /** @var Cache|NULL|FALSE */
    protected static $_cache_instance = NULL;

    /** @var array */
    protected $_metrics = array(
        'people_total',
        'people_inactive',
        'cards_total_type1',
        'cards_expiring',
        'cards_expired',
        'people_without_card',
        'cards_inactive',
        'rfid_invalid',
    );

    public function __construct()
    {
        $this->_config = Kohana::$config->load('restdashboard.dashboard');
        $this->_api_config = Kohana::$config->load('restdashboard.api');
        $this->_person = Model::factory('Restdashboard_Person');
        $this->_card = Model::factory('Restdashboard_Card');

        $this->_init_timezones();
    }

    /**
     * Сводка по разделу.
     *
     * @param   bool    $refresh  игнорировать свежий кеш
     * @return  array
     * @throws  Kohana_Exception
     */
    public function getSummary($refresh = FALSE)
    {
        if ( ! $this->_enabled())
        {
            return $this->emptySummary(TRUE);
        }

        $refresh = (bool) $refresh;
        $key = $this->_cache_key('summary');
        $stale_key = $this->_cache_key('summary_stale');

        if ($refresh)
        {
            $this->_cache_delete($key);
            $this->_cache_delete($stale_key);
        }

        $cached = $this->_cache_get($key);

        if ( ! $refresh
            AND is_array($cached)
            AND isset($cached['generated_ts'])
            AND (int) $cached['generated_ts'] + $this->_cache_lifetime() > time())
        {
            return $cached;
        }

        $stale = $this->_cache_get($stale_key);

        try
        {
            $fresh = $this->_compute_summary();
        }
        catch (Exception $e)
        {
            if (is_array($stale))
            {
                $stale['stale'] = TRUE;
                $stale['stale_age'] = max(0, time() - (int) Arr::get($stale, 'generated_ts', time()));
                $stale['error'] = $e->getMessage();
                $stale['warnings'][] = 'Показаны данные из резервного кеша: ' . $e->getMessage();

                return $stale;
            }

            // Ни одного показателя и нет stale — ошибку обработает контроллер.
            throw $e;
        }

        if ($this->_has_any_metric($fresh))
        {
            if ($this->_cache_lifetime() > 0)
            {
                $this->_cache_set($key, $fresh, $this->_cache_lifetime());
            }

            if ($this->_stale_lifetime() > 0)
            {
                $this->_cache_set($stale_key, $fresh, $this->_stale_lifetime());
            }
        }

        return $fresh;
    }

    /**
     * Страница-список по показателю.
     *
     * @param   string  $metric
     * @param   int     $page      1-based
     * @param   int     $per_page
     * @param   bool    $refresh
     * @return  array
     * @throws  Kohana_Exception
     */
    public function getList($metric, $page = 1, $per_page = NULL, $refresh = FALSE)
    {
        if ( ! $this->is_valid_metric($metric))
        {
            throw new Kohana_Exception(__('restdashboard_not_found'));
        }

        $page = max(1, (int) $page);

        if ($per_page === NULL)
        {
            $per_page = $this->_list_page_size();
        }

        $per_page = max(10, min($this->_list_page_size_max(), (int) $per_page));

        if ($metric === 'people_total' OR $metric === 'people_inactive')
        {
            return $this->_list_people($metric, $page, $per_page);
        }

        if ($metric === 'cards_total_type1' OR $metric === 'cards_inactive')
        {
            return $this->_list_cards($metric, $page, $per_page);
        }

        return $this->_list_derived($metric, $page, $per_page, (bool) $refresh);
    }

    /**
     * Заголовок страницы-списка.
     *
     * @param   string  $metric
     * @return  string
     */
    public function list_title($metric)
    {
        if ($metric === 'cards_expiring')
        {
            $horizon = $this->_horizon();

            return __('restdashboard_cards_expiring', array(':date' => $horizon['date']));
        }

        return __($this->metric_title_key($metric));
    }

    /**
     * Ключ i18n для показателя.
     *
     * @param   string  $metric
     * @return  string
     */
    public function metric_title_key($metric)
    {
        $map = array(
            'people_total'        => 'restdashboard_people_total',
            'people_inactive'     => 'restdashboard_people_inactive',
            'cards_total_type1'   => 'restdashboard_cards_total_type1',
            'cards_expiring'      => 'restdashboard_cards_expiring',
            'cards_expired'       => 'restdashboard_cards_expired',
            'people_without_card' => 'restdashboard_people_without_card',
            'cards_inactive'      => 'restdashboard_cards_inactive',
            'rfid_invalid'        => 'restdashboard_rfid_invalid',
        );

        return isset($map[$metric]) ? $map[$metric] : 'restdashboard_not_found';
    }

    /**
     * @param   string  $metric
     * @return  bool
     */
    public function is_valid_metric($metric)
    {
        return in_array($metric, $this->_metrics, TRUE);
    }

    /**
     * @return  array
     */
    public function metric_keys()
    {
        return $this->_metrics;
    }

    /**
     * Пустая сводка (выключенный раздел или фатальная ошибка).
     *
     * @param   bool    $disabled
     * @return  array
     */
    public function emptySummary($disabled = FALSE)
    {
        $metrics = array();

        foreach ($this->_metrics as $metric)
        {
            $metrics[$metric] = NULL;
        }

        $horizon = $this->_horizon();

        return array(
            'generated_at'    => $this->_format_ts(time()),
            'generated_ts'    => time(),
            'disabled'        => (bool) $disabled,
            'stale'           => FALSE,
            'stale_age'       => NULL,
            'error'           => NULL,
            'warnings'        => array(),
            'scan_complete'   => FALSE,
            'scan_consistent' => FALSE,
            'metrics'         => $metrics,
            'derived'         => array(
                'horizon_date'     => $horizon['date'],
                'horizon_end'      => $horizon['display'],
                'cards_total_all'  => NULL,
                'cards_scanned'    => 0,
                'card_pages'       => 0,
                'type1_scanned'    => 0,
                'inactive_scanned' => 0,
                'distinct_tabnums' => 0,
                'empty_tabnum'     => 0,
                'invalid_timeend'  => 0,
                'scan_consistent'  => FALSE,
                'scan_reason'      => NULL,
            ),
            'rfid_cards'      => array(),
            'source'          => (string) $this->_cfg_api('base_url', ''),
        );
    }

    /* ---------------------------------------------------------------------
     * Расчёт сводки
     * ------------------------------------------------------------------ */

    /**
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _compute_summary()
    {
        $now = time();
        $horizon = $this->_horizon();
        $card_type = $this->_card_type();
        $max_rfid_list = $this->_max_rfid_list();
        $rfid_pattern = $this->_rfid_pattern();

        $warnings = array();
        $errors = array();

        $people = NULL;
        $all = NULL;
        $type1 = NULL;
        $inactive = NULL;

        try
        {
            $people = $this->_person->getCount();
        }
        catch (Exception $e)
        {
            $errors[] = 'Сотрудники: ' . $e->getMessage();
            $this->_log(Log::ERROR, 'Person/GetCount: :message', array(':message' => $e->getMessage()));
        }

        try
        {
            $all = $this->_card->getCount();
        }
        catch (Exception $e)
        {
            $errors[] = 'Карты: ' . $e->getMessage();
            $this->_log(Log::ERROR, 'Identifier/GetCount: :message', array(':message' => $e->getMessage()));
        }

        try
        {
            $type1 = $this->_card->getCount(array('id_cardtype' => $card_type));
        }
        catch (Exception $e)
        {
            $errors[] = 'Карты типа ' . $card_type . ': ' . $e->getMessage();
            $this->_log(Log::ERROR, 'Identifier/GetCount?id_cardtype: :message', array(':message' => $e->getMessage()));
        }

        try
        {
            $inactive = $this->_card->getCount(array('active' => 0));
        }
        catch (Exception $e)
        {
            $errors[] = 'Неактивные карты: ' . $e->getMessage();
            $this->_log(Log::ERROR, 'Identifier/GetCount?active=0: :message', array(':message' => $e->getMessage()));
        }

        $people_total = ($people !== NULL) ? (int) $people['total'] : NULL;
        $people_inactive = ($people !== NULL) ? (int) $people['inactive'] : NULL;
        $cards_total_all = ($all !== NULL) ? (int) $all['total'] : NULL;
        $cards_total_type1 = ($type1 !== NULL) ? (int) $type1['total'] : NULL;
        $cards_inactive = ($inactive !== NULL) ? (int) $inactive['inactive'] : NULL;

        $scan = array(
            'tabnums'         => array(),
            'expired'         => 0,
            'expiring'        => 0,
            'rfid'            => 0,
            'rfid_cards'      => array(),
            'type1'           => 0,
            'inactive'        => 0,
            'empty_tabnum'    => 0,
            'invalid_timeend' => 0,
        );

        $scan_stats = array(
            'pages'      => 0,
            'rows'       => 0,
            'page_count' => NULL,
            'complete'   => FALSE,
            'stopped'    => NULL,
            'warnings'   => array(),
        );

        $scan_reason = NULL;
        $scan_complete = FALSE;
        $scan_consistent = FALSE;

        $page_size = $this->_page_size();
        $max_pages = $this->_max_pages();
        $expected_pages = ($cards_total_all === NULL)
            ? NULL
            : (int) ceil($cards_total_all / $page_size);

        $skip_scan = ($expected_pages !== NULL AND $expected_pages > $max_pages);

        if ($skip_scan)
        {
            $scan_reason = 'expected_pages';
            $warnings[] = 'Карт больше, чем позволяет обойти лимит max_pages; показатели по срокам и RFID не рассчитаны.';
            $this->_log(Log::WARNING, 'Обход карт не начат: ожидается :expected страниц при лимите :max', array(
                ':expected' => $expected_pages,
                ':max'      => $max_pages,
            ));
        }
        else
        {
            $self = $this;

            $callback = function ($row) use (&$scan, $self, $now, $horizon, $card_type, $max_rfid_list, $rfid_pattern)
            {
                $tabnum = (string) Arr::get($row, 'tabnum', '');
                $id_card = (string) Arr::get($row, 'id_card', '');
                $type = (int) Arr::get($row, 'id_cardtype', 0);
                $active = Arr::get($row, 'active_state', NULL);
                $timeend_raw = Arr::get($row, 'timeend', NULL);

                if ($tabnum === '')
                {
                    $scan['empty_tabnum']++;
                }
                else
                {
                    $scan['tabnums'][$tabnum] = TRUE;
                }

                if ($active === 0)
                {
                    $scan['inactive']++;
                }

                // Срок действия считается по картам всех типов.
                if ($active === 1 AND $timeend_raw !== NULL)
                {
                    $timeend = $self->parse_api_datetime($timeend_raw);

                    if ($timeend === NULL)
                    {
                        $scan['invalid_timeend']++;
                    }
                    elseif ($timeend < $now)
                    {
                        $scan['expired']++;
                    }
                    elseif ($timeend <= $horizon['end_ts'])
                    {
                        $scan['expiring']++;
                    }
                }

                // RFID — только карты типа сотрудника.
                if ($type === $card_type)
                {
                    $scan['type1']++;

                    if ($id_card !== '' AND preg_match($rfid_pattern, $id_card) === 1)
                    {
                        $scan['rfid']++;

                        if (count($scan['rfid_cards']) < $max_rfid_list)
                        {
                            $scan['rfid_cards'][] = array(
                                'id_card'     => $id_card,
                                'tabnum'      => $tabnum,
                                'id_cardtype' => (string) Arr::get($row, 'id_cardtype', ''),
                                'timeend'     => $timeend_raw,
                                'active'      => Arr::get($row, 'active', NULL),
                            );
                        }
                    }
                }
            };

            try
            {
                $this->_card->scan($callback, array(
                    'page_size'   => $page_size,
                    'max_pages'   => $max_pages,
                    'max_seconds' => $this->_max_scan_seconds(),
                ), $scan_stats);

                $scan_complete = (bool) $scan_stats['complete'];
                $scan_reason = $scan_stats['stopped'];

                if ( ! empty($scan_stats['warnings']))
                {
                    $warnings = array_merge($warnings, $scan_stats['warnings']);
                }

                $scan_consistent = ($cards_total_all === NULL
                    OR (int) $scan_stats['rows'] === $cards_total_all);

                if ($scan_complete AND ! $scan_consistent)
                {
                    $warnings[] = 'Количество карт изменилось во время обхода; показатели приблизительные.';

                    $this->_log(Log::WARNING, 'Сверка обхода: прочитано :rows, всего :total', array(
                        ':rows'  => $scan_stats['rows'],
                        ':total' => $cards_total_all,
                    ));
                }

                if ( ! $scan_complete)
                {
                    $warnings[] = 'Обход карт не завершён; производные показатели не рассчитаны.';
                    $this->_log(Log::WARNING, 'Обход карт прерван (:reason): страниц :pages, карт :rows', array(
                        ':reason' => ($scan_reason !== NULL) ? $scan_reason : 'unknown',
                        ':pages'  => $scan_stats['pages'],
                        ':rows'   => $scan_stats['rows'],
                    ));
                }
            }
            catch (Exception $e)
            {
                $scan_complete = FALSE;
                $scan_reason = 'api_error';
                $warnings[] = 'Не удалось прочитать карты: ' . $e->getMessage();
                $this->_log(Log::ERROR, 'Обход карт: :message', array(':message' => $e->getMessage()));
            }
        }

        if ($scan_complete)
        {
            if ($cards_total_type1 !== NULL AND $scan['type1'] !== $cards_total_type1)
            {
                $warnings[] = 'Сверка по картам типа ' . $card_type . ' не совпала: '
                    . $scan['type1'] . ' из ' . $cards_total_type1 . '.';
            }

            if ($cards_inactive !== NULL AND $scan['inactive'] !== $cards_inactive)
            {
                $warnings[] = 'Сверка по неактивным картам не совпала: '
                    . $scan['inactive'] . ' из ' . $cards_inactive . '.';
            }
        }

        $cards_expiring = $scan_complete ? $scan['expiring'] : NULL;
        $cards_expired = $scan_complete ? $scan['expired'] : NULL;
        $rfid_invalid = $scan_complete ? $scan['rfid'] : NULL;

        $people_without_card = NULL;

        if ($people_total !== NULL AND $scan_complete)
        {
            $with_card = count($scan['tabnums']);
            $people_without_card = max(0, $people_total - $with_card);

            if ($with_card > $people_total)
            {
                $warnings[] = 'Количество сотрудников с картами превышает общее количество сотрудников; проверьте уникальность TABNUM.';
            }

            if ($scan['empty_tabnum'] > 0)
            {
                $warnings[] = 'Найдены карты без табельного номера; показатель «Сотрудники без карты» может быть завышен.';
            }
        }

        if ($scan['invalid_timeend'] > 0)
        {
            $warnings[] = 'Найдены карты с невалидной датой TIMEEND: ' . $scan['invalid_timeend'] . '.';
        }

        $metrics = array(
            'people_total'        => $people_total,
            'people_inactive'     => $people_inactive,
            'cards_total_type1'   => $cards_total_type1,
            'cards_expiring'      => $cards_expiring,
            'cards_expired'       => $cards_expired,
            'people_without_card' => $people_without_card,
            'cards_inactive'      => $cards_inactive,
            'rfid_invalid'        => $rfid_invalid,
        );

        if ( ! $this->_has_any_metric(array('metrics' => $metrics)) AND ! empty($errors))
        {
            throw new Kohana_Exception(implode(' ', $errors));
        }

        $warnings = $this->_unique($warnings);

        return array(
            'generated_at'    => $this->_format_ts($now),
            'generated_ts'    => $now,
            'disabled'        => FALSE,
            'stale'           => FALSE,
            'stale_age'       => NULL,
            'error'           => empty($errors) ? NULL : implode(' ', $errors),
            'warnings'        => $warnings,
            'scan_complete'   => $scan_complete,
            'scan_consistent' => $scan_consistent,
            'metrics'         => $metrics,
            'derived'         => array(
                'horizon_date'     => $horizon['date'],
                'horizon_end'      => $horizon['display'],
                'cards_total_all'  => $cards_total_all,
                'cards_scanned'    => (int) $scan_stats['rows'],
                'card_pages'       => (int) $scan_stats['pages'],
                'type1_scanned'    => (int) $scan['type1'],
                'inactive_scanned' => (int) $scan['inactive'],
                'distinct_tabnums' => count($scan['tabnums']),
                'empty_tabnum'     => (int) $scan['empty_tabnum'],
                'invalid_timeend'  => (int) $scan['invalid_timeend'],
                'scan_consistent'  => $scan_consistent,
                'scan_reason'      => $scan_reason,
            ),
            'rfid_cards'      => $scan['rfid_cards'],
            'source'          => (string) $this->_cfg_api('base_url', ''),
        );
    }

    /* ---------------------------------------------------------------------
     * Списки: прямые страницы API
     * ------------------------------------------------------------------ */

    /**
     * @param   string  $metric
     * @param   int     $page
     * @param   int     $per_page
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _list_people($metric, $page, $per_page)
    {
        $active = ($metric === 'people_inactive') ? 0 : NULL;

        $count = $this->_person->getCount();
        $page_data = $this->_person->getPage($page - 1, $per_page, $active);

        $total = ($active === 0) ? $count['inactive'] : $count['total'];
        $page_count = ($page_data['page_count'] !== NULL)
            ? $page_data['page_count']
            : (int) ceil($total / $per_page);

        $page_count = max(1, $page_count);
        $requested_page = $page;

        if ($page > $page_count)
        {
            $page = $page_count;
        }

        if ($page !== $requested_page)
        {
            // После ограничения номера страницы перечитываем нужную страницу.
            $page_data = $this->_person->getPage($page - 1, $per_page, $active);
        }

        $rows = $page_data['rows'];

        return array(
            'metric'        => $metric,
            'kind'          => 'people',
            'title'         => $this->list_title($metric),
            'rows'          => $rows,
            'total'         => $total,
            'page'          => $page,
            'per_page'      => $per_page,
            'page_count'    => $page_count,
            'truncated'     => FALSE,
            'scan_complete' => TRUE,
            'warnings'      => $page_data['warnings'],
            'generated_at'  => $this->_format_ts(time()),
            'stale'         => FALSE,
            'stale_age'     => NULL,
        );
    }

    /**
     * @param   string  $metric
     * @param   int     $page
     * @param   int     $per_page
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _list_cards($metric, $page, $per_page)
    {
        $filters = array();

        if ($metric === 'cards_total_type1')
        {
            $filters['id_cardtype'] = $this->_card_type();
        }
        elseif ($metric === 'cards_inactive')
        {
            $filters['active'] = 0;
        }

        $count = $this->_card->getCount($filters);
        $page_data = $this->_card->getPage($page - 1, $per_page, $filters);

        $total = $count['total'];
        $page_count = ($page_data['page_count'] !== NULL)
            ? $page_data['page_count']
            : (int) ceil($total / $per_page);

        $page_count = max(1, $page_count);
        $requested_page = $page;

        if ($page > $page_count)
        {
            $page = $page_count;
        }

        if ($page !== $requested_page)
        {
            $page_data = $this->_card->getPage($page - 1, $per_page, $filters);
        }

        $rows = $page_data['rows'];

        foreach ($rows as $index => $row)
        {
            $rows[$index] = $this->decorate_card_row($row);
        }

        return array(
            'metric'        => $metric,
            'kind'          => 'cards',
            'title'         => $this->list_title($metric),
            'rows'          => $rows,
            'total'         => $total,
            'page'          => $page,
            'per_page'      => $per_page,
            'page_count'    => $page_count,
            'truncated'     => FALSE,
            'scan_complete' => TRUE,
            'warnings'      => $page_data['warnings'],
            'generated_at'  => $this->_format_ts(time()),
            'stale'         => FALSE,
            'stale_age'     => NULL,
        );
    }

    /* ---------------------------------------------------------------------
     * Списки: производные наборы (обход карт)
     * ------------------------------------------------------------------ */

    /**
     * @param   string  $metric
     * @param   int     $page
     * @param   int     $per_page
     * @param   bool    $refresh
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _list_derived($metric, $page, $per_page, $refresh)
    {
        $dataset = $this->_dataset($metric, $refresh);
        $rows = $dataset['rows'];
        $total = (int) $dataset['total'];

        // Если список обрезан max_list_rows, страниц не больше, чем есть строк.
        $available = count($rows);
        $page_count = max(1, (int) ceil(min($total, $available) / $per_page));
        $page = min($page, $page_count);

        $offset = ($page - 1) * $per_page;
        $page_rows = array_slice($rows, $offset, $per_page);

        return array(
            'metric'        => $metric,
            'kind'          => ($metric === 'people_without_card') ? 'people' : 'cards',
            'title'         => $this->list_title($metric),
            'rows'          => $page_rows,
            'total'         => $total,
            'page'          => $page,
            'per_page'      => $per_page,
            'page_count'    => $page_count,
            'truncated'     => (bool) $dataset['truncated'],
            'scan_complete' => (bool) $dataset['scan_complete'],
            'warnings'      => $dataset['warnings'],
            'generated_at'  => $dataset['generated_at'],
            'stale'         => (bool) $dataset['stale'],
            'stale_age'     => $dataset['stale_age'],
        );
    }

    /**
     * Набор строк для производного показателя (с кешем и stale).
     *
     * @param   string  $metric
     * @param   bool    $refresh
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _dataset($metric, $refresh)
    {
        $key = $this->_cache_key('list_' . $metric);

        if ($refresh)
        {
            $this->_cache_delete($key);
        }

        $cached = $this->_cache_get($key);
        $lifetime = $this->_list_cache_lifetime();

        if ( ! $refresh
            AND is_array($cached)
            AND isset($cached['generated_ts'])
            AND (int) $cached['generated_ts'] + $lifetime > time())
        {
            return $cached;
        }

        try
        {
            $fresh = $this->_build_dataset($metric);
        }
        catch (Exception $e)
        {
            if (is_array($cached))
            {
                $cached['stale'] = TRUE;
                $cached['stale_age'] = max(0, time() - (int) Arr::get($cached, 'generated_ts', time()));
                $cached['warnings'][] = 'Показаны данные из кеша: ' . $e->getMessage();

                return $cached;
            }

            throw $e;
        }

        // Храним дольше, чем TTL свежести: запись используется как stale.
        $this->_cache_set($key, $fresh, max(3600, $lifetime));

        return $fresh;
    }

    /**
     * @param   string  $metric
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _build_dataset($metric)
    {
        if ($metric === 'people_without_card')
        {
            return $this->_build_dataset_people_without_card();
        }

        return $this->_build_dataset_cards($metric);
    }

    /**
     * Списки карт: срок истекает, срок истёк, неверный RFID.
     *
     * @param   string  $metric
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _build_dataset_cards($metric)
    {
        $now = time();
        $horizon = $this->_horizon();
        $card_type = $this->_card_type();
        $max_rows = $this->_max_list_rows();

        if ($metric === 'rfid_invalid')
        {
            $max_rows = min($max_rows, $this->_max_rfid_list());
        }

        $rfid_pattern = $this->_rfid_pattern();

        $warnings = array();
        $rows = array();
        $total = 0;
        $truncated = FALSE;
        $invalid_timeend = 0;

        $self = $this;

        $callback = function ($row) use (&$rows, &$total, &$truncated, &$invalid_timeend,
            $self, $metric, $now, $horizon, $card_type, $max_rows, $rfid_pattern)
        {
            $match = FALSE;

            if ($metric === 'rfid_invalid')
            {
                $match = ((int) Arr::get($row, 'id_cardtype', 0) === $card_type
                    AND (string) Arr::get($row, 'id_card', '') !== ''
                    AND preg_match($rfid_pattern, (string) Arr::get($row, 'id_card', '')) === 1);
            }
            else
            {
                if (Arr::get($row, 'active_state', NULL) !== 1)
                {
                    return;
                }

                $timeend_raw = Arr::get($row, 'timeend', NULL);

                if ($timeend_raw === NULL)
                {
                    return;
                }

                $timeend = $self->parse_api_datetime($timeend_raw);

                if ($timeend === NULL)
                {
                    $invalid_timeend++;
                    return;
                }

                if ($metric === 'cards_expired')
                {
                    $match = ($timeend < $now);
                }
                elseif ($metric === 'cards_expiring')
                {
                    $match = ($timeend >= $now AND $timeend <= $horizon['end_ts']);
                }
            }

            if ( ! $match)
            {
                return;
            }

            $total++;

            if (count($rows) < $max_rows)
            {
                $rows[] = $self->decorate_card_row($row);
            }
            else
            {
                $truncated = TRUE;
            }
        };

        $stats = array();
        $this->_card->scan($callback, array(
            'page_size'   => $this->_page_size(),
            'max_pages'   => $this->_max_pages(),
            'max_seconds' => $this->_max_scan_seconds(),
        ), $stats);

        if ( ! $stats['complete'])
        {
            $warnings[] = __('restdashboard_scan_incomplete');
        }

        if ( ! empty($stats['warnings']))
        {
            $warnings = array_merge($warnings, $stats['warnings']);
        }

        if ($invalid_timeend > 0)
        {
            $warnings[] = 'Найдены карты с невалидной датой TIMEEND: ' . $invalid_timeend . '.';
        }

        return array(
            'rows'          => $rows,
            'total'         => $total,
            'truncated'     => $truncated,
            'scan_complete' => (bool) $stats['complete'],
            'warnings'      => $this->_unique($warnings),
            'generated_at'  => $this->_format_ts(time()),
            'generated_ts'  => time(),
            'stale'         => FALSE,
            'stale_age'     => NULL,
        );
    }

    /**
     * Список сотрудников без карты.
     *
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _build_dataset_people_without_card()
    {
        $max_rows = $this->_max_list_rows();
        $warnings = array();

        $people = array();
        $people_truncated = FALSE;

        $people_callback = function ($row) use (&$people, &$people_truncated, $max_rows)
        {
            if (count($people) < $max_rows)
            {
                $people[] = $row;
            }
            else
            {
                $people_truncated = TRUE;
            }
        };

        $people_stats = array();
        $this->_person->scan($people_callback, array(
            'page_size'   => $this->_page_size(),
            'max_pages'   => $this->_max_pages(),
            'max_seconds' => $this->_max_scan_seconds(),
        ), $people_stats);

        $card_tabnums = array();
        $empty_tabnum = 0;

        $card_callback = function ($row) use (&$card_tabnums, &$empty_tabnum)
        {
            $tabnum = (string) Arr::get($row, 'tabnum', '');

            if ($tabnum === '')
            {
                $empty_tabnum++;
                return;
            }

            $card_tabnums[$tabnum] = TRUE;
        };

        $card_stats = array();
        $this->_card->scan($card_callback, array(
            'page_size'   => $this->_page_size(),
            'max_pages'   => $this->_max_pages(),
            'max_seconds' => $this->_max_scan_seconds(),
        ), $card_stats);

        if ( ! $people_stats['complete'])
        {
            $warnings[] = 'Обход сотрудников не завершён; список может быть неполным.';
        }

        if ( ! $card_stats['complete'])
        {
            $warnings[] = 'Обход карт не завершён; список может быть неполным.';
        }

        if ($empty_tabnum > 0)
        {
            $warnings[] = 'Найдены карты без табельного номера; список может быть завышен.';
        }

        $rows = array();
        $total = 0;
        $truncated = FALSE;

        foreach ($people as $person)
        {
            $tabnum = (string) Arr::get($person, 'tabnum', '');

            if ($tabnum !== '' AND isset($card_tabnums[$tabnum]))
            {
                continue;
            }

            $total++;

            if (count($rows) < $max_rows)
            {
                $rows[] = $person;
            }
            else
            {
                $truncated = TRUE;
            }
        }

        if ($people_truncated)
        {
            $truncated = TRUE;
        }

        if ( ! empty($people_stats['warnings']))
        {
            $warnings = array_merge($warnings, $people_stats['warnings']);
        }

        if ( ! empty($card_stats['warnings']))
        {
            $warnings = array_merge($warnings, $card_stats['warnings']);
        }

        return array(
            'rows'          => $rows,
            'total'         => $total,
            'truncated'     => $truncated,
            'scan_complete' => ($people_stats['complete'] AND $card_stats['complete']),
            'warnings'      => $this->_unique($warnings),
            'generated_at'  => $this->_format_ts(time()),
            'generated_ts'  => time(),
            'stale'         => FALSE,
            'stale_age'     => NULL,
        );
    }

    /* ---------------------------------------------------------------------
     * Даты и оформление строк
     * ------------------------------------------------------------------ */

    /**
     * Разбор даты API в timestamp часового пояса City.
     *
     * @param   string|NULL $raw
     * @return  int|NULL
     */
    public function parse_api_datetime($raw)
    {
        if ($raw === NULL OR $raw === '')
        {
            return NULL;
        }

        try
        {
            $dt = new DateTime((string) $raw, $this->_api_tz);
            $dt->setTimezone($this->_tz);

            return $dt->getTimestamp();
        }
        catch (Exception $e)
        {
            return NULL;
        }
    }

    /**
     * Строка карты с добавленными полями для отображения.
     *
     * @param   array   $row
     * @return  array
     */
    public function decorate_card_row(array $row)
    {
        $row['timestart_display'] = $this->_display_datetime(Arr::get($row, 'timestart', NULL));
        $row['timeend_display'] = $this->_display_datetime(Arr::get($row, 'timeend', NULL));

        $active = Arr::get($row, 'active_state', NULL);

        if ($active === 1)
        {
            $row['active_display'] = __('restdashboard_yes');
        }
        elseif ($active === 0)
        {
            $row['active_display'] = __('restdashboard_no');
        }
        else
        {
            $row['active_display'] = __('restdashboard_unknown');
        }

        return $row;
    }

    /**
     * @param   string|NULL $raw
     * @return  string
     */
    protected function _display_datetime($raw)
    {
        $ts = $this->parse_api_datetime($raw);

        if ($ts === NULL)
        {
            return ($raw === NULL OR $raw === '') ? __('restdashboard_unknown') : (string) $raw;
        }

        return $this->_format_ts($ts, 'd.m.Y H:i');
    }

    /**
     * @param   int     $ts
     * @param   string  $format
     * @return  string
     */
    protected function _format_ts($ts, $format = 'd.m.Y H:i:s')
    {
        $dt = new DateTime('@' . (int) $ts);
        $dt->setTimezone($this->_tz);

        return $dt->format($format);
    }

    /**
     * Горизонт «срок действия завершится до».
     *
     * @return  array  date, display, end_ts
     */
    protected function _horizon()
    {
        $days = $this->_expiring_days();

        $date = new DateTime('now', $this->_tz);
        $date->setTime(0, 0, 0);
        $date->modify('+' . $days . ' days');

        $end = clone $date;
        $end->setTime(23, 59, 59);

        return array(
            'date'    => $end->format('d.m.Y'),
            'display' => $end->format('Y-m-d H:i:s'),
            'end_ts'  => $end->getTimestamp(),
        );
    }

    /**
     * Часовые пояса расчётов.
     */
    protected function _init_timezones()
    {
        $timezone = $this->_cfg('timezone', NULL);

        if ($timezone === NULL OR $timezone === '')
        {
            $timezone = date_default_timezone_get();
        }

        try
        {
            $this->_tz = new DateTimeZone($timezone);
            $this->_tz_name = $timezone;
        }
        catch (Exception $e)
        {
            $this->_tz = new DateTimeZone(date_default_timezone_get());
            $this->_tz_name = date_default_timezone_get();
        }

        $api_timezone = $this->_cfg('api_timezone', NULL);

        if ($api_timezone === NULL OR $api_timezone === '')
        {
            $api_timezone = $this->_tz_name;
        }

        try
        {
            $this->_api_tz = new DateTimeZone($api_timezone);
            $this->_api_tz_name = $api_timezone;
        }
        catch (Exception $e)
        {
            $this->_api_tz = $this->_tz;
            $this->_api_tz_name = $this->_tz_name;
        }
    }

    /* ---------------------------------------------------------------------
     * Кеш
     * ------------------------------------------------------------------ */

    /**
     * @param   string  $kind
     * @return  string
     */
    protected function _cache_key($kind)
    {
        $parts = array(
            defined('RESTDASHBOARD_VERSION') ? RESTDASHBOARD_VERSION : '1.0.0',
            (string) $this->_cfg_api('base_url', ''),
            (string) $this->_card_type(),
            (string) $this->_expiring_days(),
            $this->_tz_name,
            $this->_api_tz_name,
            (string) $this->_page_size(),
            (string) $this->_max_pages(),
            (string) $this->_max_scan_seconds(),
            (string) $this->_max_rfid_list(),
            (string) $this->_max_list_rows(),
            (string) $this->_rfid_pattern(),
        );

        return self::CACHE_PREFIX . $kind . '_' . sha1(implode('|', $parts));
    }

    /**
     * @return  Cache|NULL
     */
    protected function _cache()
    {
        if (self::$_cache_instance === NULL)
        {
            self::$_cache_instance = FALSE;

            if (class_exists('Cache'))
            {
                try
                {
                    self::$_cache_instance = Cache::instance('file');
                }
                catch (Exception $e)
                {
                    self::$_cache_instance = FALSE;
                }
            }
        }

        return self::$_cache_instance ? self::$_cache_instance : NULL;
    }

    protected function _cache_get($key)
    {
        $cache = $this->_cache();

        if ( ! $cache)
        {
            return NULL;
        }

        try
        {
            return $cache->get($key);
        }
        catch (Exception $e)
        {
            return NULL;
        }
    }

    protected function _cache_set($key, $value, $lifetime)
    {
        $cache = $this->_cache();

        if ( ! $cache)
        {
            return;
        }

        try
        {
            $cache->set($key, $value, (int) $lifetime);
        }
        catch (Exception $e)
        {
            // Кеш не критичен.
        }
    }

    protected function _cache_delete($key)
    {
        $cache = $this->_cache();

        if ( ! $cache)
        {
            return;
        }

        try
        {
            $cache->delete($key);
        }
        catch (Exception $e)
        {
            // ignore
        }
    }

    /* ---------------------------------------------------------------------
     * Конфигурация
     * ------------------------------------------------------------------ */

    /**
     * @param   string  $key
     * @param   mixed   $default
     * @return  mixed
     */
    protected function _cfg($key, $default = NULL)
    {
        return $this->_config_value($this->_config, $key, $default);
    }

    /**
     * @param   string  $key
     * @param   mixed   $default
     * @return  mixed
     */
    protected function _cfg_api($key, $default = NULL)
    {
        return $this->_config_value($this->_api_config, $key, $default);
    }

    /**
     * Значение из массива или Config_Group/ArrayObject.
     *
     * Kohana может вернуть как массив (для "group.key"), так и
     * Config_Group (ArrayObject) — поддерживаются оба варианта.
     *
     * @param   mixed   $config
     * @param   string  $key
     * @param   mixed   $default
     * @return  mixed
     */
    protected function _config_value($config, $key, $default = NULL)
    {
        if (is_array($config))
        {
            return array_key_exists($key, $config) ? $config[$key] : $default;
        }

        if (is_object($config) AND $config instanceof ArrayAccess AND isset($config[$key]))
        {
            return $config[$key];
        }

        return $default;
    }

    protected function _enabled()
    {
        return (bool) $this->_cfg('enabled', TRUE);
    }

    protected function _card_type()
    {
        return max(0, (int) $this->_cfg('card_type_employee', 1));
    }

    protected function _expiring_days()
    {
        $days = $this->_cfg('expiring_days', NULL);

        if ($days === NULL OR $days === '')
        {
            $days = 30;

            try
            {
                $city_config = Kohana::$config->load('artonitcity_config');
                $configured = $this->_config_value($city_config, 'count_day_befor_end_time', NULL);

                if ($configured !== NULL AND $configured !== '')
                {
                    $days = (int) $configured;
                }
            }
            catch (Exception $e)
            {
                $days = 30;
            }
        }

        return max(0, min(3650, (int) $days));
    }

    protected function _cache_lifetime()
    {
        return max(0, min(86400, (int) $this->_cfg('cache_lifetime', 300)));
    }

    protected function _stale_lifetime()
    {
        return max($this->_cache_lifetime(), (int) $this->_cfg('stale_lifetime', 86400));
    }

    protected function _list_cache_lifetime()
    {
        return max(0, min(86400, (int) $this->_cfg('list_cache_lifetime', 120)));
    }

    protected function _page_size()
    {
        return max(1, min(1000, (int) $this->_cfg('page_size', 1000)));
    }

    protected function _max_pages()
    {
        return max(1, min(1000, (int) $this->_cfg('max_pages', 50)));
    }

    protected function _max_scan_seconds()
    {
        return max(1, min(300, (int) $this->_cfg('max_scan_seconds', 30)));
    }

    protected function _max_list_rows()
    {
        return max(0, min(10000, (int) $this->_cfg('max_list_rows', 2000)));
    }

    protected function _max_rfid_list()
    {
        return max(0, min(10000, (int) $this->_cfg('max_rfid_list', 1000)));
    }

    protected function _list_page_size()
    {
        return max(10, (int) $this->_cfg('list_page_size', 50));
    }

    protected function _list_page_size_max()
    {
        return max(10, min(1000, (int) $this->_cfg('list_page_size_max', 200)));
    }

    protected function _rfid_pattern()
    {
        $pattern = (string) $this->_cfg('rfid_pattern', '/[a-f]/');

        if (@preg_match($pattern, '') === FALSE)
        {
            return '/[a-f]/';
        }

        return $pattern;
    }

    /* ---------------------------------------------------------------------
     * Вспомогательные методы
     * ------------------------------------------------------------------ */

    /**
     * @param   array   $summary
     * @return  bool
     */
    protected function _has_any_metric(array $summary)
    {
        if ( ! isset($summary['metrics']) OR ! is_array($summary['metrics']))
        {
            return FALSE;
        }

        foreach ($summary['metrics'] as $value)
        {
            if ($value !== NULL)
            {
                return TRUE;
            }
        }

        return FALSE;
    }

    /**
     * @param   array   $values
     * @return  array
     */
    protected function _unique(array $values)
    {
        $result = array();

        foreach ($values as $value)
        {
            $value = trim((string) $value);

            if ($value !== '')
            {
                $result[$value] = TRUE;
            }
        }

        return array_keys($result);
    }

    /**
     * @param   int     $level
     * @param   string  $message
     * @param   array   $values
     */
    protected function _log($level, $message, array $values = array())
    {
        if ( ! class_exists('Kohana') OR ! Kohana::$log)
        {
            return;
        }

        try
        {
            Kohana::$log->add($level, 'restdashboard: ' . $message, $values);
        }
        catch (Exception $e)
        {
            // ignore
        }
    }
}
