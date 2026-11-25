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
        /// Перегрузка сохраняет прежний контракт GetList с фильтром identifire.
        /// </summary>
        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize, string? identifire)
        {
            return GetAll(pageIndex, pageSize, new IdentifierV2Filter
            {
                IdCard = string.IsNullOrWhiteSpace(identifire) ? null : identifire
            });
        }

        /// <summary>
        /// Возвращает страницу идентификаторов по необязательным фильтрам:
        /// номер карты, tabnum, тип, активность, TIMESTART, TIMEEND и CREATEDAT.
        /// </summary>
        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize, IdentifierV2Filter? filter)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var parameters = new Dictionary<string, object>();
            var where = BuildWhere(filter, parameters);

            // long защищает от переполнения int при большом pageIndex.
            var skip = (long)pageIndex * pageSize;

            var queryList = $@"
                select
                    first {pageSize} skip {skip}
                    c.id_card, c.id_cardtype, p.tabnum,
                    c.timestart, c.timeend, c.""ACTIVE"", c.createdat
                from card c
                join people p on p.id_pep = c.id_pep and p.id_db = c.id_db{where}
                order by c.id_card, c.id_db";

            var queryCount = $@"
                select count(*)
                from card c
                join people p on p.id_pep = c.id_pep and p.id_db = c.id_db{where}";

            var countResult = DatabaseService.GetParameterized<COUNTDataBase>(queryCount, parameters);
            if (countResult.State != State.Successes || countResult.Value is not COUNTDataBase count)
            {
                return new Tuple<DatabaseResult, int>(countResult, 0);
            }

            var listResult = DatabaseService.GetListParameterized<IdentifierV2List>(queryList, parameters);
            return new Tuple<DatabaseResult, int>(listResult, count.count);
        }

        /// <summary>
        /// Возвращает статистику по таблице CARD после применения фильтров:
        /// всего, активных (ACTIVE = 1) и неактивных (ACTIVE = 0) записей.
        /// </summary>
        public static DatabaseResult GetCount(IdentifierV2Filter? filter)
        {
            var parameters = new Dictionary<string, object>();
            var where = BuildWhere(filter, parameters);

            var query = $@"
                select
                    count(*) as total_count,
                    coalesce(sum(case when c.""ACTIVE"" = 1 then 1 else 0 end), 0) as active_count,
                    coalesce(sum(case when c.""ACTIVE"" = 0 then 1 else 0 end), 0) as inactive_count
                from card c
                join people p on p.id_pep = c.id_pep and p.id_db = c.id_db{where}";

            return DatabaseService.GetParameterized<IdentifierV2Count>(query, parameters);
        }

        /// <summary>
        /// Строит общее условие WHERE для списка и статистики, добавляя
        /// параметры в <paramref name="parameters"/>.
        /// </summary>
        private static string BuildWhere(
            IdentifierV2Filter? filter,
            IDictionary<string, object> parameters)
        {
            var conditions = new List<string>();

            var idCard = filter?.IdCard;
            if (!string.IsNullOrWhiteSpace(idCard))
            {
                conditions.Add("c.id_card = @idCard");
                parameters["@idCard"] = idCard.Trim();
            }

            var tabNum = filter?.TabNum;
            if (!string.IsNullOrWhiteSpace(tabNum))
            {
                conditions.Add("p.tabnum = @tabnum");
                parameters["@tabnum"] = tabNum.Trim();
            }

            if (filter?.IdCardtype is int idCardtype)
            {
                conditions.Add("c.id_cardtype = @idCardtype");
                parameters["@idCardtype"] = idCardtype;
            }

            if (filter?.Active is bool active)
            {
                conditions.Add("c.\"ACTIVE\" = @active");
                parameters["@active"] = active ? 1 : 0;
            }

            AddDateCondition(conditions, parameters, filter?.TimeStart, "c.timestart", "@timestart");
            AddDateCondition(conditions, parameters, filter?.TimeEnd, "c.timeend", "@timeend");
            AddDateCondition(conditions, parameters, filter?.CreatedAt, "c.createdat", "@createdat");

            return conditions.Count == 0
                ? string.Empty
                : " where " + string.Join(" and ", conditions);
        }

        /// <summary>
        /// Добавляет условие по колонке-дате. Для значения без времени
        /// используется интервал [дата 00:00; следующая дата 00:00).
        /// </summary>
        private static void AddDateCondition(
            ICollection<string> conditions,
            IDictionary<string, object> parameters,
            IdentifierV2DateFilter? filter,
            string column,
            string parameter)
        {
            if (filter == null) return;

            if (filter.DateOnly)
            {
                conditions.Add($"{column} >= {parameter} and {column} < {parameter}End");
                parameters[parameter] = filter.Value.Date;
                parameters[parameter + "End"] = filter.Value.Date.AddDays(1);
            }
            else
            {
                conditions.Add($"{column} = {parameter}");
                parameters[parameter] = filter.Value;
            }
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
