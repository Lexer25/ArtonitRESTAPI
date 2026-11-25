using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

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
        /// Получить количество записей в таблице CARD по фильтрам: tabnum,
        /// TIMESTART, TIMEEND, ACTIVE, ID_CARDTYPE и CREATEDAT.
        /// В ответе — всего, активных и неактивных записей после фильтрации.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника.</param>
        /// <param name="timestart">Дата или дата-время начала действия идентификатора.</param>
        /// <param name="timeend">Дата или дата-время окончания действия идентификатора.</param>
        /// <param name="active">Признак активности: 0 или 1 (также true/false, active/inactive, all).</param>
        /// <param name="idCardtype">Тип идентификатора (CARD.ID_CARDTYPE).</param>
        /// <param name="createdAt">Дата или дата-время создания записи (CARD.CREATEDAT).</param>
        [HttpGet(nameof(GetCount))]
        [ProducesResponseType(typeof(IdentifierV2Count), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status500InternalServerError)]
        public IActionResult GetCount(
            [FromQuery(Name = "tabnum")] string? tabnum = null,
            [FromQuery(Name = "timestart")] string? timestart = null,
            [FromQuery(Name = "timeend")] string? timeend = null,
            [FromQuery(Name = "active")] string? active = null,
            [FromQuery(Name = "id_cardtype")] string? idCardtype = null,
            [FromQuery(Name = "createdat")] string? createdAt = null)
        {
            if (!TryParseFilter(
                    tabnum, timestart, timeend, active, idCardtype, createdAt, null,
                    out var filter, out var error))
            {
                return error!;
            }

            return DataBaseStatusToWebStatusCode(IdentifierV2DBController.GetCount(filter));
        }

        /// <summary>
        /// Получить список идентификаторов (с разбиением на страницы) по фильтрам:
        /// tabnum, TIMESTART, TIMEEND, ACTIVE, ID_CARDTYPE и CREATEDAT.
        /// В ответе вместо id_pep возвращается tabnum.
        /// </summary>
        /// <param name="pageIndex">Номер страницы, начиная с 0. По умолчанию 0.</param>
        /// <param name="pageSize">Размер страницы от 1 до 1000. По умолчанию 10.</param>
        /// <param name="identifire">Номер карты (прежний фильтр GetList).</param>
        /// <param name="tabnum">Табельный номер сотрудника.</param>
        /// <param name="timestart">Дата или дата-время начала действия идентификатора.</param>
        /// <param name="timeend">Дата или дата-время окончания действия идентификатора.</param>
        /// <param name="active">Признак активности: 0 или 1 (также true/false, active/inactive, all).</param>
        /// <param name="idCardtype">Тип идентификатора (CARD.ID_CARDTYPE).</param>
        /// <param name="createdAt">Дата или дата-время создания записи (CARD.CREATEDAT).</param>
        [HttpGet(nameof(GetList))]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiError), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetList(
            int pageIndex = 0,
            int pageSize = 10,
            string identifire = "",
            [FromQuery(Name = "tabnum")] string? tabnum = null,
            [FromQuery(Name = "timestart")] string? timestart = null,
            [FromQuery(Name = "timeend")] string? timeend = null,
            [FromQuery(Name = "active")] string? active = null,
            [FromQuery(Name = "id_cardtype")] string? idCardtype = null,
            [FromQuery(Name = "createdat")] string? createdAt = null)
        {
            if (pageIndex < 0) pageIndex = 0;
            if (pageSize <= 0) pageSize = 10;
            if (pageSize > 1000) pageSize = 1000;

            if (!TryParseFilter(
                    tabnum, timestart, timeend, active, idCardtype, createdAt, identifire,
                    out var filter, out var error))
            {
                return error!;
            }

            var request = IdentifierV2DBController.GetAll(pageIndex, pageSize, filter);
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

        /// <summary>
        /// Разбирает и проверяет все необязательные фильтры IdentifierV2.
        /// При ошибке возвращает готовый ответ 400 VALIDATION_ERROR.
        /// </summary>
        private static bool TryParseFilter(
            string? tabnum,
            string? timestart,
            string? timeend,
            string? active,
            string? idCardtype,
            string? createdAt,
            string? idCard,
            out IdentifierV2Filter filter,
            out IActionResult? error)
        {
            filter = new IdentifierV2Filter();
            error = null;

            var normalizedTabnum = tabnum?.Trim();
            if (normalizedTabnum?.Length > TabnumResolver.MaxLength)
            {
                error = ApiErrorResults.Validation(
                    "tabnum", "too_long",
                    $"Параметр tabnum не должен превышать {TabnumResolver.MaxLength} символов");
                return false;
            }

            if (normalizedTabnum?.Length == 0) normalizedTabnum = null;

            if (!TryParseDateFilter(timestart, "timestart", out var timeStart, out error)
                || !TryParseDateFilter(timeend, "timeend", out var timeEnd, out error)
                || !TryParseDateFilter(createdAt, "createdat", out var createdAtFilter, out error))
            {
                return false;
            }

            if (!TryParseActiveFilter(active, out var activeFilter, out var activeError))
            {
                error = ApiErrorResults.Validation("active", "invalid_value", activeError);
                return false;
            }

            int? cardtype = null;
            if (!string.IsNullOrWhiteSpace(idCardtype))
            {
                if (!int.TryParse(
                        idCardtype.Trim(),
                        NumberStyles.Integer,
                        CultureInfo.InvariantCulture,
                        out var parsed)
                    || parsed < 0)
                {
                    error = ApiErrorResults.Validation(
                        "id_cardtype", "invalid_value",
                        "Параметр id_cardtype должен быть целым неотрицательным числом");
                    return false;
                }

                cardtype = parsed;
            }

            filter = new IdentifierV2Filter
            {
                IdCard = string.IsNullOrWhiteSpace(idCard) ? null : idCard.Trim(),
                TabNum = normalizedTabnum,
                IdCardtype = cardtype,
                Active = activeFilter,
                TimeStart = timeStart,
                TimeEnd = timeEnd,
                CreatedAt = createdAtFilter
            };

            return true;
        }

        /// <summary>
        /// Разбирает фильтр по колонке-дате. Значение без времени охватывает
        /// весь день, значение с временем сравнивается точно.
        /// </summary>
        private static bool TryParseDateFilter(
            string? value,
            string field,
            out IdentifierV2DateFilter? filter,
            out IActionResult? error)
        {
            filter = null;
            error = null;

            if (string.IsNullOrWhiteSpace(value)) return true;

            var text = value.Trim();

            if (!DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed)
                && !DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
            {
                error = ApiErrorResults.Validation(
                    field, "invalid_value",
                    $"Не удалось разобрать {field}. Ожидается дата или дата-время, например 2026-11-25 или 2026-11-25 18:00:00");
                return false;
            }

            // Если время не указано явно, фильтр охватывает весь день.
            var hasTime = text.Contains(':') || text.Contains('T');

            filter = new IdentifierV2DateFilter
            {
                Value = parsed,
                DateOnly = !hasTime && parsed.TimeOfDay == TimeSpan.Zero
            };

            return true;
        }

        /// <summary>
        /// Разбирает фильтр активности. Пустое значение и all означают «все записи».
        /// </summary>
        private static bool TryParseActiveFilter(string? value, out bool? active, out string error)
        {
            active = null;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(value)) return true;

            switch (value.Trim().ToLowerInvariant())
            {
                case "all":
                case "any":
                    return true;

                case "1":
                case "true":
                case "yes":
                case "active":
                    active = true;
                    return true;

                case "0":
                case "false":
                case "no":
                case "inactive":
                    active = false;
                    return true;

                default:
                    error = "Параметр active должен быть одним из значений: all, active, inactive, 1, 0, true, false";
                    return false;
            }
        }
    }
}
