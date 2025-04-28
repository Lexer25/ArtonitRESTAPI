using OpenAPIArtonit.Anotation;

namespace ArtonitRESTAPI.Model
{
    [DatabaseName("ACESSNAME")]
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
        public string Name { get; set; }



    }


    public class AccessList : AccessBase
    {
       

    }


}
