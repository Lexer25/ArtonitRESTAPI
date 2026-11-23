using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// Формирует ответы об ошибках в едином формате REST API v2:
    /// <c>{ "error": ..., "message": ..., "details": [...] }</c>.
    /// </summary>
    public static class ApiErrorResults
    {
        /// <summary>Ошибка валидации полей запроса.</summary>
        public const string ValidationError = "VALIDATION_ERROR";

        /// <summary>Тело запроса не разбирается как JSON.</summary>
        public const string InvalidJsonError = "INVALID_JSON";

        /// <summary>Попытка изменить divcode.</summary>
        public const string DivCodeImmutableError = "DIVCODE_IMMUTABLE";

        /// <summary>PATCH без изменяемых полей.</summary>
        public const string EmptyPatchError = "EMPTY_PATCH";

        /// <summary>Токен отсутствует, просрочен или неверен.</summary>
        public const string UnauthorizedError = "UNAUTHORIZED";

        /// <summary>Нет прав администратора.</summary>
        public const string ForbiddenError = "FORBIDDEN";

        /// <summary>Организация не найдена.</summary>
        public const string NotFoundError = "NOT_FOUND";

        /// <summary>divcode уже существует.</summary>
        public const string DuplicateDivCodeError = "DUPLICATE_DIVCODE";

        /// <summary>Ссылка parent образует цикл.</summary>
        public const string CycleError = "CYCLE";

        /// <summary>У организации есть дочерние организации.</summary>
        public const string HasChildrenError = "HAS_CHILDREN";

        /// <summary>В организации есть активные сотрудники.</summary>
        public const string HasPeopleError = "HAS_PEOPLE";

        /// <summary>Есть и дочерние организации, и активные сотрудники.</summary>
        public const string HasChildrenAndPeopleError = "HAS_CHILDREN_AND_PEOPLE";

        /// <summary>Родительская организация не существует.</summary>
        public const string ParentNotFoundError = "PARENT_NOT_FOUND";

        /// <summary>Категория доступа не существует.</summary>
        public const string AccessNameNotFoundError = "ACCESSNAME_NOT_FOUND";

        /// <summary>В базе отсутствует таблица связей SS_ACCESSORG.</summary>
        public const string AccessOrgUnavailableError = "SS_ACCESSORG_UNAVAILABLE";

        /// <summary>Внутренняя ошибка сервера.</summary>
        public const string InternalError = "INTERNAL_ERROR";

        /// <summary>Запрет удаления корневой организации.</summary>
        public const string RootOrganizationError = "ROOT_ORGANIZATION";

        /// <summary>
        /// Создаёт ответ с произвольным кодом и телом ошибки.
        /// </summary>
        public static ObjectResult Create(int statusCode, string error, string message, List<ApiErrorDetail>? details = null)
        {
            var payload = new ApiError
            {
                Error = error,
                Message = message,
                Details = details is { Count: > 0 } ? details : null
            };

            return new ObjectResult(payload) { StatusCode = statusCode };
        }

        /// <summary>
        /// 400 с кодом VALIDATION_ERROR и одним уточнением.
        /// </summary>
        public static ObjectResult Validation(string field, string issue, string message)
        {
            return Create(
                StatusCodes.Status400BadRequest,
                ValidationError,
                message,
                new List<ApiErrorDetail> { new ApiErrorDetail { Field = field, Issue = issue } });
        }

        /// <summary>
        /// 400 с кодом VALIDATION_ERROR без уточнений.
        /// </summary>
        public static ObjectResult Validation(string message)
        {
            return Create(StatusCodes.Status400BadRequest, ValidationError, message);
        }

        /// <summary>
        /// 400 INVALID_JSON: тело запроса не разбирается как JSON.
        /// </summary>
        public static ObjectResult InvalidJson()
        {
            return Create(
                StatusCodes.Status400BadRequest,
                InvalidJsonError,
                "Тело запроса не является корректным JSON");
        }

        /// <summary>
        /// 400 INVALID_JSON: JSON должен быть объектом.
        /// </summary>
        public static ObjectResult JsonObjectExpected()
        {
            return Create(
                StatusCodes.Status400BadRequest,
                InvalidJsonError,
                "Тело запроса должно быть JSON-объектом");
        }

        /// <summary>
        /// Преобразует ошибки привязки модели в VALIDATION_ERROR с перечнем полей.
        /// </summary>
        public static ObjectResult FromModelState(ModelStateDictionary modelState)
        {
            var details = new List<ApiErrorDetail>();
            var fields = new List<string>();

            foreach (var entry in modelState)
            {
                if (entry.Value.Errors.Count == 0) continue;

                var field = NormalizeFieldName(entry.Key);
                details.Add(new ApiErrorDetail { Field = field, Issue = "invalid_value" });
                if (!fields.Contains(field)) fields.Add(field);
            }

            var message = fields.Count == 0
                ? "Некорректное тело запроса"
                : $"Некорректное значение {(fields.Count == 1 ? $"поля {fields[0]}" : $"полей {string.Join(", ", fields)}")}";

            return Create(StatusCodes.Status400BadRequest, ValidationError, message, details);
        }

        /// <summary>
        /// Приводит ключ ModelState к имени поля JSON (например, «$.flag» → «flag»).
        /// </summary>
        public static string NormalizeFieldName(string key)
        {
            if (string.IsNullOrEmpty(key)) return "body";

            var field = key;
            if (field.StartsWith("$.", StringComparison.Ordinal)) field = field.Substring(2);
            if (field.StartsWith("body.", StringComparison.Ordinal)) field = field.Substring(5);
            if (field == "body") return "body";
            if (field.Length == 0) return "body";

            return char.ToLowerInvariant(field[0]) + field.Substring(1);
        }

        /// <summary>
        /// 404 NOT_FOUND для организации.
        /// </summary>
        public static ObjectResult OrganizationNotFound(string divCode)
        {
            return Create(
                StatusCodes.Status404NotFound,
                NotFoundError,
                $"Организация с divcode={divCode} не найдена");
        }

        /// <summary>
        /// 404 NOT_FOUND для сотрудника.
        /// </summary>
        public static ObjectResult PersonNotFound(string tabnum)
        {
            return Create(
                StatusCodes.Status404NotFound,
                NotFoundError,
                $"Сотрудник с tabnum '{tabnum}' не найден");
        }

        /// <summary>
        /// 409 DUPLICATE_DIVCODE.
        /// </summary>
        public static ObjectResult DuplicateDivCode(string divCode)
        {
            return Create(
                StatusCodes.Status409Conflict,
                DuplicateDivCodeError,
                $"Организация с divcode={divCode} уже существует");
        }

        /// <summary>
        /// 400 DIVCODE_IMMUTABLE.
        /// </summary>
        public static ObjectResult DivCodeImmutable()
        {
            return Create(
                StatusCodes.Status400BadRequest,
                DivCodeImmutableError,
                "Код divcode нельзя изменить");
        }

        /// <summary>
        /// 400 EMPTY_PATCH.
        /// </summary>
        public static ObjectResult EmptyPatch()
        {
            return Create(
                StatusCodes.Status400BadRequest,
                EmptyPatchError,
                "PATCH не содержит ни одного изменяемого поля: ожидается name, parent или flag");
        }

        /// <summary>
        /// 409 CYCLE.
        /// </summary>
        public static ObjectResult Cycle()
        {
            return Create(
                StatusCodes.Status409Conflict,
                CycleError,
                "Нельзя указать родителем организацию-потомка");
        }

        /// <summary>
        /// 422 PARENT_NOT_FOUND.
        /// </summary>
        public static ObjectResult ParentNotFound(string divCode)
        {
            return Create(
                StatusCodes.Status422UnprocessableEntity,
                ParentNotFoundError,
                $"Родительская организация с divcode={divCode} не найдена");
        }

        /// <summary>
        /// 422 ACCESSNAME_NOT_FOUND: перечисленные категории доступа не существуют.
        /// </summary>
        public static ObjectResult AccessNamesNotFound(IReadOnlyCollection<int> idAccessNames)
        {
            const int maxListed = 20;

            var ids = idAccessNames.Take(maxListed).ToList();
            var listed = string.Join(", ", ids);

            var message = idAccessNames.Count == 0
                ? "Категория доступа не найдена"
                : idAccessNames.Count > maxListed
                    ? $"Категории доступа с id_accessname={listed} и ещё {idAccessNames.Count - maxListed} не найдены"
                    : $"Категории доступа с id_accessname={listed} не найдены";

            return Create(
                StatusCodes.Status422UnprocessableEntity,
                AccessNameNotFoundError,
                message,
                new List<ApiErrorDetail> { new ApiErrorDetail { Field = "accessnames", Issue = "not_found" } });
        }

        /// <summary>
        /// 409 SS_ACCESSORG_UNAVAILABLE: в базе нет таблицы связей организаций
        /// с категориями доступа, сохранять связи некуда.
        /// </summary>
        public static ObjectResult AccessOrgUnavailable()
        {
            return Create(
                StatusCodes.Status409Conflict,
                AccessOrgUnavailableError,
                "Сохранение невозможно: в базе отсутствует таблица SS_ACCESSORG");
        }

        /// <summary>
        /// 409 ROOT_ORGANIZATION: корневую организацию удалить нельзя.
        /// </summary>
        public static ObjectResult RootOrganizationDelete(string divCode)
        {
            return Create(
                StatusCodes.Status409Conflict,
                RootOrganizationError,
                $"Удаление невозможно: организация с divcode={divCode} является корневой");
        }

        /// <summary>
        /// 409 HAS_CHILDREN / HAS_PEOPLE / HAS_CHILDREN_AND_PEOPLE.
        /// </summary>
        public static ObjectResult DeleteConflict(
            string error,
            string message,
            string divCode,
            List<string>? children,
            List<string>? people)
        {
            var payload = new OrganizationDeleteConflict
            {
                Error = error,
                Message = message,
                DivCode = divCode,
                ChildrenCount = children?.Count,
                Children = children is { Count: > 0 } ? children : null,
                PeopleCount = people?.Count,
                People = people is { Count: > 0 } ? people : null
            };

            return new ObjectResult(payload) { StatusCode = StatusCodes.Status409Conflict };
        }

        /// <summary>
        /// 500 INTERNAL_ERROR.
        /// </summary>
        public static ObjectResult Internal()
        {
            return Create(
                StatusCodes.Status500InternalServerError,
                InternalError,
                "Внутренняя ошибка сервера");
        }

        /// <summary>
        /// 401 UNAUTHORIZED.
        /// </summary>
        public static ApiError UnauthorizedPayload()
        {
            return new ApiError { Error = UnauthorizedError, Message = "Требуется авторизация" };
        }

        /// <summary>
        /// 403 FORBIDDEN.
        /// </summary>
        public static ApiError ForbiddenPayload()
        {
            return new ApiError
            {
                Error = ForbiddenError,
                Message = "Недостаточно прав: требуется роль администратора"
            };
        }
    }
}
