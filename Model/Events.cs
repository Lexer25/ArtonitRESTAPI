using OpenAPIArtonit.Anotation;
using System.Text.Json.Serialization;

namespace ArtonitRESTAPI.Model
{
    public class EventResponse
    {
        [DatabaseName("ESS1")]
        public string Id { get; set; }

        [DatabaseName("DATETIME")]
        public string Timestamp { get; set; }

        public EventResponse()
        {
        }

        public EventResponse(string id, string timestamp)
        {
            Id = id;
            Timestamp = timestamp;
        }
    }

    /// <summary>
    /// Событие в REST API v2: сотрудник представлен табельным номером tabnum.
    /// </summary>
    public class EventV2Response
    {
        [JsonPropertyName("tabnum")]
        public string TabNum { get; set; } = string.Empty;

        [DatabaseName("DATETIME")]
        public string Timestamp { get; set; } = string.Empty;
    }

    /// <summary>
    /// Строка выборки событий с табельным номером сотрудника.
    /// </summary>
    public class EventV2Row
    {
        [DatabaseName("ESS1")]
        public int? Id { get; set; }

        [DatabaseName("DATETIME")]
        public string Timestamp { get; set; } = string.Empty;

        [DatabaseName("TABNUM")]
        public string TabNum { get; set; } = string.Empty;
    }
}