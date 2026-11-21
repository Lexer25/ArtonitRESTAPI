using OpenAPIArtonit.Anotation;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace OpenAPIArtonit.Model
{
    /// <summary>
    /// Сотрудник в REST API v2. Вместо id_pep используется табельный номер tabnum.
    /// </summary>
    public class PersonV2Get
    {
        /// <summary>
        /// Табельный номер сотрудника.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        public string TabNum { get; set; } = string.Empty;

        /// <summary>
        /// Фамилия.
        /// </summary>
        [DatabaseName("SURNAME")]
        public string Surname { get; set; } = string.Empty;

        /// <summary>
        /// Имя.
        /// </summary>
        [DatabaseName("NAME")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Отчество.
        /// </summary>
        [DatabaseName("PATRONYMIC")]
        public string Patronymic { get; set; } = string.Empty;
    }

    /// <summary>
    /// Тело запроса на добавление сотрудника в REST API v2.
    /// </summary>
    public class PersonV2PostSee
    {
        /// <summary>
        /// Табельный номер. Необязателен: если не указан, формируется как tn_&lt;id_pep&gt;.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        [StringLength(50, ErrorMessage = "tabnum не должен превышать 50 символов")]
        public string? TabNum { get; set; }

        /// <summary>
        /// Фамилия.
        /// </summary>
        [DatabaseName("SURNAME")]
        [Required(ErrorMessage = "Заполнение Surname обязательно")]
        [StringLength(50, ErrorMessage = "Surname не должен превышать 50 символов")]
        public string Surname { get; set; } = string.Empty;

        /// <summary>
        /// Имя.
        /// </summary>
        [DatabaseName("NAME")]
        [StringLength(50, ErrorMessage = "Name не должен превышать 50 символов")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Отчество.
        /// </summary>
        [DatabaseName("PATRONYMIC")]
        [StringLength(50, ErrorMessage = "Patronymic не должен превышать 50 символов")]
        public string Patronymic { get; set; } = string.Empty;
    }

    /// <summary>
    /// Сотрудник для вставки в таблицу People.
    /// </summary>
    [DatabaseName("People")]
    public class PersonV2Post : PersonV2PostSee
    {
        /// <summary>
        /// Создаёт пустую модель для вставки.
        /// </summary>
        public PersonV2Post() { }

        /// <summary>
        /// Создаёт модель для вставки из тела запроса.
        /// </summary>
        public PersonV2Post(PersonV2PostSee body)
        {
            TabNum = body.TabNum;
            Surname = body.Surname;
            Name = body.Name;
            Patronymic = body.Patronymic;
        }

        /// <summary>
        /// Внутренний идентификатор. Назначается генератором и в API v2 не передаётся.
        /// </summary>
        [DatabaseName("ID_PEP")]
        [JsonIgnore]
        public int Id_pep { get; set; }

        /// <summary>
        /// Идентификатор базы данных.
        /// </summary>
        [DatabaseName("ID_DB")]
        [JsonIgnore]
        public int? id_db { get; set; }

        /// <summary>
        /// Идентификатор организации.
        /// </summary>
        [DatabaseName("ID_ORG")]
        [JsonIgnore]
        public int? id_org { get; set; }
    }

    /// <summary>
    /// Результат добавления сотрудника: назначенный табельный номер.
    /// </summary>
    public class PersonV2Created
    {
        /// <summary>
        /// Табельный номер созданного сотрудника.
        /// </summary>
        [JsonPropertyName("tabnum")]
        public string TabNum { get; set; } = string.Empty;
    }

    /// <summary>
    /// Тело запроса на изменение сотрудника в REST API v2.
    /// </summary>
    public class PersonV2Patch
    {
        /// <summary>
        /// Табельный номер сотрудника, которого нужно изменить.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        [Required(ErrorMessage = "Заполнение tabnum обязательно")]
        [StringLength(50, ErrorMessage = "tabnum не должен превышать 50 символов")]
        public string TabNum { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор базы данных.
        /// </summary>
        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }

        /// <summary>
        /// Идентификатор организации.
        /// </summary>
        [DatabaseName("ID_ORG")]
        public int? id_org { get; set; }
    }

    /// <summary>
    /// Тело запроса на добавление категории доступа сотруднику в REST API v2.
    /// </summary>
    public class PersonV2AddAccessSee
    {
        /// <summary>
        /// Табельный номер сотрудника.
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        [Required(ErrorMessage = "Заполнение tabnum обязательно")]
        [StringLength(50, ErrorMessage = "tabnum не должен превышать 50 символов")]
        public string TabNum { get; set; } = string.Empty;

        /// <summary>
        /// Идентификатор категории доступа.
        /// </summary>
        [DatabaseName("ID_ACCESSNAME")]
        public int? Id_accessname { get; set; }
    }
}
