using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;

namespace ArtonitRESTAPI.APIControllers
{
    [ApiController]
    [Route("[controller]")]

    public class AccessController : BaseAPIController
    {

        //получаю список типов идентификаторов
        /// <summary>
        /// Получить список типов идентификаторов
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="AccessName"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10, string AccessName = "")
        {
            //var request = IdentifierDBController.GetAll(pageIndex, pageSize);
            var request = AccessDBController.GetAll(pageIndex, pageSize, AccessName);
            var allpersons = request.Item1;
            if (allpersons.State == State.Successes) allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
            return DataBaseStatusToWebStatusCode(allpersons);
        }


        /*

        [HttpPost]
        public IActionResult addIdentifierForpeopel([FromBody] IdentifirePostSee body)
        {
            if (!Regex.IsMatch(body.Id_card, "[A-F0-9]{8}")) return BadRequest("sdsds");
            return DataBaseStatusToWebStatusCode(IdentifierDBController.Add(new IdentifirePost(body)));
        }


        */
    }
}
