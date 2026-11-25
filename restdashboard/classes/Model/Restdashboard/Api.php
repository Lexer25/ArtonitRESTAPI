<?php defined('SYSPATH') or die('No direct script access.');

/**
 * HTTP-клиент Artonit REST API v2 для модуля restdashboard.
 *
 * Клиент выполняет только GET-запросы, не выполняет логин и не хранит
 * пароль. Необязательный Bearer-токен берётся из конфига/окружения.
 *
 * Контракт: http://localhost:5056/swagger/v2/swagger.json
 */
class Model_Restdashboard_Api extends Model
{
    /** @var Config_Group */
    protected $_config;

    /** @var array|NULL последняя ошибка: status, data, url */
    protected static $_last_error = NULL;

    public function __construct()
    {
        $this->_config = Kohana::$config->load('restdashboard.api');
    }

    /**
     * Полный URL метода API с query-параметрами.
     *
     * @param   string  $path
     * @param   array   $query
     * @return  string
     */
    public function url($path, array $query = array())
    {
        $base = rtrim($this->_config('base_url', 'http://localhost:5056'), '/');
        $url = $base . '/' . ltrim((string) $path, '/');

        if ( ! empty($query))
        {
            $url .= '?' . http_build_query($query, '', '&');
        }

        return $url;
    }

    /**
     * GET-запрос к API.
     *
     * @param   string  $path
     * @param   array   $query
     * @return  array   декодированный JSON
     * @throws  Kohana_Exception
     */
    public function get($path, array $query = array())
    {
        self::$_last_error = NULL;

        $retry = (bool) $this->_config('retry_once', TRUE);
        $attempt = 0;

        while (TRUE)
        {
            $attempt++;

            try
            {
                return $this->_request($path, $query);
            }
            catch (Restdashboard_Api_Retry_Exception $e)
            {
                if ( ! $retry OR $attempt >= 2)
                {
                    throw new Kohana_Exception($e->getMessage());
                }

                // Один повтор при сетевой ошибке/502/503/504.
                usleep(200000);
            }
        }
    }

    /**
     * Сведения о последней ошибке API.
     *
     * @return  array|NULL  array('status' => int, 'data' => mixed, 'url' => string)
     */
    public function lastError()
    {
        return self::$_last_error;
    }

