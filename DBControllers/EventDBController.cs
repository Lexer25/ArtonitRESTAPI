using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;

namespace ArtonitRESTAPI.DBControllers
{
    public class EventDBController
    {
        public static DatabaseResult GetPassIdDg(string id, string fromDate, string toDate, string deviceGroupName)
        {
            Console.WriteLine($"Debug: deviceGroupName (raw) = '{deviceGroupName}'");
            var query = $@"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN devgroup dg ON dg.id_dev = e.id_dev
                JOIN devgroup dg2 ON dg.id_parent = dg2.id_devgroup
                WHERE e.datetime BETWEEN '{fromDate}' AND '{toDate}'
                AND E.ID_EVENTTYPE IN (50)
                AND dg2.name = '{deviceGroupName}' 
                AND e.ess1 = '{id}'
                ORDER BY e.datetime";

            Console.WriteLine($"Debug: Generated Query = '{query}'");
            return DatabaseService.GetList<EventResponse>(query);
        }

        public static DatabaseResult GetPassIdAp(string id, string fromDate, string toDate, string accessPointName)
        {
            Console.WriteLine($"Debug: accessPointName (raw) = '{accessPointName}'");
            var query = $@"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN device d ON d.id_dev = e.id_dev
                WHERE d.name = '{accessPointName}' 
                AND e.datetime BETWEEN '{fromDate}' AND '{toDate}'
                AND E.ID_EVENTTYPE IN (50)
                AND e.ess1 = '{id}'
                ORDER BY e.datetime";

            Console.WriteLine($"Debug: Generated Query = '{query}'");
            return DatabaseService.GetList<EventResponse>(query);
        }

        public static DatabaseResult GetPassAllDG(string fromDate, string toDate, string deviceGroupName)
        {
            Console.WriteLine($"Debug: deviceGroupName (raw) = '{deviceGroupName}'");
            var query = $@"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN devgroup dg ON dg.id_dev = e.id_dev
                JOIN devgroup dg2 ON dg.id_parent = dg2.id_devgroup
                WHERE e.datetime BETWEEN '{fromDate}' AND '{toDate}'
                AND E.ID_EVENTTYPE IN (50)
                AND dg2.name = '{deviceGroupName}'
                ORDER BY e.datetime";

            Console.WriteLine($"Debug: Generated Query = '{query}'");
            return DatabaseService.GetList<EventResponse>(query);
        }

        public static DatabaseResult GetPassAllAp(string fromDate, string toDate, string accessPointName)
        {
            Console.WriteLine($"Debug: accessPointName (raw) = '{accessPointName}'");
            var query = $@"
                SELECT FIRST 1000 DISTINCT e.ess1, e.datetime 
                FROM events e
                JOIN device d ON d.id_dev = e.id_dev
                WHERE d.name = '{accessPointName}' 
                AND e.datetime BETWEEN '{fromDate}' AND '{toDate}'
                AND E.ID_EVENTTYPE IN (50)
                ORDER BY e.datetime";

            Console.WriteLine($"Debug: Generated Query = '{query}'");
            return DatabaseService.GetList<EventResponse>(query);
        }
    }
}