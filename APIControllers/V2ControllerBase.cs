using ArtonitRESTAPI.DBControllers;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// Общая логика контроллеров REST API v2: обработка ошибок tabnum и периода дат.
    /// </summary>
    public abstract class V2ControllerBase : BaseAPIController
    {
        /// <summary>
        /// Преобразует результат поиска tabnum в HTTP-ответ с подходящим кодом.
        /// </summary>
        protected IActionResult TabnumLookupError(TabnumLookupResult lookup)
        {
            return lookup.State switch
            {
                TabnumLookupState.Invalid => BadRequest(lookup.ErrorMessage),
                TabnumLookupState.NotFound => NotFound(lookup.ErrorMessage),
                TabnumLookupState.Ambiguous => Conflict(lookup.ErrorMessage),
                _ => StatusCode(StatusCodes.Status500InternalServerError, lookup.ErrorMessage)
            };
        }

        /// <summary>
        /// Разбирает и проверяет период fromDate/toDate.
        /// </summary>
        protected static bool TryParsePeriod(
            string? fromDate,
            string? toDate,
            out DateTime from,
            out DateTime to,
            out string error)
        {
            from = default;
            to = default;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(fromDate) || string.IsNullOrWhiteSpace(toDate))
            {
                error = "Параметры fromDate и toDate обязательны";
                return false;
            }

            if (!TryParseDate(fromDate, out from) || !TryParseDate(toDate, out to))
            {
                error = "Не удалось разобрать fromDate/toDate. Ожидается дата и время, например 2026-01-31 18:00:00";
                return false;
            }

            if (from > to)
            {
                error = "fromDate не может быть позже toDate";
                return false;
            }

            return true;
        }

        private static bool TryParseDate(string value, out DateTime result)
        {
            return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out result)
                || DateTime.TryParse(value, CultureInfo.CurrentCulture, DateTimeStyles.None, out result);
        }
    }
}
