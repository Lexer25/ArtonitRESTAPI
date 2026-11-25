<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Сводка «Информация по сотрудникам и картам» на данных REST API v2.
 *
 * @var array|NULL  $summary
 * @var string|NULL $error
 * @var array       $links
 * @var string      $source
 * @var string      $refresh_url
 * @var string      $version
 */

$metrics = (is_array($summary) AND isset($summary['metrics']) AND is_array($summary['metrics']))
    ? $summary['metrics']
    : array();

$derived = (is_array($summary) AND isset($summary['derived']) AND is_array($summary['derived']))
    ? $summary['derived']
    : array();

$warnings = (is_array($summary) AND ! empty($summary['warnings']) AND is_array($summary['warnings']))
    ? $summary['warnings']
    : array();

$disabled = (is_array($summary) AND ! empty($summary['disabled']));
$stale = (is_array($summary) AND ! empty($summary['stale']));
$generated_at = is_array($summary) ? Arr::get($summary, 'generated_at', '') : '';
$horizon_date = Arr::get($derived, 'horizon_date', '');

$rows = array(
    'people_total' => array(
        'label' => __('restdashboard_people_total'),
        'hint'  => __('restdashboard_hint_people_total'),
    ),
    'people_inactive' => array(
        'label' => __('restdashboard_people_inactive'),
        'hint'  => __('restdashboard_hint_people_inactive'),
    ),
    'cards_total_type1' => array(
        'label' => __('restdashboard_cards_total_type1'),
        'hint'  => __('restdashboard_hint_cards_total_type1'),
    ),
    'cards_expiring' => array(
        'label' => __('restdashboard_cards_expiring', array(':date' => $horizon_date)),
        'hint'  => __('restdashboard_hint_cards_expiring'),
    ),
    'cards_expired' => array(
        'label' => __('restdashboard_cards_expired'),
        'hint'  => __('restdashboard_hint_cards_expired'),
    ),
    'people_without_card' => array(
        'label' => __('restdashboard_people_without_card'),
        'hint'  => __('restdashboard_hint_people_without_card'),
    ),
    'cards_inactive' => array(
        'label' => __('restdashboard_cards_inactive'),
        'hint'  => __('restdashboard_hint_cards_inactive'),
    ),
);

