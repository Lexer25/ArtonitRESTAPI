

namespace OpenAPIArtonit.Legasy_Service
{
    public class SettingsDBLog 
    {
        public static string LogPath {  get; set; }
        public static string DBuser { get; set; }
        public static string DBpassword { get; set; }
        public static string DBpath { get; set; }
        public static string DBDataSource { get; set; }
        public static int DBport { get; set; }
        public static int DBDialect { get; set; }
        public static string DBCharset { get; set; }
        public static string DBRoule { get; set; }
        public static int DBConnectionlifetime { get; set; }
        public static bool DBPooling { get; set; }
        public static int DBMinPoolSize { get; set; }
        public static int DBMaxPoolSize { get; set; }
        public static int DBPacket_Size { get; set; }
        public static int DBServerType { get; set; }
        public static string GetDatabaseConnectionString()
        {
            return $@"User = {DBuser}; Password = {DBpassword}; Database = {DBpath} DataSource = {DBDataSource}; Port = {DBport}; 
            Dialect = {DBDialect}; Charset = {DBCharset}; Role ={DBRoule};Connection lifetime = {DBConnectionlifetime}; Pooling = {DBPooling};
            MinPoolSize = {DBMinPoolSize}; MaxPoolSize = {DBMaxPoolSize}; Packet Size = {DBPacket_Size}; ServerType = {DBServerType};";
        }
    }
}
