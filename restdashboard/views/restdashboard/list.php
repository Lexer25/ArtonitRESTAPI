<?php defined('SYSPATH') or die('No direct script access.');

/**
 * Список записей по показателю раздела.
 *
 * @var array|NULL  $list
 * @var string|NULL $error
 * @var string      $metric
 * @var string      $title
 * @var string      $back_url
 * @var string      $refresh_url
 * @var string      $source
 * @var string      $version
 * @var int         $per_page
 */

$rows = (is_array($list) AND isset($list['rows']) AND is_array($list['rows']))
    ? $list['rows']
    : array();

$per_page = isset($per_page) ? (int) $per_page : 0;

$warnings = (is_array($list) AND ! empty($list['warnings']) AND is_array($list['warnings']))
    ? $list['warnings']
    : array();

$kind = (is_array($list) AND ! empty($list['kind'])) ? $list['kind'] : 'cards';
$is_people = ($kind === 'people');

$page = (is_array($list) AND ! empty($list['page'])) ? (int) $list['page'] : 1;
$page_count = (is_array($list) AND ! empty($list['page_count'])) ? (int) $list['page_count'] : 1;
$total = (is_array($list) AND isset($list['total'])) ? (int) $list['total'] : 0;
$per_page_effective = (is_array($list) AND ! empty($list['per_page']))
    ? (int) $list['per_page']
    : max(1, (int) $per_page);
$generated_at = is_array($list) ? Arr::get($list, 'generated_at', '') : '';
$stale = (is_array($list) AND ! empty($list['stale']));
$truncated = (is_array($list) AND ! empty($list['truncated']));

$base_url = URL::site('restdashboard/list');

$page_url = function ($target) use ($base_url, $metric, $per_page_effective) {
    $query = array(
        'metric' => $metric,
        'page'   => (int) $target,
    );

    if ((int) $per_page_effective > 0)
    {
        $query['per_page'] = (int) $per_page_effective;
    }

    return $base_url . '?' . http_build_query($query, '', '&');
};

$empty_text = __('restdashboard_no_rows');

if ($metric === 'rfid_invalid')
{
    $empty_text = __('restdashboard_rfid_empty');
}
elseif ($is_people)
{
    $empty_text = __('restdashboard_people_empty');
}
else
{
    $empty_text = __('restdashboard_cards_empty');
}
?>
<style>
    .rd-list-panel { margin-bottom: 20px; }
    .rd-list-meta { margin-bottom: 10px; }
    .rd-list-panel td, .rd-list-panel th { vertical-align: middle !important; }
    .rd-list-panel .rd-warnings { margin: 0; padding-left: 18px; }
</style>

