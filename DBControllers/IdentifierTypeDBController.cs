using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using System.Security.Claims;

namespace ArtonitRESTAPI.DBControllers
{
    public class IdentifierTypeDBController
    {

        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize)
        {
            var query_list = $@"select
                first {pageSize} skip {pageSize * pageIndex}
                ct.id, ct.name, ct.description";
            var query_count = $@"select count(*)";
            var base_query = $@"
                from cardtype ct";

            return new Tuple<DatabaseResult, int>
                (DatabaseService.GetList<identifierTypeList>(query_list + base_query),
                ((COUNTDataBase)DatabaseService.Get<COUNTDataBase>(query_count + base_query).Value).count);
        }
        public static DatabaseResult Add(IdentifirePost identifireAdd)
        {
            var userIdentity = ClaimsPrincipal.Current;
            var result = DatabaseService.Create(identifireAdd);
            return result;


        }
    }
}
