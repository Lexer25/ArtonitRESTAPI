using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Выборка событий для REST API v2. Запросы параметризованы, сотрудник
    /// определяется по tabnum (преобразуется в id_pep до вызова методов).
    /// </summary>
    public static class EventV2DBController
    {
        /// <summary>
        /// События сотрудника по названию группы устройств.
        /// </summary>
        public static DatabaseResult GetPassIdDg(int idPep, DateTime fromDate, DateTime toDate, string deviceGroupName)
        {
            const string query = @"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN devgroup dg ON dg.id_dev = e.id_dev
                JOIN devgroup dg2 ON dg.id_parent = dg2.id_devgroup
                WHERE e.datetime BETWEEN @fromDate AND @toDate
                AND e.id_eventtype IN (50)
                AND dg2.name = @deviceGroupName 
                AND e.ess1 = @idPep
                ORDER BY e.datetime";

            return DatabaseService.GetListParameterized<EventResponse>(
                query, PeriodParameters(fromDate, toDate, "@deviceGroupName", deviceGroupName, idPep));
        }

        /// <summary>
        /// События сотрудника по названию точки прохода.
        /// </summary>
        public static DatabaseResult GetPassIdAp(int idPep, DateTime fromDate, DateTime toDate, string accessPointName)
        {
            const string query = @"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN device d ON d.id_dev = e.id_dev
                WHERE d.name = @accessPointName 
                AND e.datetime BETWEEN @fromDate AND @toDate
                AND e.id_eventtype IN (50)
                AND e.ess1 = @idPep
                ORDER BY e.datetime";

            return DatabaseService.GetListParameterized<EventResponse>(
                query, PeriodParameters(fromDate, toDate, "@accessPointName", accessPointName, idPep));
        }

        /// <summary>
        /// Все события по названию группы устройств (с tabnum сотрудника).
        /// </summary>
        public static DatabaseResult GetPassAllDg(DateTime fromDate, DateTime toDate, string deviceGroupName)
        {
            const string query = @"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime, coalesce(p.tabnum, '') AS tabnum
                FROM events e
                JOIN devgroup dg ON dg.id_dev = e.id_dev
                JOIN devgroup dg2 ON dg.id_parent = dg2.id_devgroup
                LEFT JOIN people p ON p.id_pep = e.ess1 AND p.id_db = e.id_db
                WHERE e.datetime BETWEEN @fromDate AND @toDate
                AND e.id_eventtype IN (50)
                AND dg2.name = @deviceGroupName
                ORDER BY e.datetime";

            return DatabaseService.GetListParameterized<EventV2Row>(
                query, PeriodParameters(fromDate, toDate, "@deviceGroupName", deviceGroupName, null));
        }

        /// <summary>
        /// Все события по названию точки прохода (с tabnum сотрудника).
        /// </summary>
        public static DatabaseResult GetPassAllAp(DateTime fromDate, DateTime toDate, string accessPointName)
        {
            const string query = @"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime, coalesce(p.tabnum, '') AS tabnum
                FROM events e
                JOIN device d ON d.id_dev = e.id_dev
                LEFT JOIN people p ON p.id_pep = e.ess1 AND p.id_db = e.id_db
                WHERE d.name = @accessPointName 
                AND e.datetime BETWEEN @fromDate AND @toDate
                AND e.id_eventtype IN (50)
                ORDER BY e.datetime";

            return DatabaseService.GetListParameterized<EventV2Row>(
                query, PeriodParameters(fromDate, toDate, "@accessPointName", accessPointName, null));
        }

        private static Dictionary<string, object> PeriodParameters(
            DateTime fromDate,
            DateTime toDate,
            string nameParameter,
            string name,
            int? idPep)
        {
            var parameters = new Dictionary<string, object>
            {
                ["@fromDate"] = fromDate,
                ["@toDate"] = toDate,
                [nameParameter] = name
            };

            if (idPep.HasValue)
            {
                parameters["@idPep"] = idPep.Value;
            }

            return parameters;
        }
    }
}
