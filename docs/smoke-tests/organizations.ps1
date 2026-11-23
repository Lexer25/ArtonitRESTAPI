param(
    [string]$BaseUrl = 'http://127.0.0.1:5056'
)

# Дымовой тест REST API v2 для организаций: проверяет контракт из
# docs\Команды REST организации (v1.2).docx на работающем экземпляре API.
# Запуск: powershell -ExecutionPolicy Bypass -File docs\smoke-tests\organizations.ps1 `
#            -BaseUrl http://127.0.0.1:5056
# Тест создаёт и удаляет организации с кодами T-* и сотрудников с tabnum T-*.

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
        [string]$Token = $null,
        [object]$Json = $null
    )

    $httpMethod = New-Object System.Net.Http.HttpMethod($Method.ToUpper())
    $client = New-Object System.Net.Http.HttpClient
    $request = New-Object System.Net.Http.HttpRequestMessage($httpMethod, ($script:Base + $Path))

    if ($Token) {
        [void]$request.Headers.TryAddWithoutValidation('Authorization', "Bearer $Token")
    }

    if ($PSBoundParameters.ContainsKey('Json') -and $null -ne $Json) {
        $request.Content = New-Object System.Net.Http.StringContent([string]$Json, [System.Text.Encoding]::UTF8, 'application/json')
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
        Status   = [int]$response.StatusCode
        Body     = $body
        Json     = $parsed
        Location = if ($response.Headers.Location) { $response.Headers.Location.ToString() } else { $null }
    }

    $client.Dispose()
    return $result
}

function Login {
    param([string]$User, [string]$Password)
    $res = Api -Method GET -Path "/login?username=$User&pasword=$Password"
    if ($res.Status -ne 200) { throw "login failed: $($res.Status) $($res.Body)" }
    return $res.Json.data.token
}

function Show($json) {
    if ($null -eq $json) { return '<null>' }
    return ($json | ConvertTo-Json -Compress -Depth 6)
}

function RemoveTestData {
    param([string]$AdminToken)

    $persons = Api -Method GET -Path '/api/v2/Person/GetList?pageIndex=0&pageSize=1000' -Token $AdminToken
    foreach ($person in @($persons.Json.context | Where-Object { $_.tabnum -like 'T-*' })) {
        [void](Api -Method DELETE -Path ('/api/v2/Person?tabnum=' + $person.tabnum))
    }

    for ($pass = 0; $pass -lt 8; $pass++) {
        $list = Api -Method GET -Path '/api/v2/organizations?pageSize=100' -Token $AdminToken
        $remaining = @($list.Json.items | Where-Object { $_.divcode -like 'T-*' })
        if ($remaining.Count -eq 0) { return }
        foreach ($org in ($remaining | Sort-Object { $_.divcode.Length } -Descending)) {
            [void](Api -Method DELETE -Path ('/api/v2/organizations/' + $org.divcode) -Token $AdminToken)
        }
    }
}

Write-Host '=== Авторизация ==='
$admin = Login -User 'ADMIN' -Password '333'
$user = Login -User 'operator' -Password '333'
Check 'auth: admin token issued' ($admin.Length -gt 50) $admin
Check 'auth: user token issued' ($user.Length -gt 50) $user

Write-Host '=== Подготовка: очистка тестовых данных ==='
RemoveTestData -AdminToken $admin
$baseline = Api -Method GET -Path '/api/v2/organizations?pageSize=100' -Token $admin
$baselineCodes = @($baseline.Json.items | ForEach-Object { $_.divcode })
Check 'baseline: list available' ($baseline.Status -eq 200) (Show $baseline.Json)

Write-Host '=== 401 / 403 ==='
$noToken = Api -Method GET -Path '/api/v2/organizations'
Check 'no token: 401' ($noToken.Status -eq 401) (Show $noToken.Json)
Check 'no token: UNAUTHORIZED' ($noToken.Json.error -eq 'UNAUTHORIZED') (Show $noToken.Json)
Check 'no token: russian message' ($noToken.Json.message -eq 'Требуется авторизация') (Show $noToken.Json)

