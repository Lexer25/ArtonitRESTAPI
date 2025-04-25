using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.Legasy_Service;
using OpenAPIArtonit.Model;
using System.Security.Claims;


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
            identifireAdd.Timestart = "now";
            identifireAdd.Status = 1;
            identifireAdd.Id_db = 1;    
           var result = DatabaseService.Create(identifireAdd);
                return result;

            
        }



    }
}
