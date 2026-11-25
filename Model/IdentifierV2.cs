using OpenAPIArtonit.Anotation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace ArtonitRESTAPI.Model
{
    /// <summary>
    /// Идентификатор в REST API v2: вместо id_pep возвращается tabnum.
    /// </summary>
    public class IdentifierV2List
    {
        /// <summary>
        /// Идентификатор карты.
        /// </summary>
        [DatabaseName("ID_CARD")]
        public string Id_card { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор типа карты.
        /// </summary>
        [DatabaseName("ID_CARDTYPE")]
        public string Id_cardtype { get; set; } = string.Empty;

        /// <summary>
        /// Табельный номер сотрудника, которому принадлежит идентификатор.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        public string TabNum { get; set; } = string.Empty;

        /// <summary>
        /// Дата и время начала действия идентификатора (CARD.TIMESTART).
        /// </summary>
        [DatabaseName("TIMESTART")]
        public DateTime Timestart { get; set; }

        /// <summary>
        /// Дата и время окончания действия идентификатора (CARD.TIMEEND).
        /// null — срок действия не ограничен.
        /// </summary>
        [DatabaseName("TIMEEND")]
        public DateTime? Timeend { get; set; }

        /// <summary>
        /// Признак активности идентификатора: 1 — активен, 0 — неактивен.
        /// </summary>
        [DatabaseName("ACTIVE")]
        public int? Active { get; set; }

        /// <summary>
        /// Дата и время создания записи (CARD.CREATEDAT).
        /// </summary>
        [DatabaseName("CREATEDAT")]
        public DateTime? Createdat { get; set; }
    }

    /// <summary>
    /// Статистика по таблице CARD после применения фильтров: всего записей,
    /// активных (ACTIVE = 1) и неактивных (ACTIVE = 0).
    /// </summary>
    public class IdentifierV2Count
    {
        /// <summary>
        /// Количество записей в таблице CARD после применения фильтров.
        /// </summary>
        [DatabaseName("TOTAL_COUNT")]
        [JsonPropertyName("total")]
        public int Total { get; set; }

        /// <summary>
        /// Количество записей с ACTIVE = 1 после применения фильтров.
        /// </summary>
        [DatabaseName("ACTIVE_COUNT")]
        [JsonPropertyName("active")]
        public int Active { get; set; }

        /// <summary>
        /// Количество записей с ACTIVE = 0 после применения фильтров.
        /// </summary>
        [DatabaseName("INACTIVE_COUNT")]
        [JsonPropertyName("inactive")]
        public int Inactive { get; set; }
    }

    /// <summary>
    /// Значение фильтра по колонке-дате. Если передана только дата без времени,
    /// фильтр охватывает весь день; иначе сравнивается точное значение.
    /// </summary>
    public sealed class IdentifierV2DateFilter
    {
        /// <summary>
        /// Разобранное значение даты или даты-времени.
        /// </summary>
        public DateTime Value { get; init; }

        /// <summary>
        /// true — передана только дата, сравнивать нужно весь день.
        /// </summary>
        public bool DateOnly { get; init; }
    }

    /// <summary>
    /// Разобранные фильтры методов IdentifierV2. Все поля необязательны:
    /// null означает, что фильтр не задан.
    /// </summary>
    public sealed class IdentifierV2Filter
    {
        /// <summary>
        /// Номер карты (параметр identifire для совместимости со старым GetList).
        /// </summary>
        public string? IdCard { get; init; }

        /// <summary>
        /// Табельный номер сотрудника (CARD.ID_PEP через PEOPLE.TABNUM).
        /// </summary>
        public string? TabNum { get; init; }

        /// <summary>
        /// Тип идентификатора (CARD.ID_CARDTYPE).
        /// </summary>
        public int? IdCardtype { get; init; }

        /// <summary>
        /// Признак активности (CARD.ACTIVE): true — 1, false — 0.
        /// </summary>
        public bool? Active { get; init; }

        /// <summary>
        /// Фильтр по CARD.TIMESTART.
        /// </summary>
        public IdentifierV2DateFilter? TimeStart { get; init; }

        /// <summary>
        /// Фильтр по CARD.TIMEEND.
        /// </summary>
        public IdentifierV2DateFilter? TimeEnd { get; init; }

        /// <summary>
        /// Фильтр по CARD.CREATEDAT.
        /// </summary>
        public IdentifierV2DateFilter? CreatedAt { get; init; }
    }

    /// <summary>
    /// Тело запроса на добавление идентификатора сотруднику в REST API v2.
    /// </summary>
    public class IdentifierV2PostSee
    {
        /// <summary>
        /// Идентификатор карты.
        /// </summary>
        [DatabaseName("ID_CARD")]
        [Required(ErrorMessage = "Заполнение Id_card обязательно")]
        public string Id_card { get; set; } = string.Empty;

        /// <summary>
        /// Табельный номер сотрудника, которому принадлежит карта.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        [Required(ErrorMessage = "Заполнение tabnum обязательно")]
        [StringLength(50, ErrorMessage = "tabnum не должен превышать 50 символов")]
        public string TabNum { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор типа карты.
        /// </summary>
        [DatabaseName("ID_CARDTYPE")]
        [Required(ErrorMessage = "Заполнение Id_cardtype обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Id_cardtype должен быть положительным числом")]
        public int Id_cardtype { get; set; }
    }
}
