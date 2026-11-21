using ArtonitRESTAPI.DBControllers;
using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using Microsoft.AspNetCore.Mvc;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// REST API v2 для событий. Сотрудник задаётся табельным номером tabnum.
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "v2")]
    [Route("api/v2/Event")]
    public class EventV2Controller : V2ControllerBase
    {
        /// <summary>
        /// Получить события сотрудника по названию группы устройств за период.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника</param>
        /// <param name="fromDate">Дата и время начала периода</param>
        /// <param name="toDate">Дата и время окончания периода</param>
        /// <param name="deviceGroupName">Название группы устройств</param>
        [HttpGet("passIdDg")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPassIdDg(
            [FromQuery(Name = "tabnum")] string? tabnum,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? deviceGroupName)
        {
            if (!TryParsePeriod(fromDate, toDate, out var from, out var to, out var error))
            {
                return BadRequest(error);
            }

            if (string.IsNullOrWhiteSpace(deviceGroupName))
            {
                return BadRequest("Параметр deviceGroupName обязателен");
            }

            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = EventV2DBController.GetPassIdDg(lookup.IdPep, from, to, deviceGroupName);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            return Ok(new { context = ToV2(result.Value as List<EventResponse>, lookup.TabNum) });
        }

        /// <summary>
        /// Получить события сотрудника по названию точки прохода за период.
        /// </summary>
        /// <param name="tabnum">Табельный номер сотрудника</param>
        /// <param name="fromDate">Дата и время начала периода</param>
        /// <param name="toDate">Дата и время окончания периода</param>
        /// <param name="accessPointName">Название точки прохода</param>
        [HttpGet("passIdAp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPassIdAp(
            [FromQuery(Name = "tabnum")] string? tabnum,
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? accessPointName)
        {
            if (!TryParsePeriod(fromDate, toDate, out var from, out var to, out var error))
            {
                return BadRequest(error);
            }

            if (string.IsNullOrWhiteSpace(accessPointName))
            {
                return BadRequest("Параметр accessPointName обязателен");
            }

            var lookup = TabnumResolver.Resolve(tabnum);
            if (!lookup.IsFound) return TabnumLookupError(lookup);

            var result = EventV2DBController.GetPassIdAp(lookup.IdPep, from, to, accessPointName);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            return Ok(new { context = ToV2(result.Value as List<EventResponse>, lookup.TabNum) });
        }

        /// <summary>
        /// Получить события за период по названию группы устройств.
        /// </summary>
        [HttpGet("passAllDG")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPassAllDG(
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? deviceGroupName)
        {
            if (!TryParsePeriod(fromDate, toDate, out var from, out var to, out var error))
            {
                return BadRequest(error);
            }

            if (string.IsNullOrWhiteSpace(deviceGroupName))
            {
                return BadRequest("Параметр deviceGroupName обязателен");
            }

            var result = EventV2DBController.GetPassAllDg(from, to, deviceGroupName);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            return Ok(new { context = ToV2(result.Value as List<EventV2Row>) });
        }

        /// <summary>
        /// Получить события за период по названию точки прохода.
        /// </summary>
        [HttpGet("passAllAp")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public IActionResult GetPassAllAp(
            [FromQuery] string? fromDate,
            [FromQuery] string? toDate,
            [FromQuery] string? accessPointName)
        {
            if (!TryParsePeriod(fromDate, toDate, out var from, out var to, out var error))
            {
                return BadRequest(error);
            }

            if (string.IsNullOrWhiteSpace(accessPointName))
            {
                return BadRequest("Параметр accessPointName обязателен");
            }

            var result = EventV2DBController.GetPassAllAp(from, to, accessPointName);
            if (result.State != State.Successes) return DataBaseStatusToWebStatusCode(result);

            return Ok(new { context = ToV2(result.Value as List<EventV2Row>) });
        }

        private static List<EventV2Response> ToV2(List<EventResponse>? events, string tabnum)
        {
            return events == null
                ? new List<EventV2Response>()
                : events.Select(e => new EventV2Response { TabNum = tabnum, Timestamp = e.Timestamp }).ToList();
        }

        private static List<EventV2Response> ToV2(List<EventV2Row>? events)
        {
            return events == null
                ? new List<EventV2Response>()
                : events.Select(e => new EventV2Response { TabNum = e.TabNum, Timestamp = e.Timestamp }).ToList();
        }
    }
}
