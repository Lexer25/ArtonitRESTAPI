using ArtonitRESTAPI.Legasy_Service;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    public class BaseAPIController : ControllerBase
    {
        protected IActionResult Request(DatabaseResult result)
        {
            var people = new Dictionary<State, IActionResult>()
            {
                { State.Successes, Ok(result.Value)},
                { State.BadSQLRequest, BadRequest(result.ErrorMessage) },
                { State.NullSQLRequest, NotFound(result.ErrorMessage) },
                { State.NullDataBase, StatusCode(StatusCodes.Status500InternalServerError) },//поправить
            };
 
            return people[result.State];
        }
        public class Pagination()
        {
            public object context { get; set; }
            public int pageIndex { get; set; }
            public int pageSize { get; set; }
            public Pagination(object context, int pageIndex,int pageSize) : this()
            {
                this.context = context;
                this.pageIndex = pageIndex;
                this.pageSize = pageSize;
            }


        }
    }
}
