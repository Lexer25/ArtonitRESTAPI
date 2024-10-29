using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.Legasy_Service;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Legasy_Service;
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
        
        [HttpPost,Authorize]
        public IActionResult Add([FromBody] PersonPostSee body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Add(new PersonPost(body)));
        }
        [HttpPatch, Authorize]
        public IActionResult Update([FromBody] PersonPach body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Update(body));
        }
        [HttpDelete, Authorize]
        public IActionResult Delite(int id)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Delete(id));
        }
    }
}
