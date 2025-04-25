using OpenAPIArtonit.Anotation;
using OpenAPIArtonit.Model;

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
        public class IdentifirePostSee : IdentifierBase
        {
            [DatabaseName("ID_PEP")]
            public int? Id_pep { get; set; }

             [DatabaseName("ID_CARDTYPE")]
            public int? Id_cardtype { get; set; }

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
            public string Timestart { get; set; }

             [DatabaseName("STATUS")]
            public int? Status { get; set; }


             [DatabaseName("\"ACTIVE\"")]
            public int? Active { get; set; }





        }
    
}