$userCreate = Api -Method POST -Path '/api/v2/organizations' -Token $user -Json '{"divcode":"T-DENIED","name":"Denied"}'
Check 'non-admin write: 403' ($userCreate.Status -eq 403) (Show $userCreate.Json)
Check 'non-admin write: FORBIDDEN' ($userCreate.Json.error -eq 'FORBIDDEN') (Show $userCreate.Json)
Check 'non-admin write: message' ($userCreate.Json.message -eq 'Недостаточно прав: требуется роль администратора') (Show $userCreate.Json)
Check 'non-admin write: organisation not created' ((Api -Method GET -Path '/api/v2/organizations/T-DENIED' -Token $admin).Status -eq 404) 'T-DENIED must not exist'

$userRead = Api -Method GET -Path '/api/v2/organizations/dv_Guest' -Token $user
Check 'non-admin read: 200' ($userRead.Status -eq 200) (Show $userRead.Json)

Write-Host '=== POST /api/v2/organizations ==='
$create = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-A","name":"ООО «Тест А»","parent":"dv_Guest","flag":0}'
Check 'POST: 201' ($create.Status -eq 201) (Show $create.Json)
Check 'POST: divcode' ($create.Json.divcode -eq 'T-A') (Show $create.Json)
Check 'POST: name' ($create.Json.name -eq 'ООО «Тест А»') (Show $create.Json)
Check 'POST: parent' ($create.Json.parent -eq 'dv_Guest') (Show $create.Json)
Check 'POST: flag' ($create.Json.flag -eq 0) (Show $create.Json)
Check 'POST: id' ($create.Json.id -gt 0) (Show $create.Json)
Check 'POST: createdAt present' ($null -ne $create.Json.createdAt) (Show $create.Json)
Check 'POST: updatedAt present' ($null -ne $create.Json.updatedAt) (Show $create.Json)
Check 'POST: Location header' ($create.Location -eq '/api/v2/organizations/T-A') "location=$($create.Location)"

$dup = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-A","name":"Duplicate"}'
Check 'POST duplicate: 409' ($dup.Status -eq 409) (Show $dup.Json)
Check 'POST duplicate: DUPLICATE_DIVCODE' ($dup.Json.error -eq 'DUPLICATE_DIVCODE') (Show $dup.Json)

$dupCase = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"t-a","name":"Duplicate case"}'
Check 'POST duplicate (case-insensitive): 409' ($dupCase.Status -eq 409) (Show $dupCase.Json)

$badCode = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"Т-А","name":"Cyrillic"}'
Check 'POST bad divcode: 400' ($badCode.Status -eq 400) (Show $badCode.Json)
Check 'POST bad divcode: VALIDATION_ERROR' ($badCode.Json.error -eq 'VALIDATION_ERROR') (Show $badCode.Json)
Check 'POST bad divcode: detail field' ($badCode.Json.details[0].field -eq 'divcode') (Show $badCode.Json)

$noName = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-NONAME"}'
Check 'POST without name: 400' ($noName.Status -eq 400) (Show $noName.Json)
Check 'POST without name: name issue' ($noName.Json.details[0].field -eq 'name' -and $noName.Json.details[0].issue -eq 'required') (Show $noName.Json)

$noCode = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"name":"No code"}'
Check 'POST without divcode: 400' ($noCode.Status -eq 400) (Show $noCode.Json)
Check 'POST without divcode: message' ($noCode.Json.message -eq 'Поле divcode обязательно') (Show $noCode.Json)

$badFlag = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-FLAG","name":"Bad flag","flag":2}'
Check 'POST bad flag: 400' ($badFlag.Status -eq 400) (Show $badFlag.Json)
Check 'POST bad flag: field' ($badFlag.Json.details[0].field -eq 'flag') (Show $badFlag.Json)

