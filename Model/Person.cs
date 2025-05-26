using OpenAPIArtonit.Anotation;
using System.ComponentModel.DataAnnotations;

namespace OpenAPIArtonit.Model
{


      
    [DatabaseName("People")]
    public class PersonBase
    {
        public PersonBase() { }

        [DatabaseName("ID_PEP")]
        [Required(ErrorMessage = "Заполнение ID обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "ID должен быть положительным числом")]
        public int Id_pep { get; set; }
       
    }

    //get
    //модель для добавленя person
    [DatabaseName("People")]
    public class PersonGet : PersonBase 
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
    
    
    
    //post
    public class PersonPostSee : PersonGet { }

   
    public class PersonPost : PersonPostSee
    {
        public PersonPost(PersonPostSee personPostSee)
        {
            this.Patronymic = personPostSee.Patronymic;
            this.Name = personPostSee.Name;
            this.Surname = personPostSee.Surname;
            this.TabNum = personPostSee.TabNum;
        }

        //[DatabaseName("ID_PEP")]
        //public int? Id { get; set; }

        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }
        [DatabaseName("ID_ORG")]
        public int? id_org { get; set; }
    }




    public class PersonAddAccessSee : PersonBase
    {

        [DatabaseName("ID_ACCESSNAME")]
        public int? Id_accessname { get; set; }

    }



    [DatabaseName("ss_accessuser")]

    public class PersonAddAccess : PersonAddAccessSee
    {
        public PersonAddAccess(PersonAddAccessSee peopleBase)
        {
            this.Id_pep= peopleBase.Id_pep;
            this.Id_accessname = peopleBase.Id_accessname;
        }
        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }

        [DatabaseName("USERNAME")]
        public string Username { get; set; }




    }
    [DatabaseName("CARD")]
    public class ForAddIdentifier
    {


        [DatabaseName("ID_PEP")]
        [Required(ErrorMessage = "Заполнение ID_PEP обязательно")]
        [Range(1, int.MaxValue, ErrorMessage = "ID_PEP должен быть положительным числом")]
        public string Id_pep { get; set; }

        [DatabaseName("ID_CARD")]
        [Required(ErrorMessage = "ID_CARD обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "ID_CARD должен быть положительным числом")]
        public int id_card { get; set; }

        [DatabaseName("ID_CARDTYPE")]
        [Required(ErrorMessage = "ID_CARDTYPE обязателен")]
        [Range(1, int.MaxValue, ErrorMessage = "ID_CARDTYPE должен быть положительным числом")]
        public int id_cardtype { get; set; }

        public ForAddIdentifier(){}
        public ForAddIdentifier(ForAddIdentifier body)
        {
            this.Id_pep = body.Id_pep;
            this.id_card = body.id_card;
            this.id_cardtype = body.id_cardtype;
        }
    }



    //pach
    public class PersonPach : PersonBase
    { 

        //[DatabaseName("ID_PEP")]
        //public int? Id { get; set; }
        
        [DatabaseName("ID_DB")]
        public int? id_db { get; set; }
        [DatabaseName("ID_ORG")]
        public int? id_org { get; set; }
    }
}