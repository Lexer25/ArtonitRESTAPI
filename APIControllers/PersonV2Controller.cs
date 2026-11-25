using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Model;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для сотрудников. Вместо id_pep используется tabnum.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Person")]
    public class PersonV2Controller : V2ControllerBase
    {
        /// <summary>
        /// Максимальное число категорий доступа в одном запросе сохранения набора.
        /// </summary>
        private const int MaxAccessNamesCount = 1000;

        /// <summary>
        /// Получить информацию о сотруднике по tabnum.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get([FromQuery(Name = "tabnum")] string? tabnum)
        {
            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var (result, _) = PersonDBController.GetById(lookup.IdPep, User);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            if (result.Value is not List<PersonGet> persons || persons.Count == 0)
            {
                return NotFound($"Сотрудник с tabnum '{lookup.TabNum}' не найден");
            }

            return Ok(persons.Select(ToV2).ToList());
        }

        /// <summary>
        /// Получить статистику по сотрудникам: количество всех записей в таблице
        /// PEOPLE, количество активных (ACTIVE = 1) и неактивных (ACTIVE = 0).
        /// </summary>
        [HttpGet(nameof(GetCount))]
        [ProducesResponseType(typeof(PersonV2Count), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetCount()
        {
            return DataBaseStatusToWebStatusCode(PersonV2DBController.GetCount());
        }

        /// <summary>
        /// Получить список сотрудников. Список разбит на страницы.
        /// Параметр active: не указан — все записи, true/1 — ACTIVE = 1,
        /// false/0 — ACTIVE = 0.
        /// </summary>
        /// <param name="pageIndex">Номер страницы, начиная с 0. По умолчанию 0.</param>
        /// <param name="pageSize">Размер страницы от 1 до 1000. По умолчанию 10.</param>
        /// <param name="active">Фильтр по признаку активности: all, active, inactive, 1, 0, true или false.</param>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(
            int pageIndex = 0,
            int pageSize = 10,
            [FromQuery(Name = "active")] string? active = null)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            if (!TryParseActiveFilter(active, out var activeFilter, out var activeError))
            {
                return ApiErrorResults.Validation("active", "invalid_value", activeError);
            }

            var request = PersonV2DBController.GetList(pageIndex, pageSize, activeFilter);
            var result = request.Item1;

            if (result.State == State.Successes && result.Value is List<PersonGet> persons)
            {
                result.Value = new Pagination(
                    persons.Select(ToV2).ToList(),
                    pageIndex,
                    pageSize,
                    request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить сотрудника. Если tabnum не указан, он формируется как tn_&lt;id_pep&gt;.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Add([FromBody] PersonV2PostSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2PostSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var result = PersonV2DBController.Add(new PersonV2Post(body));
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить категорию доступа сотруднику, найденному по tabnum.
        /// </summary>
        [HttpPost(nameof(AddAccess))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddAccess([FromBody] PersonV2AddAccessSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2AddAccessSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = PersonV2DBController.AddAccess(lookup.IdPep, body.Id_accessname!.Value);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Получить набор категорий доступа сотрудника, найденного по tabnum.
        /// Источник — таблица связей SS_ACCESSUSER.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника.</param>
        [HttpGet("Access")]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccess([FromQuery(Name = "tabnum")] string? tabnum)
        {
            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return PersonLookupError(lookup);

            var result = PersonV2DBController.GetAccessNames(lookup.IdPep);

            return result.Status switch
            {
                PersonAccessOperationStatus.Success => Ok(result.Items),
                PersonAccessOperationStatus.NotFound => ApiErrorResults.PersonNotFound(lookup.TabNum),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Сохранить набор категорий доступа сотрудника: полная замена связей
        /// в SS_ACCESSUSER. Недостающие связи добавляются, лишние удаляются;
        /// пустой массив очищает набор.
        /// </summary>
        /// <param name="body">Табельный номер и полный набор: поле accessnames.</param>
        [HttpPut("Access")]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult SaveAccess([FromBody] PersonV2AccessPutSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var validation = new PersonV2AccessPutSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            if (body.AccessNames == null)
            {
                return ApiErrorResults.Validation(
                    "accessnames", "required",
                    "Поле accessnames обязательно: передайте массив идентификаторов категорий доступа или пустой массив");
            }

            if (body.AccessNames.Count > MaxAccessNamesCount)
            {
                return ApiErrorResults.Validation(
                    "accessnames", "too_long",
                    $"Поле accessnames не должно содержать больше {MaxAccessNamesCount} идентификаторов");
            }

            if (body.AccessNames.Any(id => id < 0))
            {
                return ApiErrorResults.Validation(
                    "accessnames", "invalid_value",
                    "Идентификаторы категорий доступа не могут быть отрицательными");
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return PersonLookupError(lookup);

            var result = PersonV2DBController.SaveAccessNames(lookup.IdPep, body.AccessNames);

            return result.Status switch
            {
                PersonAccessOperationStatus.Success => Ok(result.Items),
                PersonAccessOperationStatus.NotFound => ApiErrorResults.PersonNotFound(lookup.TabNum),
                PersonAccessOperationStatus.AccessNameNotFound => ApiErrorResults.AccessNamesNotFound(
                    result.MissingAccessNames),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Добавить идентификатор сотруднику, найденному по tabnum.
        /// </summary>
        [HttpPost(nameof(AddIdentifier))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddIdentifier([FromBody] IdentifierV2PostSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new IdentifierV2PostSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = IdentifierV2DBController.Add(body, lookup.IdPep);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Изменить информацию о сотруднике, найденном по tabnum.
        /// </summary>
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Update([FromBody] PersonV2Patch? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2PatchValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = PersonV2DBController.Update(lookup.IdPep, body);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Полностью заменить ФИО сотрудника, найденного по tabnum:
        /// фамилия обязательна, имя и отчество могут быть пустыми.
        /// </summary>
        /// <param name="body">Табельный номер и новые значения: surname, name, patronymic.</param>
        [HttpPut]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(PersonV2Get), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult ReplaceName([FromBody] PersonV2PutSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var validation = new PersonV2PutSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return PersonLookupError(lookup);

            var result = PersonV2DBController.UpdateName(lookup.IdPep, body);

            return result.State switch
            {
                State.Successes => Ok(new PersonV2Get
                {
                    TabNum = lookup.TabNum,
                    Surname = body.Surname.Trim(),
                    Name = (body.Name ?? string.Empty).Trim(),
                    Patronymic = (body.Patronymic ?? string.Empty).Trim()
                }),
                State.NotFound => ApiErrorResults.PersonNotFound(lookup.TabNum),
                State.BadSQLRequest => BadRequest(result.ErrorMessage),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Удалить сотрудника по tabnum.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete([FromQuery(Name = "tabnum")] string? tabnum)
        {
            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            return DataBaseStatusToWebStatusCode(PersonV2DBController.Delete(lookup.IdPep));
        }

        /// <summary>
        /// Удалить идентификатор по номеру карты.
        /// </summary>
        [HttpDelete("DeleteIdentifier")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteIdentifier([FromQuery] string? idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return BadRequest("Параметр idCard обязателен");
            }

            return DataBaseStatusToWebStatusCode(IdentifierDBController.DeleteIdentifier(idCard));
        }

        /// <summary>
        /// Ошибка поиска сотрудника по tabnum в едином формате REST API v2.
        /// Используется методами версии 1.6.0 (ФИО и набор категорий), чтобы
        /// клиент получал машиночитаемый код ошибки, а не строку.
        /// </summary>
        private IActionResult PersonLookupError(TabnumLookupResult lookup)
        {
            return lookup.State switch
            {
                TabnumLookupState.Invalid => ApiErrorResults.Validation(lookup.ErrorMessage),
                TabnumLookupState.NotFound => ApiErrorResults.PersonNotFound(lookup.TabNum),
                TabnumLookupState.Ambiguous => ApiErrorResults.Create(
                    StatusCodes.Status409Conflict,
                    "AMBIGUOUS_TABNUM",
                    lookup.ErrorMessage),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Разбирает фильтр активности для списка сотрудников. Пустое значение
        /// означает «все записи», поэтому отсутствие параметра не меняет
        /// поведение прежнего метода.
        /// </summary>
        private static bool TryParseActiveFilter(string? value, out bool? active, out string error)
        {
            active = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return true;
            }

            switch (value.Trim().ToLowerInvariant())
            {
                case "all":
                case "any":
                    return true;

                case "1":
                case "true":
                case "yes":
                case "active":
                    active = true;
                    return true;

                case "0":
                case "false":
                case "no":
                case "inactive":
                    active = false;
                    return true;

                default:
                    error = "Параметр active должен быть одним из значений: all, active, inactive, 1, 0, true, false";
                    return false;
            }
        }

        private static PersonV2Get ToV2(PersonGet person)
        {
            return new PersonV2Get
            {
                TabNum = person.TabNum,
                Surname = person.Surname,
                Name = person.Name,
                Patronymic = person.Patronymic
            };
        }
    }
}
