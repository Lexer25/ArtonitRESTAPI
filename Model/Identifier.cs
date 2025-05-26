using NHibernate.Criterion;
using OpenAPIArtonit.Anotation;
using OpenAPIArtonit.Model;
using Swashbuckle.AspNetCore.Annotations;
using System.ComponentModel.DataAnnotations;
using System.Runtime;

namespace ArtonitRESTAPI.Model
{
  
        [DatabaseName("CARD")]
        abstract public class IdentifierBase // базовый класс для идентификаторов
        {

            [DatabaseName("ID_CARD")]
            public string Id_card { get; set; }
            
           
        }

        public class IdentifierList : IdentifierBase
        {
            
        [DatabaseName("ID_CARDTYPE")]
            public string Id_cardtype { get; set; }
                [DatabaseName("ID_PEP")]
                public int? Id_pep { get; set; }

        }
    //что мы вписываем в таблицу card при добавлении 
    /// <summary>
    /// Класс для представления идентификаторов поста.
    /// </summary>
    public class IdentifirePostSee : IdentifierBase
    {
        /// <summary>
        /// Идентификатор карты.
        /// Пример: строка // Идентификатор карты
        /// </summary>
        [DatabaseName("ID_CARD")]
        [SwaggerSchema(Description = "Идентификатор карты. Пример: строка // Идентификатор карты")]
        [Required(ErrorMessage = "Заполнение Id_card обязательно обязательно")]
        public string Id_card { get; set; }

        /// <summary>
        /// Идентификатор PEP.
        /// Пример: integer // Идентификатор PEP
        /// </summary>
        [DatabaseName("ID_PEP")]
        [SwaggerSchema(Description = "Идентификатор PEP. Пример: integer // Идентификатор PEP")]
        [Required(ErrorMessage = "Заполнение Id_pep обязательно обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "ID_PEP должен быть положительным числом")]
        public int Id_pep { get; set; }

        /// <summary>
        /// Идентификатор типа карты.
        /// Пример: integer // Идентификатор типа карты
        /// </summary>
        [DatabaseName("ID_CARDTYPE")]
        [SwaggerSchema(Description = "Идентификатор типа карты. Пример: integer // Идентификатор типа карты")]
        [Required(ErrorMessage = "Заполнение Id_cardtype обязательно обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "Id_cardtype должен быть положительным числом")]
        public int Id_cardtype { get; set; }
    }



    public class IdentifirePost : IdentifirePostSee
        {
            public IdentifirePost(IdentifirePostSee identifirePostSee)
            {
               this.Id_pep=identifirePostSee.Id_pep;
               this.Id_cardtype = identifirePostSee.Id_cardtype;
               this.Id_card = identifirePostSee.Id_card;
             }
            
        [DatabaseName("ID_DB")]
            public int? Id_db { get; set; }

            [DatabaseName("TIMESTART")]
            public DateTime Timestart { get; set; }

             [DatabaseName("STATUS")]
            public int? Status { get; set; }


             [DatabaseName("\"ACTIVE\"")]
            public int? Active { get; set; }





        }

    
}
