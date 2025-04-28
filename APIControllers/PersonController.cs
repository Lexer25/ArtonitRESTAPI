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
        
        
        /// <summary>
        /// Получить информацию о person по id_pep
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get(int id=1)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.GetById(id));
        }

        /// <summary>
        /// Получить список person. Список разбит на страницы
        /// </summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <returns></returns>
        [HttpGet(nameof(GetList))]
        public IActionResult GetList(int pageIndex = 1, int pageSize = 10)
        {
            var request = PersonDBController.GetAll(pageIndex, pageSize);
            var allpersons = request.Item1;
            if (allpersons.State == State.Successes) allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
            return DataBaseStatusToWebStatusCode(allpersons);
        }

        /// <summary>
        /// Добавить person
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost]
        
        public IActionResult Add([FromBody] PersonPostSee body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Add(new PersonPost(body)));
        }
        
       
        /// <summary>
        /// Добавить категорию доступа для person
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        [HttpPost(nameof(AddAccess))]
        public IActionResult AddAccess([FromBody] PersonAddAccessSee body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.AddAccess(new PersonAddAccess(body)));


        }
        
      
        /// <summary>
        /// Изменить информацию о person
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        
        [HttpPatch]
        public IActionResult Update([FromBody] PersonPach body)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Update(body));
        }


        /// <summary>
        /// Удалить person
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [HttpDelete]
        public IActionResult Delite(int id)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Delete(id));
        }
    }
}
