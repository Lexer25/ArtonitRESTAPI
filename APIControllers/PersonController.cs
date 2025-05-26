using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
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
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Get([FromQuery] PersonBase request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            //вот это проверка берет True или False из метода 
            if (!PersonDBController.CheckIdPepExists(request.Id_pep))
            {
                return NotFound($"Пользователь с ID_PEPPPP {request.Id_pep} не найден");
            }

            var result = PersonDBController.GetById(request.Id_pep, User);
            //if (result.Item1.State == State.NotFound)
            //{
            //    return NotFound($"Пользователь с ID_PEP {request.Id_pep} не найден");
            //}

            //if (result.Item1.State == State.Successes && (result.Item1.Value == null ||
            //    (result.Item1.Value is IList<object> list && list.Count == 0)))
            //{
            //    return NotFound($"Пользователь с ID_PEP {request.Id_pep} не найден");
            //}

            return DataBaseStatusToWebStatusCode(result.Item1);
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
        /// Добавить идентификатор указанному person
        /// </summary>
        /// <param name="body">
        /// id_card - уникальный код карточки
        /// 
        /// id_pep - код сотрудника, которому принадлежит карточка
        /// 
        /// id_cardtype - тип идентификатора карты
        /// </param>
        /// <returns></returns>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost(nameof(AddIdentifier))]
        public IActionResult AddIdentifier([FromBody] IdentifirePostSee body)
        {
            //if (!ModelState.IsValid)
            //{
            //    return BadRequest(ModelState);
            //}
            if (body == null)
            {
                return NotFound("Ошибка: получено пустое тело сообщения. Проверьте формат отправленного сообщения (IdentifierConroller.cs:line 74)");
            }
            var validator = new IdentifierPostSeeValidator();

            if (!IdentifierDBController.CheckIdPepExists(body.Id_pep))
            {
                return NotFound($"Сотрудник с Id_pep = {body.Id_pep} не найден в таблице People");
            }


            //Console.WriteLine(body.Id_card);
            //Console.WriteLine(body.Id_cardtype);
            //Console.WriteLine(body.Id_pep);
            var validationResult = validator.Validate(body);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return BadRequest(errors);
            }

            return DataBaseStatusToWebStatusCode(IdentifierDBController.Add(new IdentifirePost(body)));
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
        public IActionResult Delete(int id)
        {
            return DataBaseStatusToWebStatusCode(PersonDBController.Delete(id));
        }

        //[HttpDelete]
        //public IActionResult DeleteIdentifier(int IdIdentifier)
        //{
        //    return DataBaseStatusToWebStatusCode(PersonDBController.DeleteIdentifier(IdIdentifier));
        //}
    }
}