$badParent = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-NOPARENT","name":"No parent","parent":"ORG-999"}'
Check 'POST missing parent: 422' ($badParent.Status -eq 422) (Show $badParent.Json)
Check 'POST missing parent: PARENT_NOT_FOUND' ($badParent.Json.error -eq 'PARENT_NOT_FOUND') (Show $badParent.Json)

$invalidJson = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode": "T-BAD",'
Check 'POST invalid json: 400' ($invalidJson.Status -eq 400) (Show $invalidJson.Json)
Check 'POST invalid json: INVALID_JSON' ($invalidJson.Json.error -eq 'INVALID_JSON') (Show $invalidJson.Json)

$emptyBody = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json ''
Check 'POST empty body: 400' ($emptyBody.Status -eq 400) (Show $emptyBody.Json)
Check 'POST empty body: VALIDATION_ERROR' ($emptyBody.Json.error -eq 'VALIDATION_ERROR') (Show $emptyBody.Json)

$arrayBody = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '[{"divcode":"T-ARR"}]'
Check 'POST array body: 400 INVALID_JSON' ($arrayBody.Status -eq 400 -and $arrayBody.Json.error -eq 'INVALID_JSON') (Show $arrayBody.Json)

$ignored = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-IGN","name":"Ignored service fields","id":999,"createdAt":"2020-01-01T00:00:00Z","updatedAt":"2020-01-01T00:00:00Z"}'
Check 'POST service fields ignored: 201' ($ignored.Status -eq 201) (Show $ignored.Json)
Check 'POST service fields ignored: id generated' ($ignored.Json.id -ne 999 -and $ignored.Json.id -gt 0) (Show $ignored.Json)
Check 'POST service fields ignored: createdAt not 2020' ($ignored.Json.createdAt -gt '2021-01-01') (Show $ignored.Json)

Write-Host '=== GET /api/v2/organizations/{divcode} ==='
$get = Api -Method GET -Path '/api/v2/organizations/T-A' -Token $admin
Check 'GET: 200' ($get.Status -eq 200) (Show $get.Json)
Check 'GET: divcode' ($get.Json.divcode -eq 'T-A') (Show $get.Json)
Check 'GET: 7 fields' (@($get.Json.PSObject.Properties.Name).Count -eq 7) ((@($get.Json.PSObject.Properties.Name)) -join ',')

$caseGet = Api -Method GET -Path '/api/v2/organizations/t-a' -Token $admin
Check 'GET case-insensitive: 200' ($caseGet.Status -eq 200) (Show $caseGet.Json)

$missing = Api -Method GET -Path '/api/v2/organizations/ORG-999' -Token $admin
Check 'GET missing: 404' ($missing.Status -eq 404) (Show $missing.Json)
Check 'GET missing: NOT_FOUND' ($missing.Json.error -eq 'NOT_FOUND') (Show $missing.Json)
Check 'GET missing: message' ($missing.Json.message -eq 'Организация с divcode=ORG-999 не найдена') (Show $missing.Json)

$badGet = Api -Method GET -Path '/api/v2/organizations/%D0%A2' -Token $admin
Check 'GET bad divcode: 400' ($badGet.Status -eq 400) (Show $badGet.Json)

Write-Host '=== PUT /api/v2/organizations/{divcode} ==='
Start-Sleep -Seconds 2
$idBefore = $get.Json.id
$put = Api -Method PUT -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"name":"ООО «Тест А» (изм.)","flag":1}'
Check 'PUT: 200' ($put.Status -eq 200) (Show $put.Json)
Check 'PUT: name' ($put.Json.name -eq 'ООО «Тест А» (изм.)') (Show $put.Json)
Check 'PUT: flag' ($put.Json.flag -eq 1) (Show $put.Json)
Check 'PUT: parent reset to null' ($null -eq $put.Json.parent) (Show $put.Json)
Check 'PUT: id unchanged' ($put.Json.id -eq $idBefore) (Show $put.Json)
Check 'PUT: updatedAt changed' ($put.Json.updatedAt -gt $get.Json.updatedAt) "before=$($get.Json.updatedAt) after=$($put.Json.updatedAt)"
Check 'PUT: createdAt <= updatedAt' ($put.Json.createdAt -le $put.Json.updatedAt) "created=$($put.Json.createdAt) updated=$($put.Json.updatedAt)"

