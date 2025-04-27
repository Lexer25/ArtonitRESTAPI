using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.Legasy_Service;
using OpenAPIArtonit.Model;
using System.Security.Claims;

namespace OpenAPIArtonit.DBControllers
{
    public class PersonDBController 
    {
        public static DatabaseResult GetById(int id)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var idPep = userIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            var query = $@"select ID_PEP, ID_DB, p.ID_ORG, SURNAME, p.NAME, PATRONYMIC,
                DATEBIRTH, PLACELIFE, PLACEREG, PHONEHOME, PHONECELLULAR, PHONEWORK,
                NUMDOC, DATEDOC, PLACEDOC, PHOTO, WORKSTART, WORKEND, ""ACTIVE"" , p.FLAG,
                LOGIN, PSWD, PEPTYPE, POST, PLACEBIRTH, NOTE, ID_AREA, TABNUM
                from people p 
                join organization_getchild (1, (select p2. id_orgctrl from people p2 where p2.id_pep={1})) og on p.id_org=og.id_org
                where p.ID_PEP = {id}";
            return DatabaseService.Get<PersonGet>(query);
        }
        public static Tuple<DatabaseResult, int>  GetAll(int pageIndex, int pageSize)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var idPep = userIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var query_list = $@"select
                first {pageSize} skip {pageSize * pageIndex}
                ID_PEP, ID_DB, ID_ORG, SURNAME, NAME, PATRONYMIC,
                DATEBIRTH, PLACELIFE, PLACEREG, PHONEHOME, PHONECELLULAR, PHONEWORK,
                NUMDOC, DATEDOC, PLACEDOC, PHOTO, WORKSTART, WORKEND, ""ACTIVE"" , FLAG,
                LOGIN, PSWD, PEPTYPE, POST, PLACEBIRTH, NOTE, ID_AREA, TABNUM";
            var query_count = $@"select count(*)";
            var base_query = $@"
                from people p
                where p.id_org in (select id_org from 
                organization_getchild (1, (select p2.id_orgctrl from people p2 where p2.id_pep={1})))
                ";
            return new Tuple<DatabaseResult, int>
                (DatabaseService.GetList<PersonGet>(query_list + base_query), 
                ((COUNTDataBase)DatabaseService.Get<COUNTDataBase>(query_count + base_query).Value).count);
        }

        //добавить пипла
        //при этом id_pep игнорируется.
        public static DatabaseResult Add(PersonPost peopleAdd)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var idOrgCtrl = userIdentity?.FindFirst(MyClaimTypes.IdOrgCtrl)?.Value;

            var rdbDatabase = DatabaseService.Get<RBDataBase>("select GEN_ID (gen_people_id, 1) from RDB$DATABASE");//получаю новое значение id_pep 
            
            if (rdbDatabase.State != State.Successes) return rdbDatabase;
            peopleAdd.id_org = 1;
            peopleAdd.id_db = 1;
            peopleAdd.Id_pep = ((RBDataBase)rdbDatabase.Value).Id;
            Console.WriteLine("60 " + ((RBDataBase)rdbDatabase.Value).Id);
            Console.WriteLine("61 " + peopleAdd.Id_pep);
            if (rdbDatabase.State == State.Successes)
            {
                var result = DatabaseService.Create(peopleAdd);
                if (result.State == State.Successes) result.Value = rdbDatabase.Value;
                return result;
            }
            Console.WriteLine("68 " + rdbDatabase);
            return rdbDatabase;
        }

         //добавить пипла в таблицу ss_accessuser с указанием необходимой категории ему доступа
        public static DatabaseResult AddAccess(PersonAddAccess peopleAdd)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var idOrgCtrl = userIdentity?.FindFirst(MyClaimTypes.IdOrgCtrl)?.Value;

            var rdbDatabase = DatabaseService.Get<RBDataBase>("select GEN_ID (GEN_SS_ACCESSUSER_ID, 1) from RDB$DATABASE");
           
            if (rdbDatabase.State != State.Successes) return rdbDatabase;

            //peopleAdd.Id_pep = ((RBDataBase)rdbDatabase.Value).Id;

            Console.WriteLine("555"); 
            return DatabaseService.Create(peopleAdd);

            if (rdbDatabase.State == State.Successes)
            {
                // var result = DatabaseService.Create(peopleAdd);
                var result = DatabaseService.ExecuteNonQuery("INSERT INTO SS_ACCESSUSER (ID_ACCESSUSER,ID_DB,ID_PEP,ID_ACCESSNAME,USERNAME) VALUES (38,1,14020,4,'SYSDBA')");
                if (result.State == State.Successes) result.Value = rdbDatabase.Value;
                return result;
            }
            return rdbDatabase;
        }




        public static DatabaseResult Update(PersonPach peopleAdd)
        {
            return DatabaseService.Update(peopleAdd, $"ID_PEP={peopleAdd.Id_pep}");
        }
        public static DatabaseResult Delete(int id)
        {
            var query = $"delete from people where ID_PEP={id}";
            return DatabaseService.ExecuteNonQuery(query);
        }
    }
}
