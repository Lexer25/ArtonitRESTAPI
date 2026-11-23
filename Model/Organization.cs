using System.Text.Json.Serialization;

namespace ArtonitRESTAPI.Model
{
    /// <summary>
    /// Организация в ответах REST API v2.
    /// </summary>
    public class OrganizationGet
    {
        /// <summary>
        /// Внутренний идентификатор записи (ID_ORG).
        /// </summary>
        [JsonPropertyName("id")]
        public int Id { get; set; }

        /// <summary>
        /// Уникальный код организации (DIVCODE).
        /// </summary>
        [JsonPropertyName("divcode")]
        public string DivCode { get; set; } = string.Empty;

        /// <summary>
        /// Название организации.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Код родительской организации; null — организация корневая.
        /// </summary>
        [JsonPropertyName("parent")]
        public string? Parent { get; set; }

        /// <summary>
        /// Флаг организации: 0 — обычная, 1 — гостевая.
        /// </summary>
        [JsonPropertyName("flag")]
        public int Flag { get; set; }

        /// <summary>
        /// Дата создания (ISO 8601, UTC). null — в базе нет отметки времени.
        /// </summary>
        [JsonPropertyName("createdAt")]
        public DateTime? CreatedAt { get; set; }

        /// <summary>
        /// Дата последнего изменения (ISO 8601, UTC). null — в базе нет отметки времени.
        /// </summary>
        [JsonPropertyName("updatedAt")]
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Постраничный список организаций.
    /// </summary>
    public class OrganizationListResponse
    {
        /// <summary>
        /// Организации текущей страницы.
        /// </summary>
        [JsonPropertyName("items")]
        public List<OrganizationGet> Items { get; set; } = new List<OrganizationGet>();

        /// <summary>
        /// Номер страницы, начиная с 1.
        /// </summary>
        [JsonPropertyName("page")]
        public int Page { get; set; }

        /// <summary>
        /// Размер страницы.
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }

        /// <summary>
        /// Общее число записей, удовлетворяющих фильтру.
        /// </summary>
        [JsonPropertyName("total")]
        public int Total { get; set; }
    }

    /// <summary>
    /// Тело запроса на создание организации (POST /api/v2/organizations).
    /// </summary>
    public class OrganizationPostSee
    {
        /// <summary>
        /// Уникальный код организации: 1–50 символов, латиница, цифры, «-», «_», «.».
        /// </summary>
        [JsonPropertyName("divcode")]
        public string? DivCode { get; set; }

        /// <summary>
        /// Название организации: 1–50 символов после обрезки пробелов.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Код родительской организации; null или отсутствие поля — корневая организация.
        /// </summary>
        [JsonPropertyName("parent")]
        public string? Parent { get; set; }

        /// <summary>
        /// Флаг организации: 0 — обычная, 1 — гостевая. По умолчанию 0.
        /// </summary>
        [JsonPropertyName("flag")]
        public int? Flag { get; set; }
    }

    /// <summary>
    /// Тело запроса на полную замену изменяемых полей организации (PUT /api/v2/organizations/{divcode}).
    /// </summary>
    public class OrganizationPutSee
    {
        /// <summary>
        /// Название организации: 1–50 символов после обрезки пробелов.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Код родительской организации; отсутствие поля или null — организация становится корневой.
        /// </summary>
        [JsonPropertyName("parent")]
        public string? Parent { get; set; }

        /// <summary>
        /// Флаг организации: 0 — обычная, 1 — гостевая. Отсутствие поля — 0.
        /// </summary>
        [JsonPropertyName("flag")]
        public int? Flag { get; set; }

        /// <summary>
        /// Код организации. Передавать не требуется; если передан, должен совпадать с путём.
        /// </summary>
        [JsonPropertyName("divcode")]
        public string? DivCode { get; set; }
    }

    /// <summary>
    /// Тело запроса на частичное изменение организации (PATCH /api/v2/organizations/{divcode}).
    /// Отсутствие поля означает «не изменять», значение null для parent — «переместить в корень».
    /// </summary>
    public class OrganizationPatchSee
    {
        /// <summary>
        /// Новое название организации. Не может быть пустым или null.
        /// </summary>
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        /// <summary>
        /// Новый код родительской организации; null — переместить в корень.
        /// </summary>
        [JsonPropertyName("parent")]
        public string? Parent { get; set; }

        /// <summary>
        /// Новый флаг организации: 0 или 1. Не может быть null.
        /// </summary>
        [JsonPropertyName("flag")]
        public int? Flag { get; set; }

        /// <summary>
        /// Код организации. Изменять нельзя.
        /// </summary>
        [JsonPropertyName("divcode")]
        public string? DivCode { get; set; }
    }

    /// <summary>
    /// Тело запроса на сохранение набора категорий доступа организации
    /// (PUT /api/v2/organizations/{divcode}/accessnames).
    /// </summary>
    public class OrganizationAccessNamesPutSee
    {
        /// <summary>
        /// Полный набор идентификаторов категорий доступа (ACCESSNAME.ID_ACCESSNAME).
        /// Пустой массив очищает набор; дубликаты игнорируются.
        /// </summary>
        [JsonPropertyName("accessnames")]
        public List<int>? AccessNames { get; set; }
    }

    /// <summary>
    /// Уточнение к ошибке валидации.
    /// </summary>
    public class ApiErrorDetail
    {
        /// <summary>
        /// Имя поля запроса.
        /// </summary>
        [JsonPropertyName("field")]
        public string Field { get; set; } = string.Empty;

        /// <summary>
        /// Машиночитаемая причина: required, invalid_format, too_long, invalid_value и т. п.
        /// </summary>
        [JsonPropertyName("issue")]
        public string Issue { get; set; } = string.Empty;
    }

    /// <summary>
    /// Единый формат ошибки REST API v2.
    /// </summary>
    public class ApiError
    {
        /// <summary>
        /// Машиночитаемый код в UPPER_SNAKE_CASE.
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Описание ошибки для человека на русском языке.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Необязательный массив уточнений по полям запроса.
        /// </summary>
        [JsonPropertyName("details")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<ApiErrorDetail>? Details { get; set; }
    }

    /// <summary>
    /// Ответ 409 при удалении организации, у которой есть дочерние организации или активные сотрудники.
    /// </summary>
    public class OrganizationDeleteConflict
    {
        /// <summary>
        /// Машиночитаемый код: HAS_CHILDREN, HAS_PEOPLE или HAS_CHILDREN_AND_PEOPLE.
        /// </summary>
        [JsonPropertyName("error")]
        public string Error { get; set; } = string.Empty;

        /// <summary>
        /// Описание причины отказа.
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Код организации, которую пытались удалить.
        /// </summary>
        [JsonPropertyName("divcode")]
        public string DivCode { get; set; } = string.Empty;

        /// <summary>
        /// Количество дочерних организаций.
        /// </summary>
        [JsonPropertyName("childrenCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? ChildrenCount { get; set; }

        /// <summary>
        /// Коды дочерних организаций.
        /// </summary>
        [JsonPropertyName("children")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? Children { get; set; }

        /// <summary>
        /// Количество активных сотрудников.
        /// </summary>
        [JsonPropertyName("peopleCount")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? PeopleCount { get; set; }

        /// <summary>
        /// Табельные номера активных сотрудников.
        /// </summary>
        [JsonPropertyName("people")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public List<string>? People { get; set; }
    }
}