$putParent = Api -Method PUT -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"name":"X","parent":"ORG-999"}'
Check 'PUT missing parent: 422' ($putParent.Status -eq 422) (Show $putParent.Json)
Check 'PUT missing parent: PARENT_NOT_FOUND' ($putParent.Json.error -eq 'PARENT_NOT_FOUND') (Show $putParent.Json)

$putImmutable = Api -Method PUT -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"divcode":"T-OTHER","name":"X"}'
Check 'PUT other divcode: 400' ($putImmutable.Status -eq 400) (Show $putImmutable.Json)
Check 'PUT other divcode: DIVCODE_IMMUTABLE' ($putImmutable.Json.error -eq 'DIVCODE_IMMUTABLE') (Show $putImmutable.Json)

$putSame = Api -Method PUT -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"divcode":"T-A","name":"ООО «Тест А»","flag":0}'
Check 'PUT same divcode: 200' ($putSame.Status -eq 200) (Show $putSame.Json)

$putNoName = Api -Method PUT -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"flag":0}'
Check 'PUT without name: 400' ($putNoName.Status -eq 400) (Show $putNoName.Json)

$putMissing = Api -Method PUT -Path '/api/v2/organizations/ORG-999' -Token $admin -Json '{"name":"X"}'
Check 'PUT missing org: 404' ($putMissing.Status -eq 404) (Show $putMissing.Json)

Write-Host '=== PATCH /api/v2/organizations/{divcode} ==='
$patch = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"name":"ООО «Тест А-2»"}'
Check 'PATCH: 200' ($patch.Status -eq 200) (Show $patch.Json)
Check 'PATCH: name changed' ($patch.Json.name -eq 'ООО «Тест А-2»') (Show $patch.Json)
Check 'PATCH: flag kept' ($patch.Json.flag -eq 0) (Show $patch.Json)

$patchFlag = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"flag":1}'
Check 'PATCH flag: 200' ($patchFlag.Status -eq 200 -and $patchFlag.Json.flag -eq 1) (Show $patchFlag.Json)
Check 'PATCH flag: name kept' ($patchFlag.Json.name -eq 'ООО «Тест А-2»') (Show $patchFlag.Json)

$patchEmpty = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{}'
Check 'PATCH empty: 400' ($patchEmpty.Status -eq 400) (Show $patchEmpty.Json)
Check 'PATCH empty: EMPTY_PATCH' ($patchEmpty.Json.error -eq 'EMPTY_PATCH') (Show $patchEmpty.Json)

$patchNameNull = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"name":null}'
Check 'PATCH name null: 400' ($patchNameNull.Status -eq 400) (Show $patchNameNull.Json)
Check 'PATCH name null: field' ($patchNameNull.Json.details[0].field -eq 'name') (Show $patchNameNull.Json)

$patchFlagNull = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"flag":null}'
Check 'PATCH flag null: 400' ($patchFlagNull.Status -eq 400) (Show $patchFlagNull.Json)

$patchDivcode = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"divcode":"T-Z"}'
Check 'PATCH divcode: 400' ($patchDivcode.Status -eq 400) (Show $patchDivcode.Json)
Check 'PATCH divcode: DIVCODE_IMMUTABLE' ($patchDivcode.Json.error -eq 'DIVCODE_IMMUTABLE') (Show $patchDivcode.Json)

$patchParentNull = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"parent":null}'
Check 'PATCH parent null: 200' ($patchParentNull.Status -eq 200) (Show $patchParentNull.Json)
Check 'PATCH parent null: parent null' ($null -eq $patchParentNull.Json.parent) (Show $patchParentNull.Json)

$patchParentBack = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"parent":"dv_Guest"}'
Check 'PATCH parent: 200' ($patchParentBack.Status -eq 200 -and $patchParentBack.Json.parent -eq 'dv_Guest') (Show $patchParentBack.Json)

