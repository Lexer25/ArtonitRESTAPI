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
        public IActionResult Get(int id=1)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.GetById(id));
        }
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 1, int pageSize = 10)
        {
            var request = PersonDBController.GetAll(pageIndex, pageSize);
            var allpersons = request.Item1;
            if (allpersons.State == State.Successes) allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
            return DataBaseStatusToWebStatusCode(allpersons);
        }
        [HttpPost]
        public IActionResult Add([FromBody] PersonPostSee body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Add(new PersonPost(body)));
        }
        [HttpPatch]
        public IActionResult Update([FromBody] PersonPach body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Update(body));
        }
        [HttpDelete]
        public IActionResult Delite(int id)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Delete(id));
        }
    }
}