<div class="panel panel-primary rd-list-panel">
    <div class="panel-heading">
        <h3 class="panel-title">
            <?php echo HTML::chars($title); ?>
            <span class="label label-info">REST API v2</span>
        </h3>
    </div>
    <div class="panel-body">

        <div class="rd-list-meta clearfix small text-muted">
            <span class="pull-left">
                <?php echo HTML::anchor($back_url, '&larr; ' . HTML::chars(__('restdashboard_back'))); ?>
                <?php if ($source !== ''): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars(__('restdashboard_source')); ?>:
                    <?php echo HTML::chars($source); ?>
                <?php endif; ?>
                <?php if ($generated_at !== ''): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars(__('restdashboard_updated')); ?>:
                    <?php echo HTML::chars($generated_at); ?>
                <?php endif; ?>
            </span>
            <span class="pull-right">
                <?php echo HTML::anchor(
                    $refresh_url,
                    HTML::chars(__('restdashboard_refresh')),
                    array('class' => 'btn btn-xs btn-default')
                ); ?>
            </span>
        </div>

        <?php if ($error !== NULL OR $list === NULL): ?>
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
                    <?php if ( ! empty($list['stale_age'])): ?>
                        <?php echo HTML::chars(__('restdashboard_stale_age', array(
                            ':age' => (int) $list['stale_age'] . ' с',
                        ))); ?>
                    <?php endif; ?>
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

            <?php if ($truncated): ?>
                <div class="alert alert-info">
                    <?php echo HTML::chars(__('restdashboard_truncated')); ?>
                </div>
            <?php endif; ?>

            <p class="small text-muted">
                <?php echo HTML::chars(__('restdashboard_total')); ?>:
                <b><?php echo number_format($total, 0, '', ' '); ?></b>
                <?php if ($page_count > 1): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars(__('restdashboard_page', array(
                        ':page'  => number_format($page, 0, '', ' '),
                        ':pages' => number_format($page_count, 0, '', ' '),
                    ))); ?>
                <?php endif; ?>
            </p>

            <?php if (empty($rows)): ?>
                <div class="alert alert-info"><?php echo HTML::chars($empty_text); ?></div>
            <?php else: ?>
                <div class="table-responsive">
                    <table class="table table-striped table-hover table-condensed">
                        <thead>
                            <tr>
                                <th style="width:60px;"><?php echo HTML::chars(__('restdashboard_col_num')); ?></th>
                                <?php if ($is_people): ?>
                                    <th><?php echo HTML::chars(__('restdashboard_col_tabnum')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_surname')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_name')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_patronymic')); ?></th>
                                <?php else: ?>
                                    <th><?php echo HTML::chars(__('restdashboard_col_id_card')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_tabnum')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_cardtype')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_timestart')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_timeend')); ?></th>
                                    <th><?php echo HTML::chars(__('restdashboard_col_active')); ?></th>
                                <?php endif; ?>
                            </tr>
                        </thead>
                        <tbody>
                            <?php foreach ($rows as $index => $row): ?>
                                <?php $number = ($page - 1) * $per_page_effective + $index + 1; ?>
                                <tr>
                                    <td><?php echo number_format($number, 0, '', ' '); ?></td>
                                    <?php if ($is_people): ?>
                                        <?php
                                        $tabnum = (string) Arr::get($row, 'tabnum', '');
                                        $surname = (string) Arr::get($row, 'surname', '');
                                        $name = (string) Arr::get($row, 'name', '');
                                        $patronymic = (string) Arr::get($row, 'patronymic', '');
                                        ?>
                                        <td><?php echo $tabnum !== '' ? HTML::chars($tabnum) : '&mdash;'; ?></td>
                                        <td><?php echo $surname !== '' ? HTML::chars($surname) : '&mdash;'; ?></td>
                                        <td><?php echo $name !== '' ? HTML::chars($name) : '&mdash;'; ?></td>
                                        <td><?php echo $patronymic !== '' ? HTML::chars($patronymic) : '&mdash;'; ?></td>
                                    <?php else: ?>
                                        <?php
                                        $id_card = (string) Arr::get($row, 'id_card', '');
                                        $tabnum = (string) Arr::get($row, 'tabnum', '');
                                        $cardtype = (string) Arr::get($row, 'id_cardtype', '');
                                        $timestart = (string) Arr::get($row, 'timestart_display', '');
                                        $timeend = (string) Arr::get($row, 'timeend_display', '');
                                        $active_display = (string) Arr::get($row, 'active_display', '');
                                        ?>
                                        <td><?php echo $id_card !== '' ? HTML::chars($id_card) : '&mdash;'; ?></td>
                                        <td><?php echo $tabnum !== '' ? HTML::chars($tabnum) : '&mdash;'; ?></td>
                                        <td><?php echo $cardtype !== '' ? HTML::chars($cardtype) : '&mdash;'; ?></td>
                                        <td><?php echo $timestart !== '' ? HTML::chars($timestart) : '&mdash;'; ?></td>
                                        <td><?php echo $timeend !== '' ? HTML::chars($timeend) : '&mdash;'; ?></td>
                                        <td><?php echo $active_display !== '' ? HTML::chars($active_display) : '&mdash;'; ?></td>
                                    <?php endif; ?>
                                </tr>
                            <?php endforeach; ?>
                        </tbody>
                    </table>
                </div>

                <?php if ($page_count > 1): ?>
                    <?php
                    $start_page = max(1, $page - 2);
                    $end_page = min($page_count, $start_page + 4);
                    $start_page = max(1, $end_page - 4);
                    ?>
                    <nav class="text-center">
                        <ul class="pagination pagination-sm" style="margin:0;">
                            <li class="<?php echo ($page <= 1) ? 'disabled' : ''; ?>">
                                <a href="<?php echo HTML::chars($page_url(max(1, $page - 1))); ?>" aria-label="&laquo;">&laquo;</a>
                            </li>
                            <?php for ($target = $start_page; $target <= $end_page; $target++): ?>
                                <li class="<?php echo ($target === $page) ? 'active' : ''; ?>">
                                    <a href="<?php echo HTML::chars($page_url($target)); ?>"><?php echo (int) $target; ?></a>
                                </li>
                            <?php endfor; ?>
                            <li class="<?php echo ($page >= $page_count) ? 'disabled' : ''; ?>">
                                <a href="<?php echo HTML::chars($page_url(min($page_count, $page + 1))); ?>" aria-label="&raquo;">&raquo;</a>
                            </li>
                        </ul>
                    </nav>
                <?php endif; ?>
            <?php endif; ?>

            <div class="small text-muted" style="margin-top:12px;">
                restdashboard v<?php echo HTML::chars($version); ?>
                <?php if ($source !== ''): ?>
                    &nbsp;·&nbsp;<?php echo HTML::chars($source); ?>
                <?php endif; ?>
            </div>

        <?php endif; ?>

    </div>
</div>
