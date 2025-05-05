using OpenAPIArtonit.Anotation;

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
        public string Name { get; set; }



    }
    [DatabaseName("ACCESSNAME")]
    public class AccessForInsert
    {
        [DatabaseName("NAME")]
        public string Name { get; set; }
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


}