$patchParentMissing = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"parent":"ORG-999"}'
Check 'PATCH missing parent: 422' ($patchParentMissing.Status -eq 422) (Show $patchParentMissing.Json)

$patchSameDivcode = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"divcode":"T-A"}'
Check 'PATCH same divcode only: 400 EMPTY_PATCH' ($patchSameDivcode.Status -eq 400 -and $patchSameDivcode.Json.error -eq 'EMPTY_PATCH') (Show $patchSameDivcode.Json)

$patchMissing = Api -Method PATCH -Path '/api/v2/organizations/ORG-999' -Token $admin -Json '{"name":"X"}'
Check 'PATCH missing org: 404' ($patchMissing.Status -eq 404) (Show $patchMissing.Json)

Write-Host '=== Иерархия и CYCLE ==='
$createB = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-B","name":"Тест Б","parent":"T-A"}'
Check 'POST T-B: 201' ($createB.Status -eq 201) (Show $createB.Json)
$createC = Api -Method POST -Path '/api/v2/organizations' -Token $admin -Json '{"divcode":"T-C","name":"Тест Ц","parent":"T-B"}'
Check 'POST T-C: 201' ($createC.Status -eq 201) (Show $createC.Json)

$cycleDeep = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"parent":"T-C"}'
Check 'PATCH cycle (grandchild): 409' ($cycleDeep.Status -eq 409) (Show $cycleDeep.Json)
Check 'PATCH cycle (grandchild): CYCLE' ($cycleDeep.Json.error -eq 'CYCLE') (Show $cycleDeep.Json)

$cycleSelf = Api -Method PATCH -Path '/api/v2/organizations/T-A' -Token $admin -Json '{"parent":"T-A"}'
Check 'PATCH cycle (self): 409 CYCLE' ($cycleSelf.Status -eq 409 -and $cycleSelf.Json.error -eq 'CYCLE') (Show $cycleSelf.Json)

$putCycle = Api -Method PUT -Path '/api/v2/organizations/T-B' -Token $admin -Json '{"name":"Тест Б","parent":"T-C"}'
Check 'PUT cycle: 409 CYCLE' ($putCycle.Status -eq 409 -and $putCycle.Json.error -eq 'CYCLE') (Show $putCycle.Json)

$rootParent = Api -Method PATCH -Path '/api/v2/organizations/dv_All' -Token $admin -Json '{"parent":"T-A"}'
Check 'root org parent: 409 CYCLE' ($rootParent.Status -eq 409 -and $rootParent.Json.error -eq 'CYCLE') (Show $rootParent.Json)

Write-Host '=== GET /api/v2/organizations (список) ==='
$roots = Api -Method GET -Path '/api/v2/organizations?parent=' -Token $admin
Check 'list parent=: 200' ($roots.Status -eq 200) (Show $roots.Json)
Check 'list parent=: only roots' (@($roots.Json.items | Where-Object { $null -ne $_.parent }).Count -eq 0) ((@($roots.Json.items | ForEach-Object { "$($_.divcode):$($_.parent)" })) -join ';')
Check 'list parent=: total matches items' ($roots.Json.total -eq @($roots.Json.items).Count) "total=$($roots.Json.total) items=$(@($roots.Json.items).Count)"
Check 'list parent=: paging fields' ($roots.Json.page -eq 1 -and $roots.Json.pageSize -eq 20) (Show $roots.Json)

$children = Api -Method GET -Path '/api/v2/organizations?parent=T-A' -Token $admin
Check 'list parent=T-A: 1 child' (@($children.Json.items).Count -eq 1 -and $children.Json.items[0].divcode -eq 'T-B') (Show $children.Json)

$missingParent = Api -Method GET -Path '/api/v2/organizations?parent=ORG-999' -Token $admin
Check 'list unknown parent: 200 empty' ($missingParent.Status -eq 200 -and @($missingParent.Json.items).Count -eq 0) (Show $missingParent.Json)

