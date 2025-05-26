using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace ArtonitRESTAPI.APIControllers
{
    [ApiController]
    [Route("[controller]")]
    public class AccessController : BaseAPIController
    {
        private readonly ILogger<AccessController> _logger;

        public AccessController(ILogger<AccessController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Получить список типов идентификаторов
        /// </summary>
        /// <param name="pageIndex">Номер страницы</param>
        /// <param name="pageSize">Размер страницы</param>
        /// <param name="AccessName">Название категории доступа</param>
        /// <returns></returns>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(int pageIndex = 0, int pageSize = 10, string AccessName = "")
        {
            _logger.LogInformation("Начало получения списка типов идентификаторов");
            try
            {
                var request = AccessDBController.GetAll(pageIndex, pageSize, AccessName);
                var allpersons = request.Item1;
                if (allpersons.State == State.Successes)
                    allpersons.Value = new Pagination(allpersons.Value, pageIndex, pageSize, request.Item2);
                _logger.LogInformation("Список типов идентификаторов успешно получен");
                return DataBaseStatusToWebStatusCode(allpersons);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при получении списка типов идентификаторов: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// Добавить право доступа
        /// </summary>
        /// <param name="body">Название категории доступа</param>
        /// <returns></returns>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddAccess([FromBody] AccessForInsert body)
        {
            _logger.LogInformation("Начало добавления права доступа");
            try
            {
                var result = DataBaseStatusToWebStatusCode(AccessDBController.Add(new AccessForInsert(body)));
                _logger.LogInformation("Право доступа успешно добавлено");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при добавлении права доступа: {Message}", ex.Message);
                throw;
            }
        }
    }
}