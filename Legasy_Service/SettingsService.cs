using Newtonsoft.Json;

namespace OpenAPIArtonit.Legasy_Service
{
    public class SettingsService
    {
        private static string fileName = "appsettings.json";

        public static string MainPath= "C:\\ArtonitRestApi";

     //   public static string Url = "http://*:8011";

        public string _databaseConnectionString
        {
            get { return DatabaseConnectionString; }
            set { DatabaseConnectionString = value; }
        }

        public static string DatabaseConnectionString = 
            "User = SYSDBA; Password = temp; " +
            "Database = C:\\Program Files (x86)\\Cardsoft\\DuoSE\\Access\\ShieldPro_rest.GDB; " +
            "DataSource = 127.0.0.1; Port = 3050; Dialect = 3; Charset = win1251; Role =; " +
            "Connection lifetime = 15; Pooling = true; MinPoolSize = 0; MaxPoolSize = 50; " +
            "Packet Size = 8192; ServerType = 0;";


        public static void Update()
        {
           if (!File.Exists($@"{MainPath}\{fileName}"))
            {
                string json = JsonConvert.SerializeObject(new SettingsService());
                File.WriteAllText($@"{MainPath}\{fileName}", json);
            }
            else
            {
                var json = File.ReadAllText($@"{MainPath}\{fileName}");
                var settings = JsonConvert.DeserializeObject<SettingsService>(json);
                DatabaseConnectionString = settings._databaseConnectionString;
            }
        }
    }
}