$search = Api -Method GET -Path '/api/v2/organizations?name=%D0%A2%D0%B5%D1%81%D1%82' -Token $admin
Check 'list name search: found' (@($search.Json.items).Count -ge 3) (Show $search.Json)

$searchLower = Api -Method GET -Path '/api/v2/organizations?name=%D1%82%D0%B5%D1%81%D1%82' -Token $admin
Check 'list name search (lowercase): found' (@($searchLower.Json.items).Count -ge 3) (Show $searchLower.Json)

$searchNone = Api -Method GET -Path '/api/v2/organizations?name=zzz-no-such' -Token $admin
Check 'list name search empty: 200' ($searchNone.Status -eq 200 -and $searchNone.Json.total -eq 0) (Show $searchNone.Json)

$sortDesc = Api -Method GET -Path '/api/v2/organizations?sort=-divcode&pageSize=100' -Token $admin
$codes = @($sortDesc.Json.items | ForEach-Object { $_.divcode })
$sorted = @($codes | Sort-Object -Descending)
Check 'list sort -divcode: ordered' (($codes -join '|') -eq ($sorted -join '|')) ($codes -join ',')

$page1 = Api -Method GET -Path '/api/v2/organizations?page=1&pageSize=2&sort=divcode' -Token $admin
$page2 = Api -Method GET -Path '/api/v2/organizations?page=2&pageSize=2&sort=divcode' -Token $admin
Check 'list page1 size' (@($page1.Json.items).Count -eq 2) (Show $page1.Json)
Check 'list page2 differs' ($page1.Json.items[0].divcode -ne $page2.Json.items[0].divcode) "p1=$($page1.Json.items[0].divcode) p2=$($page2.Json.items[0].divcode)"

$badPage = Api -Method GET -Path '/api/v2/organizations?page=0' -Token $admin
Check 'list page=0: 400' ($badPage.Status -eq 400) (Show $badPage.Json)
$badSize = Api -Method GET -Path '/api/v2/organizations?pageSize=101' -Token $admin
Check 'list pageSize=101: 400' ($badSize.Status -eq 400) (Show $badSize.Json)
$badSort = Api -Method GET -Path '/api/v2/organizations?sort=unknown' -Token $admin
Check 'list bad sort: 400' ($badSort.Status -eq 400 -and $badSort.Json.details[0].field -eq 'sort') (Show $badSort.Json)
$badPageType = Api -Method GET -Path '/api/v2/organizations?page=abc' -Token $admin
Check 'list page=abc: 400' ($badPageType.Status -eq 400) (Show $badPageType.Json)

$sortCreated = Api -Method GET -Path '/api/v2/organizations?sort=-createdAt&pageSize=100' -Token $admin
Check 'list sort -createdAt: 200' ($sortCreated.Status -eq 200 -and @($sortCreated.Json.items).Count -gt 0) (Show $sortCreated.Json)

$wildcard = Api -Method GET -Path '/api/v2/organizations?name=%25' -Token $admin
Check 'list name=%: wildcard escaped' ($wildcard.Status -eq 200 -and $wildcard.Json.total -eq 0) (Show $wildcard.Json)

Write-Host '=== DELETE /api/v2/organizations/{divcode} ==='
$delWithChild = Api -Method DELETE -Path '/api/v2/organizations/T-A' -Token $admin
Check 'DELETE with children: 409' ($delWithChild.Status -eq 409) (Show $delWithChild.Json)
Check 'DELETE with children: HAS_CHILDREN' ($delWithChild.Json.error -eq 'HAS_CHILDREN') (Show $delWithChild.Json)
Check 'DELETE with children: divcode' ($delWithChild.Json.divcode -eq 'T-A') (Show $delWithChild.Json)
Check 'DELETE with children: childrenCount' ($delWithChild.Json.childrenCount -eq 1) (Show $delWithChild.Json)
Check 'DELETE with children: children list' ($delWithChild.Json.children[0] -eq 'T-B') (Show $delWithChild.Json)

