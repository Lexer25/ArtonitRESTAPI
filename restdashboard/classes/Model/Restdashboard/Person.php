<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Сотрудники Artonit REST API v2.
 *
 * Используются методы:
 *   GET /api/v2/Person/GetCount            -> {total, active, inactive}
 *   GET /api/v2/Person/GetList?active=...  -> {context, pageIndex, pageSize, pageCount}
 */
class Model_Restdashboard_Person extends Model
{
    /** @var Model_Restdashboard_Api */
    protected $_api;

    public function __construct()
    {
        $this->_api = Model::factory('Restdashboard_Api');
    }

    /**
     * Количество всех/активных/неактивных сотрудников.
     *
     * @return  array  array('total' => int, 'active' => int, 'inactive' => int)
     * @throws  Kohana_Exception
     */
    public function getCount()
    {
        $data = $this->_api->get('/api/v2/Person/GetCount');

        return $this->_validate_counts($data, 'Person/GetCount');
    }

    /**
     * Одна страница сотрудников.
     *
     * @param   int         $page_index  с нуля
     * @param   int         $page_size
     * @param   int|NULL    $active      NULL — все, 1 — активные, 0 — неактивные
     * @return  array
     * @throws  Kohana_Exception
     */
    public function getPage($page_index, $page_size, $active = NULL)
    {
        $query = array(
            'pageIndex' => max(0, (int) $page_index),
            'pageSize'  => max(1, (int) $page_size),
        );

        if ($active !== NULL)
        {
            $query['active'] = ((int) $active) ? 1 : 0;
        }

        $data = $this->_api->get('/api/v2/Person/GetList', $query);

        return $this->_normalize_page($data, 'Person/GetList');
    }

    /**
     * Постраничный обход сотрудников.
     *
     * @param   callable  $callback  function(array $row)
     * @param   array     $options   page_size, max_pages, max_seconds, active
     * @param   array     $stats     результаты обхода (передаётся по ссылке)
     * @throws  Kohana_Exception
     */
    public function scan($callback, array $options, array &$stats)
    {
        $page_size   = max(1, (int) Arr::get($options, 'page_size', 1000));
        $max_pages   = max(1, (int) Arr::get($options, 'max_pages', 50));
        $max_seconds = max(1, (int) Arr::get($options, 'max_seconds', 30));
        $active      = Arr::get($options, 'active', NULL);

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
            $page_data = $this->getPage($page, $page_size, $active);
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
     * Проверка и приведение ответа GetCount.
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
     * Нормализация страницы сотрудников.
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
                $warnings[] = 'API вернул строку сотрудника не в виде объекта; строка пропущена.';
                continue;
            }

            $rows[] = array(
                'tabnum'     => trim((string) Arr::get($row, 'tabnum', '')),
                'surname'    => trim((string) Arr::get($row, 'surname', '')),
                'name'       => trim((string) Arr::get($row, 'name', '')),
                'patronymic' => trim((string) Arr::get($row, 'patronymic', '')),
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
