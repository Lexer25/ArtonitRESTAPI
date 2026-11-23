using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для категорий доступа. Идентификатор категории — ID_ACCESSNAME
    /// (таблица ACCESSNAME). Список сотрудников с категорией строится по SS_ACCESSUSER
    /// и возвращает их tabnum.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Access")]
    public class AccessV2Controller : BaseAPIController
    {
        /// <summary>
        /// Получить список категорий доступа.
        /// </summary>
        /// <param name="pageIndex">Номер страницы, начиная с 0.</param>
        /// <param name="pageSize">Размер страницы, от 1 до 1000.</param>
        /// <param name="AccessName">Подстрока для поиска по названию категории.</param>
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
        /// <param name="body">Название категории доступа (обязательно, до 50 символов).</param>
        [HttpPost]
        [ProducesResponseType(typeof(AccessCategory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult AddAccess([FromBody] AccessForInsert? body)
        {
            if (body == null)
            {
                return BadRequest("Тело запроса обязательно");
            }

            if (string.IsNullOrWhiteSpace(body.Name))
            {
                return BadRequest("Название категории доступа обязательно");
            }

            var name = body.Name.Trim();
            if (name.Length > AccessDBController.MaxNameLength)
            {
                return BadRequest($"Название категории доступа не должно превышать {AccessDBController.MaxNameLength} символов");
            }

            return DataBaseStatusToWebStatusCode(AccessDBController.Add(name));
        }

        /// <summary>
        /// Удалить категорию доступа по её идентификатору ID_ACCESSNAME.
        /// </summary>
        /// <param name="id_accessname">Идентификатор категории доступа.</param>
        [HttpDelete("{id_accessname:int}")]
        [ProducesResponseType(typeof(AccessCategory), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult Delete(int id_accessname)
        {
            if (id_accessname < 0)
            {
                return BadRequest("Параметр id_accessname не может быть отрицательным");
            }

            return DataBaseStatusToWebStatusCode(AccessDBController.Delete(id_accessname));
        }

        /// <summary>
        /// Получить список сотрудников (их tabnum), которым назначена указанная
        /// категория доступа. Источник — таблица SS_ACCESSUSER.
        /// </summary>
        /// <param name="id_accessname">Идентификатор категории доступа.</param>
        /// <param name="pageIndex">Номер страницы, начиная с 0.</param>
        /// <param name="pageSize">Размер страницы, от 1 до 1000.</param>
        [HttpGet(nameof(GetPeople))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPeople(int? id_accessname, int pageIndex = 0, int pageSize = 10)
        {
            if (!id_accessname.HasValue || id_accessname.Value < 0)
            {
                return BadRequest("Параметр id_accessname обязателен и не может быть отрицательным");
            }

            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            var request = AccessDBController.GetPeople(id_accessname.Value, pageIndex, pageSize);
            var result = request.Item1;

            if (result.State == State.Successes)
            {
                result.Value = new Pagination(result.Value, pageIndex, pageSize, request.Item2);
            }

            return DataBaseStatusToWebStatusCode(result);
        }
    }
}
