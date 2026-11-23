using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для организаций: иерархический справочник подразделений.
    /// Все команды требуют заголовок Authorization: Bearer &lt;token&gt;,
    /// изменяющие команды дополнительно требуют роль администратора.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/organizations")]
    [Authorize]
    [Produces("application/json")]
    public class OrganizationV2Controller : V2ControllerBase
    {
        /// <summary>
        /// Имя политики авторизации для изменяющих команд.
        /// </summary>
        public const string AdminPolicy = BaseAPIController.AdminPolicy;

        private const int MaxCodeLength = 50;
        private const int MaxNameLength = 50;
        private const int DefaultPageSize = 20;
        private const int MaxPageSize = 100;

        /// <summary>
        /// Максимальное число категорий доступа в одном запросе сохранения набора.
        /// </summary>
        private const int MaxAccessNamesCount = 1000;

        private static readonly Regex DivCodeRegex = new Regex(
            @"^[A-Za-z0-9._-]+$",
            RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Получить список организаций с фильтрацией и пагинацией.
        /// </summary>
        /// <param name="parent">Код родителя; пустое значение — только корневые организации.</param>
        /// <param name="name">Поиск по подстроке в названии без учёта регистра.</param>
        /// <param name="page">Номер страницы, начиная с 1. По умолчанию 1.</param>
        /// <param name="pageSize">Размер страницы от 1 до 100. По умолчанию 20.</param>
        /// <param name="sort">Поле и направление: name, -name, divcode, -divcode, createdAt, -createdAt.</param>
        [HttpGet]
        [ProducesResponseType(typeof(OrganizationListResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(
            [FromQuery(Name = "parent")] string? parent,
            [FromQuery(Name = "name")] string? name,
            [FromQuery(Name = "page")] int? page,
            [FromQuery(Name = "pageSize")] int? pageSize,
            [FromQuery(Name = "sort")] string? sort)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            var pageNumber = page ?? 1;
            if (pageNumber < 1)
            {
                return ApiErrorResults.Validation(
                    "page", "invalid_value", "Параметр page должен быть целым числом не меньше 1");
            }

            var size = pageSize ?? DefaultPageSize;
            if (size < 1 || size > MaxPageSize)
            {
                return ApiErrorResults.Validation(
                    "pageSize", "invalid_value", $"Параметр pageSize должен быть целым числом от 1 до {MaxPageSize}");
            }

            if (!TryParseSort(sort, out var sortField, out var descending))
            {
                return ApiErrorResults.Validation(
                    "sort", "invalid_value",
                    "Параметр sort должен быть одним из значений: name, -name, divcode, -divcode, createdAt, -createdAt");
            }

            var parentSpecified = Request.Query.ContainsKey("parent");

            var result = OrganizationDBController.GetList(
                parentSpecified, parent, name, pageNumber, size, sortField, descending);

            if (result.Status != OrganizationOperationStatus.Success)
            {
                return ApiErrorResults.Internal();
            }

            return Ok(new OrganizationListResponse
            {
                Items = result.Items.Select(record => record.ToApi()).ToList(),
                Page = pageNumber,
                PageSize = size,
                Total = result.Total
            });
        }

        /// <summary>
        /// Получить организацию по коду divcode.
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        [HttpGet("{divcode}")]
        [ProducesResponseType(typeof(OrganizationGet), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult Get([FromRoute] string divcode)
        {
            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            var lookup = OrganizationDBController.Find(code);

            return lookup.Status switch
            {
                OrganizationOperationStatus.Success when lookup.Record != null => Ok(lookup.Record.ToApi()),
                OrganizationOperationStatus.NotFound => ApiErrorResults.OrganizationNotFound(code),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Получить список идентификаторов категорий доступа, привязанных к организации.
        /// Источник — таблица связей SS_ACCESSORG.
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        [HttpGet("{divcode}/accessnames")]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetAccessNames([FromRoute] string divcode)
        {
            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            var result = OrganizationDBController.GetAccessNames(code);

            return result.Status switch
            {
                OrganizationOperationStatus.Success => Ok(result.Items),
                OrganizationOperationStatus.NotFound => ApiErrorResults.OrganizationNotFound(code),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Сохранить набор категорий доступа организации: полная замена связей в SS_ACCESSORG.
        /// Недостающие связи добавляются, лишние удаляются; пустой массив очищает набор.
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        /// <param name="body">Полный набор идентификаторов категорий доступа: поле accessnames.</param>
        [HttpPut("{divcode}/accessnames")]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult SaveAccessNames(
            [FromRoute] string divcode,
            [FromBody] OrganizationAccessNamesPutSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
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

            var result = OrganizationDBController.SaveAccessNames(code, body.AccessNames);

            return result.Status switch
            {
                OrganizationOperationStatus.Success => Ok(result.Items),
                OrganizationOperationStatus.NotFound => ApiErrorResults.OrganizationNotFound(code),
                OrganizationOperationStatus.AccessNameNotFound => ApiErrorResults.AccessNamesNotFound(
                    result.MissingAccessNames),
                OrganizationOperationStatus.AccessOrgUnavailable => ApiErrorResults.AccessOrgUnavailable(),
                _ => ApiErrorResults.Internal()
            };
        }

        /// <summary>
        /// Добавить организацию.
        /// </summary>
        /// <param name="body">Поля создаваемой организации: divcode, name, parent, flag.</param>
        [HttpPost]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(OrganizationGet), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult Create([FromBody] OrganizationPostSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var divCodeError = ValidateDivCode(body.DivCode, out var divCode);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            var nameError = ValidateName(body.Name, out var name);
            if (nameError != null)
            {
                return nameError;
            }

            var flag = body.Flag ?? 0;
            if (flag != 0 && flag != 1)
            {
                return ApiErrorResults.Validation(
                    "flag", "invalid_value", "Поле flag может принимать значения 0 (обычная) или 1 (гостевая)");
            }

            var result = OrganizationDBController.Create(divCode, name, NormalizeParent(body.Parent), flag);

            switch (result.Status)
            {
                case OrganizationOperationStatus.Success when result.Record != null:
                    var created = result.Record.ToApi();
                    Response.Headers.Location = $"/api/v2/organizations/{Uri.EscapeDataString(created.DivCode)}";
                    return StatusCode(StatusCodes.Status201Created, created);

                case OrganizationOperationStatus.DuplicateDivCode:
                    return ApiErrorResults.DuplicateDivCode(divCode);

                case OrganizationOperationStatus.ParentNotFound:
                    return ApiErrorResults.ParentNotFound(result.ParentDivCode ?? body.Parent ?? string.Empty);

                default:
                    return ApiErrorResults.Internal();
            }
        }

        /// <summary>
        /// Полностью заменить изменяемые поля организации (name, parent, flag).
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        /// <param name="body">Новые значения полей: name обязателен; отсутствие parent — корень; отсутствие flag — 0.</param>
        [HttpPut("{divcode}")]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(OrganizationGet), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult Replace([FromRoute] string divcode, [FromBody] OrganizationPutSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            if (body.DivCode != null
                && !string.Equals(body.DivCode.Trim(), code, StringComparison.OrdinalIgnoreCase))
            {
                return ApiErrorResults.DivCodeImmutable();
            }

            var nameError = ValidateName(body.Name, out var name);
            if (nameError != null)
            {
                return nameError;
            }

            var flag = body.Flag ?? 0;
            if (flag != 0 && flag != 1)
            {
                return ApiErrorResults.Validation(
                    "flag", "invalid_value", "Поле flag может принимать значения 0 (обычная) или 1 (гостевая)");
            }

            var lookup = OrganizationDBController.Find(code);
            if (lookup.Status == OrganizationOperationStatus.DatabaseError)
            {
                return ApiErrorResults.Internal();
            }

            if (lookup.Status != OrganizationOperationStatus.Success || lookup.Record == null)
            {
                return ApiErrorResults.OrganizationNotFound(code);
            }

            var result = OrganizationDBController.Update(lookup.Record.IdOrg, name, NormalizeParent(body.Parent), flag);

            return ToMutationResponse(result, code, body.Parent);
        }

        /// <summary>
        /// Изменить только переданные поля организации.
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        /// <param name="body">Поля для изменения: name, parent, flag. Хотя бы одно поле обязательно.</param>
        [HttpPatch("{divcode}")]
        [Authorize(Policy = AdminPolicy)]
        [JsonBody]
        [ProducesResponseType(typeof(OrganizationGet), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status422UnprocessableEntity)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult Update([FromRoute] string divcode, [FromBody] OrganizationPatchSee? body)
        {
            if (!ModelState.IsValid)
            {
                return ApiErrorResults.FromModelState(ModelState);
            }

            if (body == null)
            {
                return ApiErrorResults.Validation("Тело запроса обязательно");
            }

            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            var fields = HttpContext.Items[JsonBodyAttribute.FieldsItemKey] as HashSet<string>
                         ?? new HashSet<string>(StringComparer.Ordinal);

            var hasName = fields.Contains("name");
            var hasParent = fields.Contains("parent");
            var hasFlag = fields.Contains("flag");

            if (fields.Contains("divcode")
                && !string.Equals((body.DivCode ?? string.Empty).Trim(), code, StringComparison.OrdinalIgnoreCase))
            {
                return ApiErrorResults.DivCodeImmutable();
            }

            if (!hasName && !hasParent && !hasFlag)
            {
                return ApiErrorResults.EmptyPatch();
            }

            string? newName = null;
            if (hasName)
            {
                if (body.Name == null)
                {
                    return ApiErrorResults.Validation("name", "null_not_allowed", "Поле name не может быть null");
                }

                var nameError = ValidateName(body.Name, out newName);
                if (nameError != null)
                {
                    return nameError;
                }
            }

            int? newFlag = null;
            if (hasFlag)
            {
                if (body.Flag == null)
                {
                    return ApiErrorResults.Validation("flag", "null_not_allowed", "Поле flag не может быть null");
                }

                if (body.Flag != 0 && body.Flag != 1)
                {
                    return ApiErrorResults.Validation(
                        "flag", "invalid_value", "Поле flag может принимать значения 0 (обычная) или 1 (гостевая)");
                }

                newFlag = body.Flag;
            }

            var lookup = OrganizationDBController.Find(code);
            if (lookup.Status == OrganizationOperationStatus.DatabaseError)
            {
                return ApiErrorResults.Internal();
            }

            if (lookup.Status != OrganizationOperationStatus.Success || lookup.Record == null)
            {
                return ApiErrorResults.OrganizationNotFound(code);
            }

            var current = lookup.Record;
            var name = hasName ? newName! : current.Name;
            var flag = newFlag ?? current.Flag;
            var parent = hasParent ? NormalizeParent(body.Parent) : current.ParentDivCode;

            var result = OrganizationDBController.Update(current.IdOrg, name, parent, flag);

            return ToMutationResponse(result, code, parent);
        }

        /// <summary>
        /// Удалить организацию, если у неё нет дочерних организаций и активных сотрудников.
        /// </summary>
        /// <param name="divcode">Код организации.</param>
        [HttpDelete("{divcode}")]
        [Authorize(Policy = AdminPolicy)]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status403Forbidden)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(OrganizationDeleteConflict), StatusCodes.Status409Conflict)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult Delete([FromRoute] string divcode)
        {
            var divCodeError = ValidateDivCode(divcode, out var code);
            if (divCodeError != null)
            {
                return divCodeError;
            }

            var result = OrganizationDBController.Delete(code);

            return result.Status switch
            {
                OrganizationOperationStatus.Success => NoContent(),
                OrganizationOperationStatus.NotFound => ApiErrorResults.OrganizationNotFound(code),
                OrganizationOperationStatus.RootOrganization => ApiErrorResults.RootOrganizationDelete(result.DivCode),
                OrganizationOperationStatus.HasChildren => ApiErrorResults.DeleteConflict(
                    ApiErrorResults.HasChildrenError,
                    "Удаление невозможно: у организации есть дочерние организации",
                    result.DivCode,
                    result.Children,
                    null),
                OrganizationOperationStatus.HasPeople => ApiErrorResults.DeleteConflict(
                    ApiErrorResults.HasPeopleError,
                    "Удаление невозможно: в организации есть сотрудники",
                    result.DivCode,
                    null,
                    result.People),
                OrganizationOperationStatus.HasChildrenAndPeople => ApiErrorResults.DeleteConflict(
                    ApiErrorResults.HasChildrenAndPeopleError,
                    "Удаление невозможно: у организации есть дочерние организации и сотрудники",
                    result.DivCode,
                    result.Children,
                    result.People),
                _ => ApiErrorResults.Internal()
            };
        }

        private IActionResult ToMutationResponse(
            OrganizationMutationResult result,
            string divCode,
            string? parentDivCode)
        {
            return result.Status switch
            {
                OrganizationOperationStatus.Success when result.Record != null => Ok(result.Record.ToApi()),
                OrganizationOperationStatus.NotFound => ApiErrorResults.OrganizationNotFound(divCode),
                OrganizationOperationStatus.DuplicateDivCode => ApiErrorResults.DuplicateDivCode(divCode),
                OrganizationOperationStatus.ParentNotFound => ApiErrorResults.ParentNotFound(
                    result.ParentDivCode ?? parentDivCode ?? string.Empty),
                OrganizationOperationStatus.Cycle => ApiErrorResults.Cycle(),
                _ => ApiErrorResults.Internal()
            };
        }

        private static IActionResult? ValidateDivCode(string? raw, out string divCode)
        {
            divCode = (raw ?? string.Empty).Trim();

            if (divCode.Length == 0)
            {
                return ApiErrorResults.Validation("divcode", "required", "Поле divcode обязательно");
            }

            if (divCode.Length > MaxCodeLength)
            {
                return ApiErrorResults.Validation(
                    "divcode", "too_long", $"Поле divcode не должно превышать {MaxCodeLength} символов");
            }

            if (!DivCodeRegex.IsMatch(divCode))
            {
                return ApiErrorResults.Validation(
                    "divcode", "invalid_format",
                    "Поле divcode может содержать только латинские буквы, цифры, «-», «_» и «.»");
            }

            return null;
        }

        private static IActionResult? ValidateName(string? raw, out string name)
        {
            name = (raw ?? string.Empty).Trim();

            if (name.Length == 0)
            {
                return ApiErrorResults.Validation("name", "required", "Поле name обязательно");
            }

            if (name.Length > MaxNameLength)
            {
                return ApiErrorResults.Validation(
                    "name", "too_long", $"Поле name не должно превышать {MaxNameLength} символов");
            }

            return null;
        }

        private static string? NormalizeParent(string? parent)
        {
            return string.IsNullOrWhiteSpace(parent) ? null : parent.Trim();
        }

        private static bool TryParseSort(string? sort, out OrganizationSortField field, out bool descending)
        {
            field = OrganizationSortField.Name;
            descending = false;

            if (string.IsNullOrWhiteSpace(sort))
            {
                return true;
            }

            var value = sort.Trim();

            if (value.StartsWith("-", StringComparison.Ordinal))
            {
                descending = true;
                value = value.Substring(1);
            }

            if (value.Equals("name", StringComparison.OrdinalIgnoreCase))
            {
                field = OrganizationSortField.Name;
                return true;
            }

            if (value.Equals("divcode", StringComparison.OrdinalIgnoreCase))
            {
                field = OrganizationSortField.DivCode;
                return true;
            }

            if (value.Equals("createdAt", StringComparison.OrdinalIgnoreCase))
            {
                field = OrganizationSortField.CreatedAt;
                return true;
            }

            return false;
        }
    }
}