    /**
     * Выполнить один HTTP-запрос.
     *
     * @param   string  $path
     * @param   array   $query
     * @return  array
     * @throws  Restdashboard_Api_Retry_Exception|Kohana_Exception
     */
    protected function _request($path, array $query)
    {
        if ( ! function_exists('curl_init'))
        {
            throw new Kohana_Exception('Расширение PHP cURL не установлено.');
        }

        $url = $this->url($path, $query);

        $headers = array(
            'Accept: application/json',
            'User-Agent: ' . (string) $this->_config('user_agent', 'ArtonitCity-RestDashboard/1.0.0'),
        );

        $token = trim((string) $this->_config('token', ''));
        if ($token !== '')
        {
            $headers[] = 'Authorization: Bearer ' . $token;
        }

        $ch = curl_init();
        curl_setopt($ch, CURLOPT_URL, $url);
        curl_setopt($ch, CURLOPT_RETURNTRANSFER, TRUE);
        curl_setopt($ch, CURLOPT_HTTPGET, TRUE);
        curl_setopt($ch, CURLOPT_CONNECTTIMEOUT, (int) $this->_config('connect_timeout', 3));
        curl_setopt($ch, CURLOPT_TIMEOUT, (int) $this->_config('timeout', 10));
        curl_setopt($ch, CURLOPT_FOLLOWLOCATION, FALSE);
        curl_setopt($ch, CURLOPT_SSL_VERIFYPEER, (bool) $this->_config('verify_ssl', FALSE));
        curl_setopt($ch, CURLOPT_SSL_VERIFYHOST, $this->_config('verify_ssl', FALSE) ? 2 : 0);
        curl_setopt($ch, CURLOPT_HTTPHEADER, $headers);

        $started = microtime(TRUE);
        $raw = curl_exec($ch);
        $errno = curl_errno($ch);
        $error = curl_error($ch);
        $status = (int) curl_getinfo($ch, CURLINFO_HTTP_CODE);
        curl_close($ch);

        $duration = microtime(TRUE) - $started;

        if ($errno)
        {
            self::$_last_error = array(
                'status' => 0,
                'data'   => NULL,
                'url'    => $url,
            );

            $this->_log('REST API недоступен: :url (:error), :duration c', array(
                ':url'      => $url,
                ':error'    => $error,
                ':duration' => number_format($duration, 3, '.', ''),
            ));

            throw new Restdashboard_Api_Retry_Exception(
                'REST API недоступен: ' . $error
            );
        }

        if ($status === 502 OR $status === 503 OR $status === 504)
        {
            self::$_last_error = array(
                'status' => $status,
                'data'   => NULL,
                'url'    => $url,
            );

            $this->_log('REST API вернул HTTP :status (:url)', array(
                ':status' => $status,
                ':url'    => $url,
            ));

            throw new Restdashboard_Api_Retry_Exception(
                'REST API временно недоступен: HTTP ' . $status
            );
        }

        if ($raw === FALSE OR $raw === NULL)
        {
            $raw = '';
        }

        // UTF-8 BOM перед json_decode.
        if (substr($raw, 0, 3) === "\xEF\xBB\xBF")
        {
            $raw = substr($raw, 3);
        }

        $data = NULL;
        if (trim($raw) !== '')
        {
            $data = json_decode($raw, TRUE);
        }

        if ($status < 200 OR $status >= 300)
        {
            self::$_last_error = array(
                'status' => $status,
                'data'   => $data,
                'url'    => $url,
            );

            $message = $this->_format_error($status, $raw, $data);

            $this->_log('REST API: :message (:url, :duration c)', array(
                ':message'  => $message,
                ':url'      => $url,
                ':duration' => number_format($duration, 3, '.', ''),
            ));

            throw new Kohana_Exception($message);
        }

        if (trim($raw) === '')
        {
            throw new Kohana_Exception('REST API вернул пустой ответ: ' . $path);
        }

        if ($data === NULL AND json_last_error() !== JSON_ERROR_NONE)
        {
            self::$_last_error = array(
                'status' => $status,
                'data'   => NULL,
                'url'    => $url,
            );

            throw new Kohana_Exception('Некорректный JSON в ответе REST API: ' . $path);
        }

        return $data;
    }

    /**
     * Человекочитаемое описание ошибки API.
     *
     * @param   int     $status
     * @param   string  $raw
     * @param   mixed   $data
     * @return  string
     */
    protected function _format_error($status, $raw, $data)
    {
        $message = '';

        if (is_array($data))
        {
            if ( ! empty($data['message']))
            {
                $message = (string) $data['message'];
            }
            elseif ( ! empty($data['error']))
            {
                $message = (string) $data['error'];
            }
        }

        if ($message === '')
        {
            $message = trim(strip_tags((string) $raw));
            $message = preg_replace('/\s+/u', ' ', $message);
        }

        if ($message === NULL)
        {
            $message = '';
        }

        if (function_exists('mb_substr'))
        {
            $message = mb_substr($message, 0, 300, 'UTF-8');
        }
        else
        {
            $message = substr($message, 0, 300);
        }

        return 'REST API: HTTP ' . (int) $status . ($message !== '' ? ': ' . $message : '');
    }

    /**
     * Запись в лог без токена и номеров карт.
     *
     * @param   string  $message
     * @param   array   $values
     */
    protected function _log($message, array $values = array())
    {
        if ( ! class_exists('Kohana') OR ! Kohana::$log)
        {
            return;
        }

        try
        {
            Kohana::$log->add(Log::ERROR, 'restdashboard: ' . $message, $values);
        }
        catch (Exception $e)
        {
            // Логирование не должно ломать страницу.
        }
    }

    /**
     * Значение конфига с fallback.
     *
     * @param   string  $key
     * @param   mixed   $default
     * @return  mixed
     */
    protected function _config($key, $default = NULL)
    {
        if (is_array($this->_config) OR $this->_config instanceof ArrayAccess)
        {
            if (isset($this->_config[$key]))
            {
                return $this->_config[$key];
            }
        }

        return $default;
    }
}

/**
 * Внутреннее исключение: запрос можно повторить.
 *
 * Отдельный класс позволяет не повторять 400/401/403/404/500.
 */
class Restdashboard_Api_Retry_Exception extends Exception
{
}
