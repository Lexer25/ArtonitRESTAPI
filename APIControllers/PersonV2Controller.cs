using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Model;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для сотрудников. Вместо id_pep используется tabnum.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Person")]
    public class PersonV2Controller : V2ControllerBase
    {
        /// <summary>
        /// Получить информацию о сотруднике по tabnum.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника</param>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Get([FromQuery(Name = "tabnum")] string? tabnum)
        {
            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var (result, _) = PersonDBController.GetById(lookup.IdPep, User);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            if (result.Value is not List<PersonGet> persons || persons.Count == 0)
            {
                return NotFound($"Сотрудник с tabnum '{lookup.TabNum}' не найден");
            }

            return Ok(persons.Select(ToV2).ToList());
        }

        /// <summary>
        /// Получить список сотрудников. Список разбит на страницы.
        /// </summary>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var request = PersonDBController.GetAll(pageIndex, pageSize);
            var result = request.Item1;

            if (result.State == State.Successes && result.Value is List<PersonGet> persons)
            {
                result.Value = new Pagination(
                    persons.Select(ToV2).ToList(),
                    pageIndex,
                    pageSize,
                    request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить сотрудника. Если tabnum не указан, он формируется как tn_&lt;id_pep&gt;.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Add([FromBody] PersonV2PostSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2PostSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var result = PersonV2DBController.Add(new PersonV2Post(body));
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить категорию доступа сотруднику, найденному по tabnum.
        /// </summary>
        [HttpPost(nameof(AddAccess))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddAccess([FromBody] PersonV2AddAccessSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2AddAccessSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = PersonV2DBController.AddAccess(lookup.IdPep, body.Id_accessname!.Value);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить идентификатор сотруднику, найденному по tabnum.
        /// </summary>
        [HttpPost(nameof(AddIdentifier))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddIdentifier([FromBody] IdentifierV2PostSee? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new IdentifierV2PostSeeValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = IdentifierV2DBController.Add(body, lookup.IdPep);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Изменить информацию о сотруднике, найденном по tabnum.
        /// </summary>
        [HttpPatch]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Update([FromBody] PersonV2Patch? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            var validation = new PersonV2PatchValidator().Validate(body);
            if (!validation.IsValid)
            {
                return BadRequest(validation.Errors.Select(e => e.ErrorMessage));
            }

            var lookup = TabnumResolver.Resolve(body.TabNum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = PersonV2DBController.Update(lookup.IdPep, body);
            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Удалить сотрудника по tabnum.
        /// </summary>
        [HttpDelete]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete([FromQuery(Name = "tabnum")] string? tabnum)
        {
            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            return DataBaseStatusToWebStatusCode(PersonV2DBController.Delete(lookup.IdPep));
        }

        /// <summary>
        /// Удалить идентификатор по номеру карты.
        /// </summary>
        [HttpDelete("DeleteIdentifier")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult DeleteIdentifier([FromQuery] string? idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return BadRequest("Параметр idCard обязателен");
            }

            return DataBaseStatusToWebStatusCode(IdentifierDBController.DeleteIdentifier(idCard));
        }

        private static PersonV2Get ToV2(PersonGet person)
        {
            return new PersonV2Get
            {
                TabNum = person.TabNum,
                Surname = person.Surname,
                Name = person.Name,
                Patronymic = person.Patronymic
            };
        }
    }
}
