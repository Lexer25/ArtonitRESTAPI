using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;

namespace ArtonitRESTAPI.DBControllers
{
    public class AccessDBController
    {

        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize, string accessName)
        {
            var query_list = $@"select
                first {pageSize} skip {pageSize * pageIndex}
                an.id_accessname, an.name";
            var query_count = $@"select count(*)";
            var base_query = $@"
                from accessname an ";
            if (accessName != "") base_query += $@" where an.name containing '{accessName}'";
            Console.WriteLine("23 " + query_list + base_query);
            Console.WriteLine("24 " + query_count + base_query);
            return new Tuple<DatabaseResult, int>
            (DatabaseService.GetList<AccessList>(query_list + base_query),
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
