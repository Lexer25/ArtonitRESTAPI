using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.Legasy_Service;
using OpenAPIArtonit.Model;
using System.Security.Claims;
using System.Collections.Generic;

namespace OpenAPIArtonit.DBControllers
{
    public class PersonDBController
    {
        public static Tuple<DatabaseResult, int> GetById(int id, ClaimsPrincipal user)
        {
            var idPep = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value;

            if (string.IsNullOrEmpty(idPep))
            {
                idPep = "1";
            }

            var query_list = $@"
select
    ID_PEP, ID_DB, p.ID_ORG, SURNAME, p.NAME, PATRONYMIC,
    DATEBIRTH, PLACELIFE, PLACEREG, PHONEHOME, PHONECELLULAR, PHONEWORK,
    NUMDOC, DATEDOC, PLACEDOC, PHOTO, WORKSTART, WORKEND, ""ACTIVE"", p.FLAG,
    LOGIN, PSWD, PEPTYPE, POST, PLACEBIRTH, NOTE, ID_AREA, TABNUM";

            var query_count = "select count(*)";

            var base_query = $@"
from people p
join organization_getchild (1, (select p2.id_orgctrl from people p2 where p2.id_pep = {idPep})) og 
    on p.id_org = og.id_org
where p.ID_PEP = {id}";

            Console.WriteLine("QueryList: " + query_list + base_query);
            Console.WriteLine("QueryCount: " + query_count + base_query);

            var countResult = DatabaseService.Get<COUNTDataBase>(query_count + base_query);
            var count = countResult?.Value is COUNTDataBase c ? c.count : 0;

            var result = DatabaseService.GetList<PersonGet>(query_list + base_query);
            if (result.State == State.Successes && result.Value is List<PersonGet> list && list.Count == 0)
            {
                result.State = State.NotFound;
                result.ErrorMessage = $"Пользователь с ID_PEP {id} не найден";
                result.Value = null;
            }

            return new Tuple<DatabaseResult, int>(result, count);
        }

        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize)
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

        public static DatabaseResult Add(PersonPost peopleAdd)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var idOrgCtrl = userIdentity?.FindFirst(MyClaimTypes.IdOrgCtrl)?.Value;

            var rdbDatabase = DatabaseService.Get<RBDataBase>("select GEN_ID (gen_people_id, 1) from RDB$DATABASE");

            if (rdbDatabase.State != State.Successes) return rdbDatabase;
            peopleAdd.id_org = 1;
            peopleAdd.id_db = 1;
            peopleAdd.Id_pep = ((RBDataBase)rdbDatabase.Value).Id;
            Console.WriteLine("60 " + ((RBDataBase)rdbDatabase.Value).Id);
            Console.WriteLine("61 " + peopleAdd.Id_pep);
            Console.WriteLine("62 " + peopleAdd.Name);
            if (rdbDatabase.State == State.Successes)
            {
                var result = DatabaseService.Create(peopleAdd);
                if (result.State == State.Successes) result.Value = rdbDatabase.Value;
                return result;
            }
            Console.WriteLine("68 " + rdbDatabase);
            return rdbDatabase;
        }

        public static DatabaseResult AddAccess(PersonAddAccess peopleAdd)
        {
            peopleAdd.id_db = 1;
            peopleAdd.Username = "REST";

            var result = DatabaseService.Create(peopleAdd);
            return result;
        }

        public static DatabaseResult AddIdentifier(ForAddIdentifier identifier)
        {
            var result = DatabaseService.Create(identifier);
            return result;
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

        public static DatabaseResult DeleteIdentifier(int IdIdentifier)
        {
            var query = $"delete from people where ID_PEP={IdIdentifier}";
            return DatabaseService.ExecuteNonQuery(query);
        }



        public static bool CheckIdPepExists(int id_pep)
        {
            var query = $"SELECT COUNT(*) FROM people WHERE ID_PEP = {id_pep}";
            var result = DatabaseService.Get<COUNTDataBase>(query);

            if (result.State == State.Successes && result.Value is COUNTDataBase countData)
            {
                return countData.count > 0;
            }

            return false;
        }
    }
}