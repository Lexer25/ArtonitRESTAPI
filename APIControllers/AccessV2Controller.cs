using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для категорий доступа. Контракт совпадает с v1: id_pep здесь не используется.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Access")]
    public class AccessV2Controller : BaseAPIController
    {
        /// <summary>
        /// Получить список категорий доступа.
        /// </summary>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10, string AccessName = "")
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var request = AccessDBController.GetAll(pageIndex, pageSize, AccessName);
            var result = request.Item1;

            if (result.State == State.Successes)
            {
                result.Value = new Pagination(result.Value, pageIndex, pageSize, request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }

        /// <summary>
        /// Добавить категорию доступа.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddAccess([FromBody] AccessForInsert? body)
        {
            if (body == null || string.IsNullOrWhiteSpace(body.Name))
            {
                return BadRequest("Название категории доступа обязательно");
            }

            return DataBaseStatusToWebStatusCode(AccessDBController.Add(new AccessForInsert(body)));
        }
    }
}
