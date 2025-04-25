using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;


namespace ArtonitRESTAPI.APIControllers
{
    [ApiController]
    [Route("[controller]")]

    public class IdentifierTypeConroller : BaseAPIController
    {
        
        //получаю список типов идентификаторов
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10)
        {
            //var request = IdentifierDBController.GetAll(pageIndex, pageSize);
            var request = IdentifierTypeDBController.GetAll(pageIndex, pageSize);
            var allpersons = request.Item1;
            if (allpersons.State == State.Successes) allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
            return DataBaseStatusToWebStatusCode(allpersons);
        }

      /*
        [HttpPost]
        public IActionResult addIdentifierForpeopel([FromBody] IdentifirePostSee body)
        {
            return DataBaseStatusToWebStatusCode(IdentifierDBController.Add(new IdentifirePost(body)));
        }

*/

    }


}


