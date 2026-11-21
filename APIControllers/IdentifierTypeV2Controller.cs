using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для типов идентификаторов. Контракт совпадает с v1: id_pep здесь не используется.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/IdentifierType")]
    public class IdentifierTypeV2Controller : BaseAPIController
    {
        /// <summary>
        /// Получить список типов идентификаторов.
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

            var request = IdentifierTypeDBController.GetAll(pageIndex, pageSize);
            var result = request.Item1;

            if (result.State == State.Successes)
            {
                result.Value = new Pagination(result.Value, pageIndex, pageSize, request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }
    }
}
