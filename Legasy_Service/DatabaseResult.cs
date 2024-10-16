namespace OpenAPIArtonit.Legasy_Service
{
    public class DatabaseResult
    {
        public State State { get; set; }

        public object Value { get; set; }

        public string ErrorMessage { get; set; }
    }
    public enum State
    {
        Successes,
        Error
    }
}
