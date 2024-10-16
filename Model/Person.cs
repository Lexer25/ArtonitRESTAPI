using OpenAPIArtonit.Anotation;

namespace OpenAPIArtonit.Model
{
    [DatabaseName("People")]
    abstract public class PersonBase
    {
        [DatabaseName("SURNAME")]
        public string Surname { get; set; }

        [DatabaseName("NAME")]
        public string Name { get; set; }

        [DatabaseName("PATRONYMIC")]
        public string Patronymic { get; set; }

        [DatabaseName("TABNUM")]
        public string TabNum { get; set; }
    }
    //get
    public class PersonGet : PersonBase 
    {
        [DatabaseName("ID_PEP")]
        public int? Id { get; set; }
    }
    //post
    public class PersonPostSee : PersonBase { }
    public class PersonPost : PersonPostSee
    {
        public PersonPost(PersonPostSee personPostSee)
        {
            this.Patronymic = personPostSee.Patronymic;
            this.Name = personPostSee.Name;
            this.Surname = personPostSee.Surname;
            this.TabNum = personPostSee.TabNum;
        }

        [DatabaseName("ID_PEP")]
        public int? Id { get; set; }
        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }
        [DatabaseName("ID_ORG")]
        public int? id_org { get; set; }
    }
    //pach
    public class PersonPach : PersonBase
    { 

        [DatabaseName("ID_PEP")]
        public int? Id { get; set; }
        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }
        [DatabaseName("ID_ORG")]
        public int? id_org { get; set; }
    }
}