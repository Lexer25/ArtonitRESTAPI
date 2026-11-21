using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Результат поиска сотрудника по tabnum.
    /// </summary>
    public enum TabnumLookupState
    {
        /// <summary>Сотрудник найден.</summary>
        Found,

        /// <summary>Некорректный или пустой tabnum.</summary>
        Invalid,

        /// <summary>Сотрудник не найден.</summary>
        NotFound,

        /// <summary>Найдено несколько сотрудников с одним tabnum.</summary>
        Ambiguous,

        /// <summary>Ошибка при обращении к базе данных.</summary>
        DatabaseError
    }

    /// <summary>
    /// Данные результата поиска сотрудника по tabnum.
    /// </summary>
    public class TabnumLookupResult
    {
        /// <summary>
        /// Состояние поиска.
        /// </summary>
        public TabnumLookupState State { get; init; }

        /// <summary>
        /// Внутренний идентификатор найденного сотрудника.
        /// </summary>
        public int IdPep { get; init; }

        /// <summary>
        /// Нормализованный (обрезанный) tabnum.
        /// </summary>
        public string TabNum { get; init; } = string.Empty;

        /// <summary>
        /// Текст ошибки для ответа клиенту.
        /// </summary>
        public string ErrorMessage { get; init; } = string.Empty;

        /// <summary>
        /// Признак успешного поиска.
        /// </summary>
        public bool IsFound => State == TabnumLookupState.Found;
    }

    /// <summary>
    /// Преобразует внешний табельный номер tabnum во внутренний id_pep.
    /// </summary>
    public static class TabnumResolver
    {
        /// <summary>
        /// Максимальная длина tabnum (совпадает с VARCHAR(50) в таблице PEOPLE).
        /// </summary>
        public const int MaxLength = 50;

        /// <summary>
        /// Находит сотрудника по tabnum и возвращает его id_pep.
        /// </summary>
        public static TabnumLookupResult Resolve(string? tabnum)
        {
            var value = tabnum?.Trim();

            if (string.IsNullOrEmpty(value))
            {
                return new TabnumLookupResult
                {
                    State = TabnumLookupState.Invalid,
                    ErrorMessage = "Параметр tabnum обязателен"
                };
            }

            if (value.Length > MaxLength)
            {
                return new TabnumLookupResult
                {
                    State = TabnumLookupState.Invalid,
                    TabNum = value,
                    ErrorMessage = $"tabnum не должен превышать {MaxLength} символов"
                };
            }

            const string query = "select first 2 p.id_pep from people p where p.tabnum = @tabnum";
            var parameters = new Dictionary<string, object> { ["@tabnum"] = value };
            var result = DatabaseService.GetListParameterized<PersonIdDataBase>(query, parameters);

            if (result.State != State.Successes)
            {
                return new TabnumLookupResult
                {
                    State = TabnumLookupState.DatabaseError,
                    TabNum = value,
                    ErrorMessage = result.ErrorMessage ?? "Ошибка при обращении к базе данных"
                };
            }

            var persons = result.Value as List<PersonIdDataBase> ?? new List<PersonIdDataBase>();

            if (persons.Count == 0)
            {
                return new TabnumLookupResult
                {
                    State = TabnumLookupState.NotFound,
                    TabNum = value,
                    ErrorMessage = $"Сотрудник с tabnum '{value}' не найден"
                };
            }

            if (persons.Count > 1)
            {
                return new TabnumLookupResult
                {
                    State = TabnumLookupState.Ambiguous,
                    TabNum = value,
                    ErrorMessage = $"Найдено несколько сотрудников с tabnum '{value}'. Обратитесь к администратору"
                };
            }

            return new TabnumLookupResult
            {
                State = TabnumLookupState.Found,
                IdPep = persons[0].Id_pep,
                TabNum = value
            };
        }

        /// <summary>
        /// Проверяет, существует ли сотрудник с указанным tabnum.
        /// </summary>
        public static DatabaseResult Exists(string tabnum)
        {
            const string query = "select count(*) from people p where p.tabnum = @tabnum";
            var parameters = new Dictionary<string, object> { ["@tabnum"] = tabnum };
            var result = DatabaseService.GetParameterized<COUNTDataBase>(query, parameters);

            if (result.State != State.Successes) return result;

            result.Value = result.Value is COUNTDataBase count && count.count > 0;
            return result;
        }
    }
}
