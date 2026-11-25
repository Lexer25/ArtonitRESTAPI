<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Карты (идентификаторы) Artonit REST API v2.
 *
 * Используются методы:
 *   GET /api/v2/Identifier/GetCount?tabnum=&timeend=&active=&id_cardtype=...
 *   GET /api/v2/Identifier/GetList?pageIndex=&pageSize=&...
 */
class Model_Restdashboard_Card extends Model
{
    /** @var Model_Restdashboard_Api */
    protected $_api;

    public function __construct()
    {
        $this->_api = Model::factory('Restdashboard_Api');
    }

    /**
     * Количество карт после фильтров.
     *
     * @param   array   $filters  tabnum, timestart, timeend, active, id_cardtype, createdat
     * @return  array   array('total' => int, 'active' => int, 'inactive' => int)
     * @throws  Kohana_Exception
     */
    public function getCount(array $filters = array())
    {
        $data = $this->_api->get('/api/v2/Identifier/GetCount', $this->_query($filters));

        return $this->_validate_counts($data, 'Identifier/GetCount');
    }

    /**
     * Одна страница карт.
     *
     * @param   int     $page_index  с нуля
     * @param   int     $page_size
     * @param   array   $filters
     * @return  array
     * @throws  Kohana_Exception
     */
    public function getPage($page_index, $page_size, array $filters = array())
    {
        $query = $this->_query($filters);
        $query['pageIndex'] = max(0, (int) $page_index);
        $query['pageSize'] = max(1, (int) $page_size);

        $data = $this->_api->get('/api/v2/Identifier/GetList', $query);

        return $this->_normalize_page($data, 'Identifier/GetList');
    }

    /**
     * Постраничный обход всех карт, доступных API.
     *
     * @param   callable  $callback  function(array $row)
     * @param   array     $options   page_size, max_pages, max_seconds
     * @param   array     $stats     результаты обхода (передаётся по ссылке)
     * @throws  Kohana_Exception
     */
    public function scan($callback, array $options, array &$stats)
    {
        $page_size   = max(1, (int) Arr::get($options, 'page_size', 1000));
        $max_pages   = max(1, (int) Arr::get($options, 'max_pages', 50));
        $max_seconds = max(1, (int) Arr::get($options, 'max_seconds', 30));

        $stats = array(
            'pages'      => 0,
            'rows'       => 0,
            'page_count' => NULL,
            'complete'   => FALSE,
            'stopped'    => NULL,
            'warnings'   => array(),
        );

        $started = microtime(TRUE);
        $page = 0;
        $last_rows = array();
        $has_more = TRUE;
        $stopped = NULL;

        do
        {
            $page_data = $this->getPage($page, $page_size);
            $rows = $page_data['rows'];
            $last_rows = $rows;

            foreach ($rows as $row)
            {
                $stats['rows']++;
                call_user_func($callback, $row);
            }

            $stats['pages']++;

            if ($page_data['page_count'] !== NULL)
            {
                $stats['page_count'] = (int) $page_data['page_count'];
            }
            else
            {
                $stats['page_count'] = NULL;
                $stats['warnings'][] = 'API не вернул pageCount; обход продолжен до пустой страницы.';
            }

            if ($stats['page_count'] === 0 AND ! empty($rows))
            {
                $stats['page_count'] = NULL;
                $stats['warnings'][] = 'API вернул непустую страницу при pageCount = 0; обход продолжен до пустой страницы.';
            }

            if ( ! empty($page_data['warnings']))
            {
                $stats['warnings'] = array_merge($stats['warnings'], $page_data['warnings']);
            }

            $page++;
            $elapsed = microtime(TRUE) - $started;

            if ($page >= $max_pages)
            {
                $stopped = 'max_pages';
            }
            elseif ($elapsed >= $max_seconds)
            {
                $stopped = 'max_seconds';
            }

            $has_more = ( ! empty($rows)
                AND ($stats['page_count'] === NULL OR $page < $stats['page_count']));
        }
        while ($has_more AND $stopped === NULL);

        if ($stopped !== NULL AND $has_more)
        {
            $stats['complete'] = FALSE;
            $stats['stopped'] = $stopped;
        }
        elseif ($stats['page_count'] === NULL)
        {
            $stats['complete'] = empty($last_rows);
        }
        else
        {
            $stats['complete'] = ($page >= $stats['page_count']);
        }
    }