$rfid_value = Arr::get($metrics, 'rfid_invalid', NULL);
$rfid_class = ($rfid_value === NULL) ? 'default' : ((int) $rfid_value > 0 ? 'danger' : 'success');
?>
<style>
    .rd-panel { max-width: 600px; }
    .rd-meta { margin-bottom: 10px; }
    .rd-metrics .rd-row {
        display: flex;
        justify-content: space-between;
        align-items: baseline;
        gap: 12px;
        padding: 3px 0;
        border-bottom: 1px dotted #eee;
    }
    .rd-metrics .rd-row:last-child { border-bottom: 0; }
    .rd-metrics .rd-label { color: #333; }
    .rd-metrics .rd-value { font-weight: 600; white-space: nowrap; }
    .rd-warnings { margin: 10px 0 0; padding-left: 18px; }
    .rd-footer { margin-top: 12px; }
</style>

<div class="panel panel-primary rd-panel">
    <div class="panel-heading">
        <h3 class="panel-title">
            <?php echo HTML::chars(__('restdashboard_title')); ?>
            <span class="label label-info">REST API v2</span>
        </h3>
    </div>
    <div class="panel-body">

        <div class="rd-meta clearfix small text-muted">
            <span class="pull-left">
                <?php echo HTML::chars(__('restdashboard_source')); ?>:
                <?php echo HTML::chars($source); ?>
                <?php if ($generated_at !== ''): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars(__('restdashboard_updated')); ?>:
                    <?php echo HTML::chars($generated_at); ?>
                <?php endif; ?>
            </span>
            <span class="pull-right">
                <?php echo HTML::anchor(
                    $refresh_url,
                    HTML::chars(__('restdashboard_refresh')),
                    array('class' => 'btn btn-xs btn-default', 'title' => __('restdashboard_refresh'))
                ); ?>
            </span>
        </div>

        <?php if ($disabled): ?>
            <div class="alert alert-warning">
                <?php echo HTML::chars(__('restdashboard_disabled')); ?>
                <?php echo HTML::anchor('setting', 'Настройки'); ?>
            </div>
        <?php elseif ($summary === NULL OR $error !== NULL): ?>
            <div class="alert alert-danger">
                <b><?php echo HTML::chars(__('restdashboard_error_title')); ?>.</b><br>
                <?php echo HTML::chars(($error !== NULL) ? $error : __('restdashboard_api_unavailable')); ?><br><br>
                <?php echo HTML::anchor(
                    $refresh_url,
                    HTML::chars(__('restdashboard_retry')),
                    array('class' => 'btn btn-sm btn-default')
                ); ?>
            </div>
        <?php else: ?>

            <?php if ($stale): ?>
                <div class="alert alert-warning">
                    <b><?php echo HTML::chars(__('restdashboard_stale')); ?></b>
                    <?php if (! empty($summary['stale_age'])): ?>
                        <?php echo HTML::chars(__('restdashboard_stale_age', array(
                            ':age' => (int) $summary['stale_age'] . ' с',
                        ))); ?>
                    <?php endif; ?>
                </div>
            <?php endif; ?>

            <?php if ( ! empty($summary['error'])): ?>
                <div class="alert alert-warning">
                    <b><?php echo HTML::chars(__('restdashboard_partial')); ?></b><br>
                    <?php echo HTML::chars($summary['error']); ?>
                </div>
            <?php endif; ?>

            <?php if ( ! empty($warnings)): ?>
                <div class="alert alert-warning">
                    <ul class="rd-warnings">
                        <?php foreach ($warnings as $warning): ?>
                            <li><?php echo HTML::chars($warning); ?></li>
                        <?php endforeach; ?>
                    </ul>
                </div>
            <?php endif; ?>

            <b><?php echo HTML::chars(__('restdashboard_scud')); ?></b><br>

            <div class="rd-metrics">
                <?php foreach ($rows as $metric => $meta): ?>
                    <?php
                    $value = Arr::get($metrics, $metric, NULL);
                    $label = $meta['label'];
                    $hint = $meta['hint'];
                    ?>
                    <div class="rd-row">
                        <span class="rd-label" title="<?php echo HTML::chars($hint); ?>">
                            <?php echo HTML::chars($label); ?>
                        </span>
                        <span class="rd-value">
                            <?php if ($value === NULL): ?>
                                &mdash;
                            <?php else: ?>
                                <?php echo HTML::anchor(
                                    $links[$metric],
                                    number_format((int) $value, 0, '', ' '),
                                    array('title' => $hint . ' — ' . __('restdashboard_list_title', array(
                                        ':title' => $label,
                                    )))
                                ); ?>
                            <?php endif; ?>
                        </span>
                    </div>
                <?php endforeach; ?>
            </div>

            <hr>

            <span class="label label-<?php echo $rfid_class; ?>">
                <?php echo HTML::chars(__('restdashboard_rfid_invalid')); ?>
            </span>
            &nbsp;
            <?php if ($rfid_value === NULL): ?>
                &mdash;
            <?php else: ?>
                <?php echo HTML::anchor(
                    $links['rfid_invalid'],
                    number_format((int) $rfid_value, 0, '', ' '),
                    array('title' => __('restdashboard_hint_rfid_invalid'))
                ); ?>
            <?php endif; ?>

            <div class="rd-footer small text-muted">
                <?php echo HTML::chars(__('restdashboard_source')); ?>:
                <?php echo HTML::chars($source); ?>
                &nbsp;·&nbsp;restdashboard v<?php echo HTML::chars($version); ?>
                <?php if ($generated_at !== ''): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars($generated_at); ?>
                <?php endif; ?>
            </div>

        <?php endif; ?>

    </div>
</div>