$delRoot = Api -Method DELETE -Path '/api/v2/organizations/dv_All' -Token $admin
Check 'DELETE root: 409 ROOT_ORGANIZATION' ($delRoot.Status -eq 409 -and $delRoot.Json.error -eq 'ROOT_ORGANIZATION') (Show $delRoot.Json)

$delC = Api -Method DELETE -Path '/api/v2/organizations/T-C' -Token $admin
Check 'DELETE leaf: 204' ($delC.Status -eq 204) (Show $delC.Json)
Check 'DELETE leaf: empty body' ([string]::IsNullOrEmpty($delC.Body)) "body=$($delC.Body)"
$delC2 = Api -Method DELETE -Path '/api/v2/organizations/T-C' -Token $admin
Check 'DELETE twice: 404' ($delC2.Status -eq 404) (Show $delC2.Json)

Write-Host '=== PEOPLE: занятость организации ==='
$personB = Api -Method POST -Path '/api/v2/Person' -Json '{"surname":"Test","name":"Test","tabnum":"T-P1"}'
Check 'POST person: 200' ($personB.Status -eq 200) (Show $personB.Json)

$assignB = Api -Method PATCH -Path '/api/v2/Person' -Json ('{"tabnum":"T-P1","id_org":' + $createB.Json.id + '}')
Check 'PATCH person org: 200' ($assignB.Status -eq 200) (Show $assignB.Json)

$delWithPeople = Api -Method DELETE -Path '/api/v2/organizations/T-B' -Token $admin
Check 'DELETE with people: 409' ($delWithPeople.Status -eq 409) (Show $delWithPeople.Json)
Check 'DELETE with people: HAS_PEOPLE' ($delWithPeople.Json.error -eq 'HAS_PEOPLE') (Show $delWithPeople.Json)
Check 'DELETE with people: peopleCount' ($delWithPeople.Json.peopleCount -eq 1) (Show $delWithPeople.Json)
Check 'DELETE with people: people list' ($delWithPeople.Json.people[0] -eq 'T-P1') (Show $delWithPeople.Json)

$personA = Api -Method POST -Path '/api/v2/Person' -Json '{"surname":"Test2","name":"Test2","tabnum":"T-P2"}'
$assignA = Api -Method PATCH -Path '/api/v2/Person' -Json ('{"tabnum":"T-P2","id_org":' + $create.Json.id + '}')
Check 'PATCH person org T-A: 200' ($personA.Status -eq 200 -and $assignA.Status -eq 200) (Show $assignA.Json)

$delBoth = Api -Method DELETE -Path '/api/v2/organizations/T-A' -Token $admin
Check 'DELETE with children and people: 409' ($delBoth.Status -eq 409) (Show $delBoth.Json)
Check 'DELETE with children and people: code' ($delBoth.Json.error -eq 'HAS_CHILDREN_AND_PEOPLE') (Show $delBoth.Json)
Check 'DELETE with children and people: counts' ($delBoth.Json.childrenCount -eq 1 -and $delBoth.Json.peopleCount -eq 1) (Show $delBoth.Json)

Write-Host '=== Очистка ==='
RemoveTestData -AdminToken $admin
$finalList = Api -Method GET -Path '/api/v2/organizations?pageSize=100' -Token $admin
$finalCodes = @($finalList.Json.items | ForEach-Object { $_.divcode })
Check 'cleanup: no T-* left' (@($finalCodes | Where-Object { $_ -like 'T-*' }).Count -eq 0) ($finalCodes -join ',')
Check 'cleanup: baseline restored' (($finalCodes -join '|') -eq ($baselineCodes -join '|')) "before=$($baselineCodes -join ',') after=$($finalCodes -join ',')"

Write-Host ''
Write-Host ("ИТОГО: успешно {0}, ошибок {1}" -f $script:Pass, $script:Fail)
if ($script:Fail -gt 0) {
    Write-Host 'Проваленные проверки:'
    $script:Failures | ForEach-Object { Write-Host " - $_" }
    exit 1
}
exit 0
