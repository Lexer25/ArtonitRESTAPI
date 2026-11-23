using OpenAPIArtonit.Anotation;
using Swashbuckle.AspNetCore.Annotations;
using System.Text.Json.Serialization;

namespace ArtonitRESTAPI.Model
{
    [DatabaseName("ACCESSNAME")]
    abstract public class AccessBase // базовый класс для категории доступа access
    {

        /// <summary>
        /// ID категории доступа
        /// </summary>
        [DatabaseName("ID_ACCESSNAME")]
        public int? Id_accessname { get; set; }

        /// <summary>
        /// Название категории доступа
        /// </summary>
        [DatabaseName("NAME")]
        [SwaggerSchema("Название категории доступа")]
        public string Name { get; set; } = string.Empty;



    }
    /// <summary>
    /// Метод POST для создания категории доступа
    /// </summary>
    [DatabaseName("ACCESSNAME")]
    public class AccessForInsert
    {
        /// <summary>
        /// Название категории доступа
        /// </summary>
        [DatabaseName("NAME")]
        [SwaggerSchema("Название категории доступа")]
        public string Name { get; set; } = string.Empty;
        public AccessForInsert() { }
        public AccessForInsert(AccessForInsert body)
        {
            this.Name = body.Name;
        }
    }


    public class AccessList : AccessBase
    {
        public AccessList() { }

        public AccessList(AccessList body)
        {
            this.Id_accessname = body.Id_accessname;
            this.Name = body.Name;
        }

    }

    /// <summary>
    /// Категория доступа в ответах на добавление и удаление.
    /// </summary>
    public class AccessCategory
    {
        /// <summary>
        /// Идентификатор категории доступа (ACCESSNAME.ID_ACCESSNAME).
        /// </summary>
        [JsonPropertyName("id_accessname")]
        public int Id_accessname { get; set; }

        /// <summary>
        /// Название категории доступа.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Сотрудник, которому назначена категория доступа. В ответе передаётся только tabnum.
    /// </summary>
    public class AccessPerson
    {
        /// <summary>
        /// Табельный номер сотрудника (PEOPLE.TABNUM).
        /// </summary>
        [DatabaseName("TABNUM")]
        [JsonPropertyName("tabnum")]
        public string TabNum { get; set; } = string.Empty;
    }

}
