using OpenAPIArtonit.Anotation; 

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
}