param(
    [string]$BaseUrl = 'http://127.0.0.1:5056'
)

# Дымовой тест статистики по сотрудникам REST API v2: проверяет
# GET /api/v2/Person/GetCount и фильтр active у GET /api/v2/Person/GetList.
# Запуск: powershell -ExecutionPolicy Bypass -File docs\smoke-tests\people-stats.ps1 `
#            -BaseUrl http://127.0.0.1:5056
# Тест только читает данные: сотрудники и настройки не изменяются.

Add-Type -AssemblyName System.Net.Http
$ErrorActionPreference = 'Stop'

$script:Base = $BaseUrl.TrimEnd('/')
$script:Pass = 0
$script:Fail = 0
$script:Failures = New-Object System.Collections.ArrayList

function Check {
    param([string]$Name, [bool]$Ok, [string]$Detail)
    if ($Ok) {
        $script:Pass++
    } else {
        $script:Fail++
        [void]$script:Failures.Add("$Name :: $Detail")
        Write-Host ("FAIL {0} :: {1}" -f $Name, $Detail) -ForegroundColor Red
    }
}

function Api {
    param([string]$Path)

    $client = New-Object System.Net.Http.HttpClient
    $request = New-Object System.Net.Http.HttpRequestMessage(
        [System.Net.Http.HttpMethod]::Get, ($script:Base + $Path))

    try {
        $response = $client.SendAsync($request).Result
    } catch {
        $client.Dispose()
        throw "HTTP GET $Path failed: $($_.Exception.GetBaseException().Message)"
    }

    $body = $response.Content.ReadAsStringAsync().Result

    $parsed = $null
    if ($body) {
        try { $parsed = $body | ConvertFrom-Json } catch { }
    }

    $result = [pscustomobject]@{
        Status = [int]$response.StatusCode
        Body   = $body
        Json   = $parsed
    }

    $client.Dispose()
    return $result
}

function Show {
    param($Json)
    if ($null -eq $Json) { return '<null>' }
    return ($Json | ConvertTo-Json -Compress -Depth 6)
}

function PageCount {
    param([int]$Total, [int]$PageSize)
    if ($Total -le 0) { return 0 }
    return [int][Math]::Ceiling($Total / [double]$PageSize)
}

function ItemCount {
    param($Json)
    return @($Json.context).Count
}

Write-Host "People statistics smoke test: $($script:Base)" -ForegroundColor Cyan

# 1. Статистика доступна и содержит три неотрицательных числа.
$counts = Api -Path '/api/v2/Person/GetCount'
Check 'GetCount: 200' ($counts.Status -eq 200) "status=$($counts.Status) body=$($counts.Body)"
Check 'GetCount: total is number' ($null -ne $counts.Json.total) (Show $counts.Json)
Check 'GetCount: active is number' ($null -ne $counts.Json.active) (Show $counts.Json)
Check 'GetCount: inactive is number' ($null -ne $counts.Json.inactive) (Show $counts.Json)

$total = 0
$active = 0
$inactive = 0
if ($counts.Status -eq 200) {
    $total = [int]$counts.Json.total
    $active = [int]$counts.Json.active
    $inactive = [int]$counts.Json.inactive
    Check 'GetCount: total >= 0' ($total -ge 0) (Show $counts.Json)
    Check 'GetCount: active >= 0' ($active -ge 0) (Show $counts.Json)
    Check 'GetCount: inactive >= 0' ($inactive -ge 0) (Show $counts.Json)
    Check 'GetCount: active + inactive <= total' (($active + $inactive) -le $total) (Show $counts.Json)
}

# 2. Количество всех записей совпадает с pageCount списка без фильтра
#    (pageSize=1 => pageCount равен числу записей).
$all = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1'
Check 'GetList all: 200' ($all.Status -eq 200) "status=$($all.Status) body=$($all.Body)"
Check 'GetList all: total = pageCount' ([int]$all.Json.pageCount -eq $total) (Show $all.Json)
Check 'GetList all: pageSize reflected' ([int]$all.Json.pageSize -eq 1) (Show $all.Json)

