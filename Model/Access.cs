using OpenAPIArtonit.Anotation;

namespace ArtonitRESTAPI.Model
{
    [DatabaseName("ACESSNAME")]
    abstract public class AccessBase // базовый класс для категории доступа access
    {

        [DatabaseName("ID_ACCESSNAME")]
        public int? Id_accessname { get; set; }

        [DatabaseName("NAME")]
        public string Name { get; set; }



    }


    public class AccessList : AccessBase
    {
       

    }


}
