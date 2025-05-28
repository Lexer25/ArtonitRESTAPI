using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.Legasy_Service;
using OpenAPIArtonit.Model;
using System.Security.Claims;
using System.Text.RegularExpressions;


namespace ArtonitRESTAPI.DBControllers
{
    public class IdentifierDBController
    {

        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize,string identifire)
        {
            var query_list = $@"select
                first {pageSize} skip {pageSize * pageIndex}
                c.id_card, c.id_cardtype, c.id_pep";
            var query_count = $@"select count(*)";
            var base_query = $@"
                from card c ";
            if (identifire != "") base_query += $@" where c.id_card='{identifire}'";
            Console.WriteLine("23 " + query_list + base_query);
            Console.WriteLine("24 " + query_count + base_query);
                return new Tuple<DatabaseResult, int>
                (DatabaseService.GetList<IdentifierList>(query_list + base_query),
                ((COUNTDataBase)DatabaseService.Get<COUNTDataBase>(query_count + base_query).Value).count);
        }
        public static DatabaseResult Add(IdentifirePost identifireAdd)
        {
            identifireAdd.Active = 1;
            identifireAdd.Timestart = DateTime.Now;
            identifireAdd.Status = 1;
            identifireAdd.Id_db = 1;  
            //if (identifireAdd.Id_cardtype == 1)
            //{
            //    if (!Regex.IsMatch(identifireAdd.Id_card, "[A-F0-9]{8}")) return State.BadSQLRequest("sdsds");
            //}
            //else if (identifireAdd.Id_cardtype == 2)
            //{
                 
            //}
            //else if (identifireAdd.Id_cardtype == 3)
            //{

            //}
            //else if (identifireAdd.Id_cardtype == 4)
            //{

            //}
            var result = DatabaseService.Create(identifireAdd);
                return result;

            
        }

        public static DatabaseResult DeleteIdentifier(string IdIdentifier)
        {
            var query = $"DELETE FROM CARD WHERE ID_CARD='{IdIdentifier}'";
                var result = DatabaseService.ExecuteNonQuery(query);
                return result;
        }

        public static bool CheckIdPepExists(int idPep)
        {
            var query = $@"select count(*) from people p where p.id_pep = {idPep}";
            var result = ((COUNTDataBase)DatabaseService.Get<COUNTDataBase>(query).Value).count;
            return result > 0;
        }

    }
}