    /**
     * Query-параметры фильтров GetCount/GetList.
     *
     * @param   array   $filters
     * @return  array
     */
    protected function _query(array $filters)
    {
        $query = array();

        foreach (array('tabnum', 'timestart', 'timeend', 'active', 'id_cardtype',
            'createdat', 'identifire') as $key)
        {
            if ( ! array_key_exists($key, $filters))
            {
                continue;
            }

            $value = $filters[$key];

            if ($value === NULL OR $value === '')
            {
                continue;
            }

            if ($key === 'active')
            {
                if (is_bool($value))
                {
                    $value = $value ? 1 : 0;
                }
                elseif (is_string($value))
                {
                    $normalized = strtolower(trim($value));

                    if ($normalized === 'all' OR $normalized === 'any')
                    {
                        continue;
                    }
                    if ($normalized === 'active' OR $normalized === 'true' OR $normalized === 'yes')
                    {
                        $value = 1;
                    }
                    elseif ($normalized === 'inactive' OR $normalized === 'false' OR $normalized === 'no')
                    {
                        $value = 0;
                    }
                }
            }

            $query[$key] = $value;
        }

        return $query;
    }

    /**
     * Проверка ответа GetCount.
     *
     * @param   mixed   $data
     * @param   string  $method
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _validate_counts($data, $method)
    {
        if ( ! is_array($data))
        {
            throw new Kohana_Exception('Некорректный ответ :method: ожидался объект.', array(':method' => $method));
        }

        $result = array();

        foreach (array('total', 'active', 'inactive') as $key)
        {
            if ( ! array_key_exists($key, $data)
                OR $data[$key] === NULL
                OR ! is_numeric($data[$key])
                OR (int) $data[$key] < 0)
            {
                throw new Kohana_Exception(
                    'Некорректный ответ :method: поле :field отсутствует или не является неотрицательным числом.',
                    array(':method' => $method, ':field' => $key)
                );
            }

            $result[$key] = (int) $data[$key];
        }

        return $result;
    }

    /**
     * Нормализация страницы карт.
     *
     * @param   mixed   $data
     * @param   string  $method
     * @return  array
     * @throws  Kohana_Exception
     */
    protected function _normalize_page($data, $method)
    {
        if ( ! is_array($data))
        {
            throw new Kohana_Exception('Некорректный ответ :method: ожидался объект.', array(':method' => $method));
        }

        if ( ! isset($data['context']) OR ! is_array($data['context']))
        {
            throw new Kohana_Exception('Некорректный ответ :method: отсутствует массив context.', array(':method' => $method));
        }

        $warnings = array();
        $rows = array();

        foreach ($data['context'] as $row)
        {
            if ( ! is_array($row))
            {
                $warnings[] = 'API вернул строку карты не в виде объекта; строка пропущена.';
                continue;
            }

            $active = Arr::get($row, 'active', NULL);

            $rows[] = array(
                'id_card'      => trim((string) Arr::get($row, 'id_card', '')),
                'id_cardtype'  => trim((string) Arr::get($row, 'id_cardtype', '')),
                'tabnum'       => trim((string) Arr::get($row, 'tabnum', '')),
                'timestart'    => $this->_string_or_null(Arr::get($row, 'timestart', NULL)),
                'timeend'      => $this->_string_or_null(Arr::get($row, 'timeend', NULL)),
                'active'       => $active,
                'active_state' => $this->activeState($active),
                'createdat'    => $this->_string_or_null(Arr::get($row, 'createdat', NULL)),
            );
        }

        return array(
            'rows'       => $rows,
            'page_count' => $this->_page_count($data),
            'page_index' => (int) Arr::get($data, 'pageIndex', 0),
            'page_size'  => (int) Arr::get($data, 'pageSize', 0),
            'warnings'   => $warnings,
        );
    }

    /**
     * Состояние карты: 1 — активна, 0 — неактивна, NULL — неизвестно.
     *
     * @param   mixed   $active
     * @return  int|NULL
     */
    public function activeState($active)
    {
        if ($active === TRUE OR $active === 1 OR $active === '1')
        {
            return 1;
        }

        if ($active === FALSE OR $active === 0 OR $active === '0')
        {
            return 0;
        }

        return NULL;
    }

    /**
     * @param   mixed   $value
     * @return  string|NULL
     */
    protected function _string_or_null($value)
    {
        if ($value === NULL)
        {
            return NULL;
        }

        $value = trim((string) $value);

        return ($value === '') ? NULL : $value;
    }

    /**
     * @param   array   $data
     * @return  int|NULL
     */
    protected function _page_count(array $data)
    {
        if ( ! array_key_exists('pageCount', $data) OR ! is_numeric($data['pageCount']))
        {
            return NULL;
        }

        return max(0, (int) $data['pageCount']);
    }
}
