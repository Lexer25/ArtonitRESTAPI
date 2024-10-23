using OpenAPIArtonit.Anotation;

namespace ArtonitRESTAPI.Model
{
    public class RBDataBase
    {
        [DatabaseName("GEN_ID")]
        public int Id { get; set; }
    }
    public class COUNTDataBase
    {
        [DatabaseName("COUNT")]
        public int count { get; set; }
    }
}
