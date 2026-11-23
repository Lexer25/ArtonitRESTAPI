param(
    [string]$BaseUrl = 'http://127.0.0.1:5056'
)

# Дымовой тест REST API v2 для категорий доступа: проверяет команды
# POST /api/v2/Access, DELETE /api/v2/Access/{id_accessname},
# GET /api/v2/Access/GetList и GET /api/v2/Access/GetPeople.
# Запуск: powershell -ExecutionPolicy Bypass -File docs\smoke-tests\access.ps1 `
#            -BaseUrl http://127.0.0.1:5056
# Тест создаёт и удаляет категорию T-ACCESS-* и сотрудника T-ACC-*.

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
    param(
        [string]$Method,
        [string]$Path,
        [object]$Json = $null
    )

    $httpMethod = New-Object System.Net.Http.HttpMethod($Method.ToUpper())
    $client = New-Object System.Net.Http.HttpClient
    $request = New-Object System.Net.Http.HttpRequestMessage($httpMethod, ($script:Base + $Path))

    if ($PSBoundParameters.ContainsKey('Json') -and $null -ne $Json) {
        $body = $Json | ConvertTo-Json -Compress -Depth 6
        $request.Content = New-Object System.Net.Http.StringContent(
            [string]$body, [System.Text.Encoding]::UTF8, 'application/json')
    }

    try {
        $response = $client.SendAsync($request).Result
    } catch {
        $client.Dispose()
        throw "HTTP $Method $Path failed: $($_.Exception.GetBaseException().Message)"
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

$suffix = [guid]::NewGuid().ToString('N').Substring(0, 8)
$categoryName = "T-ACCESS-$suffix"
$tabnum = "T-ACC-$suffix"
$categoryId = $null
$categoryDeleted = $false
$personCreated = $false

try {
    Write-Host "Access API smoke test: $($script:Base)" -ForegroundColor Cyan

    # 1. Список категорий доступен.
    $list = Api -Method GET -Path '/api/v2/Access/GetList?pageIndex=0&pageSize=10'
    Check 'GetList: 200' ($list.Status -eq 200) "status=$($list.Status) body=$($list.Body)"
    Check 'GetList: context is array' ($null -ne $list.Json.context) (Show $list.Json)

    # 2. Валидация названия.
    $empty = Api -Method POST -Path '/api/v2/Access' -Json @{ name = '' }
    Check 'POST empty name: 400' ($empty.Status -eq 400) "status=$($empty.Status) body=$($empty.Body)"

    $tooLong = Api -Method POST -Path '/api/v2/Access' -Json @{ name = ('x' * 51) }
    Check 'POST long name: 400' ($tooLong.Status -eq 400) "status=$($tooLong.Status) body=$($tooLong.Body)"

    # 3. Создание категории.
    $created = Api -Method POST -Path '/api/v2/Access' -Json @{ name = "  $categoryName  " }
    Check 'POST category: 200' ($created.Status -eq 200) "status=$($created.Status) body=$($created.Body)"
    Check 'POST category: id assigned' ($null -ne $created.Json.id_accessname -and [int]$created.Json.id_accessname -gt 0) (Show $created.Json)
    Check 'POST category: name trimmed' ($created.Json.name -eq $categoryName) (Show $created.Json)
    $categoryId = [int]$created.Json.id_accessname

    # 4. Поиск категории в списке.
    $found = Api -Method GET -Path ('/api/v2/Access/GetList?pageIndex=0&pageSize=100&AccessName=' + [uri]::EscapeDataString($categoryName))
    Check 'GetList filter: 200' ($found.Status -eq 200) "status=$($found.Status) body=$($found.Body)"
    $foundIds = @($found.Json.context | ForEach-Object { [int]$_.id_accessname })
    Check 'GetList filter: category present' ($foundIds -contains $categoryId) (Show $found.Json)

    # 5. У категории ещё нет сотрудников.
    $emptyPeople = Api -Method GET -Path ('/api/v2/Access/GetPeople?id_accessname=' + $categoryId)
    Check 'GetPeople empty: 200' ($emptyPeople.Status -eq 200) "status=$($emptyPeople.Status) body=$($emptyPeople.Body)"
    Check 'GetPeople empty: empty context' (@($emptyPeople.Json.context).Count -eq 0) (Show $emptyPeople.Json)

    $missingParam = Api -Method GET -Path '/api/v2/Access/GetPeople'
    Check 'GetPeople without id: 400' ($missingParam.Status -eq 400) "status=$($missingParam.Status) body=$($missingParam.Body)"

    # 6. Создаём сотрудника и назначаем ему категорию.
    $person = Api -Method POST -Path '/api/v2/Person' -Json @{
        surname = 'AccessTest'
        name = 'API'
        tabnum = $tabnum
    }
    Check 'POST person: 200' ($person.Status -eq 200) "status=$($person.Status) body=$($person.Body)"
    $personCreated = $true

    $assigned = Api -Method POST -Path '/api/v2/Person/AddAccess' -Json @{
        tabnum = $tabnum
        id_accessname = $categoryId
    }
    Check 'AddAccess: 200' ($assigned.Status -eq 200) "status=$($assigned.Status) body=$($assigned.Body)"

    # 7. Сотрудник виден в категории, в ответе только tabnum.
    $people = Api -Method GET -Path ('/api/v2/Access/GetPeople?id_accessname=' + $categoryId + '&pageIndex=0&pageSize=10')
    Check 'GetPeople: 200' ($people.Status -eq 200) "status=$($people.Status) body=$($people.Body)"
    Check 'GetPeople: one person' (@($people.Json.context).Count -eq 1) (Show $people.Json)
    Check 'GetPeople: tabnum returned' ($people.Json.context[0].tabnum -eq $tabnum) (Show $people.Json)

    $page = Api -Method GET -Path ('/api/v2/Access/GetPeople?id_accessname=' + $categoryId + '&pageIndex=0&pageSize=1')
    Check 'GetPeople pagination: pageCount=1' ($page.Json.pageCount -eq 1) (Show $page.Json)

    # 8. Защищённая и несуществующая категории.
    $protected = Api -Method DELETE -Path '/api/v2/Access/1'
    Check 'DELETE protected category: 409' ($protected.Status -eq 409) "status=$($protected.Status) body=$($protected.Body)"

    $missing = Api -Method DELETE -Path '/api/v2/Access/2147483000'
    Check 'DELETE missing category: 404' ($missing.Status -eq 404) "status=$($missing.Status) body=$($missing.Body)"

    $negative = Api -Method DELETE -Path '/api/v2/Access/-1'
    Check 'DELETE negative id: 400' ($negative.Status -eq 400) "status=$($negative.Status) body=$($negative.Body)"

    # 9. Удаление категории: связь с сотрудником удаляется каскадно.
    $deleted = Api -Method DELETE -Path ('/api/v2/Access/' + $categoryId)
    Check 'DELETE category: 200' ($deleted.Status -eq 200) "status=$($deleted.Status) body=$($deleted.Body)"
    Check 'DELETE category: same id' ([int]$deleted.Json.id_accessname -eq $categoryId) (Show $deleted.Json)
    $categoryDeleted = $true

    $peopleAfter = Api -Method GET -Path ('/api/v2/Access/GetPeople?id_accessname=' + $categoryId)
    Check 'GetPeople after delete: 404' ($peopleAfter.Status -eq 404) "status=$($peopleAfter.Status) body=$($peopleAfter.Body)"

    $personAfter = Api -Method GET -Path ('/api/v2/Person?tabnum=' + $tabnum)
    Check 'Person survives category delete: 200' ($personAfter.Status -eq 200) "status=$($personAfter.Status) body=$($personAfter.Body)"

    $listAfter = Api -Method GET -Path ('/api/v2/Access/GetList?pageIndex=0&pageSize=100&AccessName=' + [uri]::EscapeDataString($categoryName))
    Check 'GetList after delete: empty' (@($listAfter.Json.context).Count -eq 0) (Show $listAfter.Json)

    $again = Api -Method DELETE -Path ('/api/v2/Access/' + $categoryId)
    Check 'DELETE twice: 404' ($again.Status -eq 404) "status=$($again.Status) body=$($again.Body)"
}
finally {
    if ($personCreated) {
        [void](Api -Method DELETE -Path ('/api/v2/Person?tabnum=' + $tabnum))
    }

    if ($null -ne $categoryId -and -not $categoryDeleted) {
        [void](Api -Method DELETE -Path ('/api/v2/Access/' + $categoryId))
    }
}

Write-Host ("Passed: {0}; failed: {1}" -f $script:Pass, $script:Fail) -ForegroundColor Green
if ($script:Fail -gt 0) {
    $script:Failures | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    exit 1
}
exit 0
