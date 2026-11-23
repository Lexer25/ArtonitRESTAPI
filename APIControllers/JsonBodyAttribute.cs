using Microsoft.AspNetCore.Mvc.Filters;
using System.Text;
using System.Text.Json;

namespace ArtonitRESTAPI.APIControllers
{
    /// <summary>
    /// Проверяет, что тело запроса является корректным JSON-объектом, и запоминает
    /// перечень переданных полей: это нужно для PATCH, где отсутствие поля и значение null
    /// означают разное. Пустое тело и неразбираемый JSON прерывают выполнение команды
    /// ответами VALIDATION_ERROR и INVALID_JSON в едином формате ошибок.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class JsonBodyAttribute : Attribute, IAsyncResourceFilter
    {
        /// <summary>
        /// Ключ в HttpContext.Items, под которым сохраняется набор имён полей JSON.
        /// </summary>
        public const string FieldsItemKey = "ArtonitRESTAPI.JsonBodyFields";

        /// <summary>
        /// Читает тело запроса до привязки модели, проверяет JSON и возвращает поток в начало.
        /// </summary>
        public async Task OnResourceExecutionAsync(ResourceExecutingContext context, ResourceExecutionDelegate next)
        {
            var request = context.HttpContext.Request;
            var contentType = request.ContentType;

            if (!string.IsNullOrEmpty(contentType)
                && contentType.IndexOf("json", StringComparison.OrdinalIgnoreCase) < 0)
            {
                // Не JSON — пусть сработает штатная проверка Content-Type (415).
                await next();
                return;
            }

            request.EnableBuffering();

            string body;
            using (var reader = new StreamReader(
                       request.Body,
                       Encoding.UTF8,
                       detectEncodingFromByteOrderMarks: false,
                       bufferSize: 1024,
                       leaveOpen: true))
            {
                body = await reader.ReadToEndAsync();
            }

            request.Body.Position = 0;

            if (string.IsNullOrWhiteSpace(body))
            {
                context.Result = ApiErrorResults.Validation("Тело запроса обязательно");
                return;
            }

            try
            {
                using var document = JsonDocument.Parse(body);

                if (document.RootElement.ValueKind != JsonValueKind.Object)
                {
                    context.Result = ApiErrorResults.JsonObjectExpected();
                    return;
                }

                var fields = new HashSet<string>(StringComparer.Ordinal);
                foreach (var property in document.RootElement.EnumerateObject())
                {
                    fields.Add(property.Name);
                }

                context.HttpContext.Items[FieldsItemKey] = fields;
            }
            catch (JsonException)
            {
                context.Result = ApiErrorResults.InvalidJson();
                return;
            }

            await next();
        }
    }
}