# 3. Активные: 1/true дают одно и то же количество.
$activeList = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1&active=1'
Check 'GetList active=1: 200' ($activeList.Status -eq 200) "status=$($activeList.Status) body=$($activeList.Body)"
Check 'GetList active=1: count matches GetCount.active' ([int]$activeList.Json.pageCount -eq $active) (Show $activeList.Json)

$activeTrue = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1&active=true'
Check 'GetList active=true: 200' ($activeTrue.Status -eq 200) "status=$($activeTrue.Status) body=$($activeTrue.Body)"
Check 'GetList active=true: same as active=1' ([int]$activeTrue.Json.pageCount -eq [int]$activeList.Json.pageCount) (Show $activeTrue.Json)

# 4. Неактивные: 0/false дают одно и то же количество.
$inactiveList = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1&active=0'
Check 'GetList active=0: 200' ($inactiveList.Status -eq 200) "status=$($inactiveList.Status) body=$($inactiveList.Body)"
Check 'GetList active=0: count matches GetCount.inactive' ([int]$inactiveList.Json.pageCount -eq $inactive) (Show $inactiveList.Json)

$inactiveFalse = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1&active=false'
Check 'GetList active=false: 200' ($inactiveFalse.Status -eq 200) "status=$($inactiveFalse.Status) body=$($inactiveFalse.Body)"
Check 'GetList active=false: same as active=0' ([int]$inactiveFalse.Json.pageCount -eq [int]$inactiveList.Json.pageCount) (Show $inactiveFalse.Json)

# 5. active=all и отсутствие фильтра возвращают одно и то же.
$allExplicit = Api -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1&active=all'
Check 'GetList active=all: 200' ($allExplicit.Status -eq 200) "status=$($allExplicit.Status) body=$($allExplicit.Body)"
Check 'GetList active=all: same as without filter' ([int]$allExplicit.Json.pageCount -eq [int]$all.Json.pageCount) (Show $allExplicit.Json)

# 6. Фильтры не пересекаются по количеству.
Check 'GetList: active + inactive = total' (($active + $inactive) -eq $total) "total=$total active=$active inactive=$inactive"

# 7. Пагинация после фильтра.
$page = Api -Path '/api/v2/Person/GetList?pageIndex=1&pageSize=2&active=1'
Check 'GetList pagination: 200' ($page.Status -eq 200) "status=$($page.Status) body=$($page.Body)"
Check 'GetList pagination: pageIndex=1' ([int]$page.Json.pageIndex -eq 1) (Show $page.Json)
Check 'GetList pagination: pageSize=2' ([int]$page.Json.pageSize -eq 2) (Show $page.Json)
Check 'GetList pagination: pageCount' ([int]$page.Json.pageCount -eq (PageCount $active 2)) (Show $page.Json)
Check 'GetList pagination: at most pageSize items' ((ItemCount $page.Json) -le 2) (Show $page.Json)

# 8. Неверный фильтр — 400 VALIDATION_ERROR.
$invalid = Api -Path '/api/v2/Person/GetList?active=maybe'
Check 'GetList active=maybe: 400' ($invalid.Status -eq 400) "status=$($invalid.Status) body=$($invalid.Body)"
Check 'GetList active=maybe: VALIDATION_ERROR' ($invalid.Json.error -eq 'VALIDATION_ERROR') (Show $invalid.Json)
Check 'GetList active=maybe: field active' (@($invalid.Json.details | Where-Object { $_.field -eq 'active' }).Count -eq 1) (Show $invalid.Json)

# 9. Ответ списка содержит ожидаемые поля.
if ($total -gt 0) {
    $first = $all.Json.context[0]
    Check 'GetList item: tabnum present' ($null -ne $first.tabnum) (Show $first)
    Check 'GetList item: surname present' ($null -ne $first.surname) (Show $first)
    Check 'GetList item: id_pep hidden' ($null -eq $first.id_pep) (Show $first)
}

Write-Host ("Passed: {0}; failed: {1}" -f $script:Pass, $script:Fail) -ForegroundColor Green
if ($script:Fail -gt 0) {
    $script:Failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
exit 0
