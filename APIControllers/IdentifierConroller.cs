using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using System.Text.RegularExpressions;


namespace ArtonitRESTAPI.APIControllers
{
    [ApiController]
    [Route("[controller]")]

    public class IdentifierController : BaseAPIController
    {

        //получаю список типов идентификаторов
        /// <summary>
        /// Получить список идентификаторов (с разбиением на странмцы), либо поиск идентификатора по его номеру.
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10,string identifire="")
        {
            //var request = IdentifierDBController.GetAll(pageIndex, pageSize);
            var request = IdentifierDBController.GetAll(pageIndex, pageSize, identifire);
            var allpersons = request.Item1;
            if (allpersons.State == State.Successes) allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
            return DataBaseStatusToWebStatusCode(allpersons);
        }
        
       
        /// <summary>
        /// Добавить идентификатор указанному person
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
         [HttpPost]
        public IActionResult addIdentifierForpeopel([FromBody] IdentifirePostSee body)
        {
            if (!Regex.IsMatch(body.Id_card, "[A-F0-9]{8}")) return BadRequest("sdsds");
            return DataBaseStatusToWebStatusCode(IdentifierDBController.Add(new IdentifirePost(body)));
        }



    }


}


