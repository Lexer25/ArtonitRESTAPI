using OpenAPIArtonit.Anotation;

namespace ArtonitRESTAPI.Model
{
   
        [DatabaseName("CARDTYPE")]
        abstract public class IdentifierTypeBase // базовый класс для идентификаторов
        {

            [DatabaseName("ID")]
            public int? Id { get; set; }


        }

        public class identifierTypeList : IdentifierTypeBase
        {

            [DatabaseName("NAME")]
            public string Name { get; set; }


            [DatabaseName("DESCRIPTION")]
            public string DESCRIPTION { get; set; }

        }
   
}
