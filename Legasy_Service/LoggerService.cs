using System.Runtime.CompilerServices;

namespace OpenAPIArtonit.Legasy_Service
{
    public class LoggerService
    {
        [MethodImpl(MethodImplOptions.Synchronized)]
        public static void Log<T>(string log, string message)
        {
            var now = DateTime.Now;
            var datePoint = $"{now.Day}.{now.Month}.{now.Year} {now.Hour}:{now.Minute}:{now.Second}";
            var logMessage = $"{datePoint} LOG: {log}-{typeof(T).Name} Message: {message.PadRight(50)}\n";
            File.AppendAllText($@"{SettingsService.MainPath}\log\LogAt{now.Day}_{now.Month}_{now.Year}.txt", logMessage);
        }
    }
}
