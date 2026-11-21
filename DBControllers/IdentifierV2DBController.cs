using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Операции над идентификаторами для REST API v2 (идентификация по tabnum).
    /// </summary>
    public static class IdentifierV2DBController
    {
        /// <summary>
        /// Возвращает страницу идентификаторов с tabnum вместо id_pep.
        /// </summary>
        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize, string? identifire)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;

            var parameters = new Dictionary<string, object>();

            var queryList = $@"select
                first {pageSize} skip {pageSize * pageIndex}
                c.id_card, c.id_cardtype, p.tabnum";

            const string queryCount = "select count(*)";

            var baseQuery = @"
                from card c
                join people p on p.id_pep = c.id_pep and p.id_db = c.id_db";

            if (!string.IsNullOrWhiteSpace(identifire))
            {
                baseQuery += " where c.id_card = @idCard";
                parameters["@idCard"] = identifire;
            }

            var listResult = DatabaseService.GetListParameterized<IdentifierV2List>(queryList + baseQuery, parameters);
            var countResult = DatabaseService.GetParameterized<COUNTDataBase>(queryCount + baseQuery, parameters);

            var count = countResult.State == State.Successes && countResult.Value is COUNTDataBase countDataBase
                ? countDataBase.count
                : 0;

            return new Tuple<DatabaseResult, int>(listResult, count);
        }

        /// <summary>
        /// Добавляет идентификатор сотруднику с указанным id_pep.
        /// </summary>
        public static DatabaseResult Add(IdentifierV2PostSee body, int idPep)
        {
            var post = new IdentifirePost(new IdentifirePostSee
            {
                Id_card = body.Id_card,
                Id_pep = idPep,
                Id_cardtype = body.Id_cardtype
            });

            return IdentifierDBController.Add(post);
        }
    }
}
