using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.Legasy_Service;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Model;

namespace OpenAPIArtonit.APIControllers
{
    [ApiController]
    [Route("[controller]")]
    public class PersonController : BaseAPIController
    {
        [HttpGet]
        public IActionResult Get(int id)
        {
            return Request(PersonDBController.GetById(id));
        }
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 1, int pageSize = 10)
        {
            Console.WriteLine(new Uri(HttpContext.Request.Host.Value + HttpContext.Request.Path));
            var allpersons = PersonDBController.GetAll(pageIndex, pageSize);
            if (allpersons.State == State.Successes)
            {
                allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize);
            }
            return Request(allpersons);
        }
        [HttpPost]
        public IActionResult Add([FromBody] PersonPostSee body)
        {
            return Request(PersonDBController.Add(new PersonPost(body)));
        }
        [HttpPatch]
        public IActionResult Update([FromBody] PersonPach body)
        {
            return Request(PersonDBController.Update(body));
        }
        [HttpDelete]
        public IActionResult Delite(int id)
        {
            return Request(PersonDBController.Delete(id));
        }
    }
}
