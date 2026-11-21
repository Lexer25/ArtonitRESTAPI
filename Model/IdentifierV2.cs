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
