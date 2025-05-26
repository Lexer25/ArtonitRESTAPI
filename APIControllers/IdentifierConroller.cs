using ArtonitRESTAPI.APIControllers;
using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using FluentValidation;
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
        /// <param name="pageIndex">Индекс начала выборки</param>
        /// <param name="pageSize">Размер выборки</param>
        /// <param name="identifire">Название идентификатор</param>
        /// <returns></returns>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
        /// <param name="body">
        /// id_card - уникальный код карточки
        /// 
        /// id_pep - код сотрудника, которому принадлежит карточка
        /// 
        /// id_cardtype - тип идентификатора карты
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult addIdentifierForpeopel([FromBody] IdentifirePostSee body)
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
            

            //Console.WriteLine(body.Id_card);
            //Console.WriteLine(body.Id_cardtype);
            //Console.WriteLine(body.Id_pep);
            var validationResult = validator.Validate(body);

            if (!validationResult.IsValid)
            {
                var errors = validationResult.Errors.Select(e => e.ErrorMessage);
                return BadRequest(errors);
            }

            if (!IdentifierDBController.CheckIdPepExists(body.Id_pep))
            {
                return NotFound($"Сотрудник с Id_pep = {body.Id_pep} не найден в таблице People");
            }

            return DataBaseStatusToWebStatusCode(IdentifierDBController.Add(new IdentifirePost(body)));
        }

        //[HttpDelete]
        //[ProducesResponseType(StatusCodes.Status200OK)]
        //[ProducesResponseType(StatusCodes.Status400BadRequest)]
        //[ProducesResponseType(StatusCodes.Status404NotFound)]
        //[ProducesResponseType(StatusCodes.Status500InternalServerError)]
        //public IActionResult Delete(string idCard)
        //{
        //    return DataBaseStatusToWebStatusCode(IdentifierDBController.DeleteIdentifier(idCard));
            
        //}



    }


}


