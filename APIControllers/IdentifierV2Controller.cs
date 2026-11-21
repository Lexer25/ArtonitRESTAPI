using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для идентификаторов. Вместо id_pep используется tabnum.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Identifier")]
    public class IdentifierV2Controller : V2ControllerBase
    {
        /// <summary>
        /// Получить список идентификаторов (с разбиением на страницы) либо найти идентификатор по номеру.
        /// В ответе вместо id_pep возвращается tabnum.
        /// </summary>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10, string identifire = "")
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var request = IdentifierV2DBController.GetAll(pageIndex, pageSize, identifire);
            var result = request.Item1;

            if (result.State == State.Successes)
            {
                result.Value = new Pagination(result.Value, pageIndex, pageSize, request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить идентификатор сотруднику, найденному по tabnum.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Add([FromBody] IdentifierV2PostSee? body)
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
        /// Удалить идентификатор по номеру карты.
        /// </summary>
        [HttpDelete("DeleteIdentifier")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete([FromQuery] string? idCard)
        {
            if (string.IsNullOrWhiteSpace(idCard))
            {
                return BadRequest("Параметр idCard обязателен");
            }

            return DataBaseStatusToWebStatusCode(IdentifierDBController.DeleteIdentifier(idCard));
        }
    }
}
