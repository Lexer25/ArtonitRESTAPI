<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Контроллер модуля restdashboard.
 *
 *   /city/index.php/restdashboard                    — сводка раздела
 *   /city/index.php/restdashboard/list?metric=...    — список по показателю
 *   /city/index.php/restdashboard/rfid               — совместимый алиас RFID
 */
class Controller_Restdashboard extends Controller_Template
{
    /**
     * @var bool
     */
    public $auto_render = TRUE;

    public function before()
    {
        parent::before();

        $_SESSION['menu_active'] = 'restdashboard';
        $this->set_full_width(TRUE);
        $this->set_title(__('restdashboard_title'));
    }

    /**
     * Сводка «Информация по сотрудникам и картам».
     */
    public function action_index()
    {
        $refresh = (bool) $this->request->query('refresh');
        $service = new Service_Restdashboard();

        $summary = NULL;
        $error = NULL;

        try
        {
            $summary = $service->getSummary($refresh);
        }
        catch (Exception $e)
        {
            $error = $e->getMessage();

            Kohana::$log->add(Log::ERROR, 'restdashboard: не удалось получить сводку: :message', array(
                ':message' => $e->getMessage(),
            ));
        }

        $api_config = Kohana::$config->load('restdashboard.api');
        $api_url = isset($api_config['base_url']) ? (string) $api_config['base_url'] : '';

        $links = array();

        foreach ($service->metric_keys() as $metric)
        {
            // Относительный URI: HTML::anchor() сам добавит base_url и index.php.
            $links[$metric] = 'restdashboard/list?metric=' . urlencode($metric);
        }

        $content = View::factory('restdashboard/index', array(
            'summary'      => $summary,
            'error'        => $error,
            'api_url'      => $api_url,
            'source'       => preg_replace('#^https?://#', '', $api_url),
            'links'        => $links,
            'refresh_url'  => 'restdashboard?refresh=1',
            'version'      => defined('RESTDASHBOARD_VERSION') ? RESTDASHBOARD_VERSION : '1.0.0',
        ));

        $this->template->content = $content;
    }

    /**
     * Список записей по показателю.
     */
    public function action_list()
    {
        $metric = trim((string) $this->request->query('metric'));
        $page = max(1, (int) $this->request->query('page'));
        $per_page = (int) $this->request->query('per_page');
        $refresh = (bool) $this->request->query('refresh');

        $service = new Service_Restdashboard();

        if ( ! $service->is_valid_metric($metric))
        {
            $this->response->status(404);

            $this->template->content = View::factory('restdashboard/list', array(
                'list'        => NULL,
                'error'       => __('restdashboard_not_found'),
                'metric'      => $metric,
                'title'       => __('restdashboard_not_found'),
                'back_url'    => 'restdashboard',
                'refresh_url' => 'restdashboard',
                'source'      => '',
                'version'     => defined('RESTDASHBOARD_VERSION') ? RESTDASHBOARD_VERSION : '1.0.0',
            ));

            return;
        }

        $this->set_title($service->list_title($metric));

        $list = NULL;
        $error = NULL;

        try
        {
            $list = $service->getList($metric, $page, ($per_page > 0 ? $per_page : NULL), $refresh);
            $page = $list['page'];
        }
        catch (Exception $e)
        {
            $error = $e->getMessage();

            Kohana::$log->add(Log::ERROR, 'restdashboard: не удалось получить список :metric: :message', array(
                ':metric'  => $metric,
                ':message' => $e->getMessage(),
            ));
        }

        $api_config = Kohana::$config->load('restdashboard.api');
        $api_url = isset($api_config['base_url']) ? (string) $api_config['base_url'] : '';

        $refresh_query = array(
            'metric' => $metric,
            'page'   => $page,
            'refresh' => 1,
        );

        if ($per_page > 0)
        {
            $refresh_query['per_page'] = $per_page;
        }

        $content = View::factory('restdashboard/list', array(
            'list'        => $list,
            'error'       => $error,
            'metric'      => $metric,
            'title'       => $service->list_title($metric),
            'back_url'    => 'restdashboard',
            'refresh_url' => 'restdashboard/list?' . http_build_query($refresh_query, '', '&'),
            'source'      => preg_replace('#^https?://#', '', $api_url),
            'version'     => defined('RESTDASHBOARD_VERSION') ? RESTDASHBOARD_VERSION : '1.0.0',
            'per_page'    => $per_page,
        ));

        $this->template->content = $content;
    }

    /**
     * Совместимый алиас страницы RFID.
     */
    public function action_rfid()
    {
        // Относительный URI: Controller::redirect() сам добавит base_url и index.php.
        $this->redirect('restdashboard/list?metric=rfid_invalid');
    }
}
