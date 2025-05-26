using Microsoft.Extensions.Configuration;
using System.IO;

namespace OpenAPIArtonit.Legasy_Service
{
    public class SettingsService
    {
        private readonly IConfiguration _configuration;
        public static string MainPath = "C:\\ArtonitRestApi";

        public string DatabaseConnectionString { get; private set; }

        public SettingsService(IConfiguration configuration)
        {
            _configuration = configuration;
            DatabaseConnectionString = _configuration.GetConnectionString("DatabaseConnectionString");
        }

        public void Update()
        {
            if (!Directory.Exists(MainPath))
            {
                Directory.CreateDirectory(MainPath);
            }
            DatabaseConnectionString = _configuration.GetConnectionString("DatabaseConnectionString");
        }
    }
}