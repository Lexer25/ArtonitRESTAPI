using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using FirebirdSql.Data.FirebirdClient;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Model;
using System.Data;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Статус операции с набором категорий доступа сотрудника в API v2.
    /// </summary>
    public enum PersonAccessOperationStatus
    {
        /// <summary>Операция выполнена.</summary>
        Success,

        /// <summary>Сотрудник с указанным tabnum не найден.</summary>
        NotFound,

        /// <summary>Категория доступа отсутствует в ACCESSNAME.</summary>
        AccessNameNotFound,

        /// <summary>Ошибка базы данных.</summary>
        DatabaseError
    }

    /// <summary>
    /// Результат чтения набора категорий доступа сотрудника.
    /// </summary>
    public class PersonAccessNamesResult
    {
        /// <summary>
        /// Статус операции.
        /// </summary>
        public PersonAccessOperationStatus Status { get; set; } = PersonAccessOperationStatus.Success;

        /// <summary>
        /// Идентификаторы категорий доступа (для сохранения — итоговый набор).
        /// </summary>
        public List<int> Items { get; set; } = new();

        /// <summary>
        /// Категории из запроса, которых нет в ACCESSNAME.
        /// </summary>
        public List<int> MissingAccessNames { get; set; } = new();

        /// <summary>
        /// Текст ошибки базы данных.
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат сохранения набора категорий доступа сотрудника.
    /// </summary>
    public class PersonAccessNamesSaveResult : PersonAccessNamesResult
    {
    }

    /// <summary>
    /// Операции над сотрудником для REST API v2 (идентификация по tabnum).
    /// </summary>
    public static class PersonV2DBController
    {
        /// <summary>
        /// Идентификатор базы данных, с которой работает API (ID_DB).
        /// </summary>
        private const int DatabaseId = 1;

        /// <summary>
        /// Код ошибки Firebird «нарушение внешнего ключа».
        /// </summary>
        private const int ForeignKeyViolationCode = 335544466;

        /// <summary>
        /// Сколько раз повторить сохранение набора при временном конфликте блокировок.
        /// </summary>
        private const int MaxLockRetries = 5;

        /// <summary>
        /// Пауза между повторами сохранения набора, миллисекунды.
        /// </summary>
        private const int LockRetryDelayMilliseconds = 50;

        /// <summary>
        /// Добавляет сотрудника, при необходимости формируя tabnum как tn_&lt;id_pep&gt;.
        /// </summary>
        public static DatabaseResult Add(PersonV2Post person)
        {
            person.id_org = 1;
            person.id_db = 1;

            // Резервируем id_pep заранее: он нужен и для вставки, и для tabnum по умолчанию.
            var generatedId = DatabaseService.Get<RBDataBase>("select GEN_ID (gen_people_id, 1) from RDB$DATABASE");
            if (generatedId.State != State.Successes || generatedId.Value is not RBDataBase rdb)
            {
                return generatedId;
            }

            person.Id_pep = rdb.Id;

            var tabnum = person.TabNum?.Trim();
            if (string.IsNullOrEmpty(tabnum))
            {
                tabnum = $"tn_{person.Id_pep}";
            }

            var exists = TabnumResolver.Exists(tabnum);
            if (exists.State != State.Successes) return exists;

            if (exists.Value is bool tabnumExists && tabnumExists)
            {
                return new DatabaseResult
                {
                    State = State.Conflict,
                    ErrorMessage = $"Сотрудник с tabnum '{tabnum}' уже существует"
                };
            }

            person.TabNum = tabnum;

            var result = DatabaseService.Create(person);
            if (result.State == State.Successes)
            {
                result.Value = new PersonV2Created { TabNum = tabnum };
            }

            return result;
        }

        /// <summary>
        /// Добавляет сотруднику категорию доступа.
        /// </summary>
        public static DatabaseResult AddAccess(int idPep, int idAccessname)
        {
            var access = new PersonAddAccess(new PersonAddAccessSee
            {
                Id_pep = idPep,
                Id_accessname = idAccessname
            });

            return PersonDBController.AddAccess(access);
        }

        /// <summary>
        /// Обновляет сотрудника по его id_pep.
        /// </summary>
        public static DatabaseResult Update(int idPep, PersonV2Patch person)
        {
            var patch = new PersonPach
            {
                Id_pep = idPep,
                id_db = person.id_db,
                id_org = person.id_org
            };

            return PersonDBController.Update(patch);
        }

        /// <summary>
        /// Удаляет сотрудника по его id_pep.
        /// </summary>
        public static DatabaseResult Delete(int idPep)
        {
            return PersonDBController.Delete(idPep);
        }

        /// <summary>
        /// Изменяет ФИО сотрудника (SURNAME, NAME, PATRONYMIC) по его id_pep.
        /// Пустые имя и отчество допустимы, фамилия обязательна.
        /// </summary>
        /// <param name="idPep">Внутренний идентификатор сотрудника.</param>
        /// <param name="body">Новые значения ФИО.</param>
        public static DatabaseResult UpdateName(int idPep, PersonV2PutSee body)
        {
            var surname = (body.Surname ?? string.Empty).Trim();
            var name = (body.Name ?? string.Empty).Trim();
            var patronymic = (body.Patronymic ?? string.Empty).Trim();

            if (surname.Length == 0)
            {
                return new DatabaseResult
                {
                    State = State.BadSQLRequest,
                    ErrorMessage = "Заполнение Surname обязательно"
                };
            }

            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                using var command = new FbCommand(
                    @"UPDATE PEOPLE
                      SET SURNAME = @surname,
                          NAME = @name,
                          PATRONYMIC = @patronymic,
                          TIME_STAMP = CURRENT_TIMESTAMP
                      WHERE ID_PEP = @idPep AND ID_DB = @idDb",
                    connection);

                command.Parameters.AddWithValue("@surname", surname);
                command.Parameters.AddWithValue("@name", name);
                command.Parameters.AddWithValue("@patronymic", patronymic);
                command.Parameters.AddWithValue("@idPep", idPep);
                command.Parameters.AddWithValue("@idDb", DatabaseId);

                var affected = command.ExecuteNonQuery();

                if (affected <= 0)
                {
                    return new DatabaseResult
                    {
                        State = State.NotFound,
                        ErrorMessage = $"Сотрудник с tabnum '{body.TabNum.Trim()}' не найден"
                    };
                }

                return new DatabaseResult
                {
                    State = State.Successes,
                    Value = affected
                };
            }
            catch (FbException ex)
            {
                LogError(ex, nameof(UpdateName));
                return new DatabaseResult
                {
                    State = State.NullDataBase,
                    ErrorMessage = $"Database error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(UpdateName));
                return new DatabaseResult
                {
                    State = State.NullDataBase,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Возвращает статистику по таблице PEOPLE: всего, активных (ACTIVE = 1)
        /// и неактивных (ACTIVE = 0) сотрудников.
        /// </summary>
        public static DatabaseResult GetCount()
        {
            const string query = @"
                SELECT
                    COUNT(*) AS TOTAL_COUNT,
                    COALESCE(SUM(CASE WHEN p.""ACTIVE"" = 1 THEN 1 ELSE 0 END), 0) AS ACTIVE_COUNT,
                    COALESCE(SUM(CASE WHEN p.""ACTIVE"" = 0 THEN 1 ELSE 0 END), 0) AS INACTIVE_COUNT
                FROM PEOPLE p";

            return DatabaseService.Get<PersonV2Count>(query);
        }

        /// <summary>
        /// Возвращает страницу сотрудников. Фильтр <paramref name="active"/>:
        /// null — все записи, true — ACTIVE = 1, false — ACTIVE = 0.
        /// </summary>
        /// <param name="pageIndex">Номер страницы, начиная с 0.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="active">Фильтр по признаку активности или null для всех записей.</param>
        public static Tuple<DatabaseResult, int> GetList(int pageIndex, int pageSize, bool? active)
        {
            var filter = active.HasValue ? " WHERE p.\"ACTIVE\" = @active" : string.Empty;

            var parameters = new Dictionary<string, object>();
            if (active.HasValue)
            {
                parameters["@active"] = active.Value ? 1 : 0;
            }

            // long защищает от переполнения int при большом pageIndex.
            var skip = (long)pageIndex * pageSize;

            var queryList = $@"
                SELECT FIRST {pageSize} SKIP {skip}
                    p.ID_PEP, p.SURNAME, p.NAME, p.PATRONYMIC, p.TABNUM
                FROM PEOPLE p{filter}
                ORDER BY p.ID_PEP";

            var queryCount = $"SELECT COUNT(*) FROM PEOPLE p{filter}";

            var countResult = DatabaseService.GetParameterized<COUNTDataBase>(queryCount, parameters);
            if (countResult.State != State.Successes || countResult.Value is not COUNTDataBase count)
            {
                return new Tuple<DatabaseResult, int>(countResult, 0);
            }

            var result = DatabaseService.GetListParameterized<PersonGet>(queryList, parameters);
            return new Tuple<DatabaseResult, int>(result, count.count);
        }

        /// <summary>
        /// Возвращает набор категорий доступа сотрудника (таблица SS_ACCESSUSER).
        /// </summary>
        /// <param name="idPep">Внутренний идентификатор сотрудника.</param>
        public static PersonAccessNamesResult GetAccessNames(int idPep)
        {
            var result = new PersonAccessNamesResult();

            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                if (!PersonExists(connection, idPep))
                {
                    result.Status = PersonAccessOperationStatus.NotFound;
                    return result;
                }

                result.Items = ReadPersonAccessNames(connection, null, idPep);
                result.Status = PersonAccessOperationStatus.Success;
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetAccessNames));
                result.Status = PersonAccessOperationStatus.DatabaseError;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Сохраняет набор категорий доступа сотрудника: недостающие связи
        /// добавляются, лишние удаляются. Изменения выполняются одной транзакцией,
        /// строка сотрудника блокируется, поэтому параллельные сохранения не
        /// пересекаются. Строки SS_ACCESSUSER не обновляются (BEFORE UPDATE
        /// trigger SS_ACCESSUSER_BU), а удаляются и вставляются заново.
        /// </summary>
        /// <param name="idPep">Внутренний идентификатор сотрудника.</param>
        /// <param name="accessNames">Полный набор идентификаторов категорий доступа.</param>
        public static PersonAccessNamesSaveResult SaveAccessNames(
            int idPep,
            IReadOnlyCollection<int> accessNames)
        {
            // Набор храним без дубликатов и по возрастанию: дубликаты запрещены
            // уникальным ограничением UNQ_SS_ACCESSUSER, порядок нужен для ответа.
            var requested = accessNames.Distinct().OrderBy(id => id).ToList();

            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return SaveAccessNamesOnce(idPep, requested);
                }
                catch (FbException ex) when (IsLockConflict(ex) && attempt < MaxLockRetries)
                {
                    DatabaseService.Logger?.LogWarning(
                        ex,
                        "Person access set save lock conflict for id_pep {IdPep}, attempt {Attempt}",
                        idPep,
                        attempt);

                    Thread.Sleep(LockRetryDelayMilliseconds * attempt);
                }
                catch (FbException ex)
                {
                    LogError(ex, nameof(SaveAccessNames));

                    return new PersonAccessNamesSaveResult
                    {
                        Status = IsViolation(ex, ForeignKeyViolationCode)
                            ? PersonAccessOperationStatus.AccessNameNotFound
                            : PersonAccessOperationStatus.DatabaseError,
                        ErrorMessage = ex.Message
                    };
                }
                catch (Exception ex)
                {
                    LogError(ex, nameof(SaveAccessNames));

                    return new PersonAccessNamesSaveResult
                    {
                        Status = PersonAccessOperationStatus.DatabaseError,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        /// <summary>
        /// Одна попытка сохранения набора категорий доступа в отдельной транзакции.
        /// Конфликты блокировок не перехватывает: их обрабатывает вызывающий метод.
        /// </summary>
        private static PersonAccessNamesSaveResult SaveAccessNamesOnce(
            int idPep,
            List<int> requested)
        {
            var result = new PersonAccessNamesSaveResult { Items = new List<int>(requested) };
            var requestedSet = new HashSet<int>(requested);

            using var connection = new FbConnection(DatabaseService.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // 1. Блокируем сотрудника: проверки и запись SS_ACCESSUSER идут
            //    в одной транзакции.
            using (var lockCommand = new FbCommand(
                       @"SELECT ID_PEP FROM PEOPLE
                         WHERE ID_PEP = @idPep AND ID_DB = @idDb WITH LOCK",
                       connection,
                       transaction))
            {
                lockCommand.Parameters.AddWithValue("@idPep", idPep);
                lockCommand.Parameters.AddWithValue("@idDb", DatabaseId);

                var value = lockCommand.ExecuteScalar();
                if (value == null || value is DBNull)
                {
                    result.Status = PersonAccessOperationStatus.NotFound;
                    return result;
                }
            }

            // 2. Все категории должны существовать, иначе вставка нарушит
            //    FK_SS_ACCESSUSER_ID_ACCESSNAME.
            var knownIds = ReadAccessNameIds(connection, transaction);
            result.MissingAccessNames = requested.Where(id => !knownIds.Contains(id)).ToList();

            if (result.MissingAccessNames.Count > 0)
            {
                result.Status = PersonAccessOperationStatus.AccessNameNotFound;
                return result;
            }

            // 3. Меняем только отличия от текущего набора: строки SS_ACCESSUSER
            //    нельзя обновлять, а лишние удаления и вставки нагружают триггеры,
            //    которые пересчитывают индексы карт.
            var currentIds = ReadPersonAccessNames(connection, transaction, idPep);

            var toRemove = currentIds
                .Where(id => !requestedSet.Contains(id))
                .OrderBy(id => id)
                .ToList();
            var toAdd = requested.Where(id => !currentIds.Contains(id)).ToList();

            foreach (var idAccessName in toRemove)
            {
                using var deleteCommand = new FbCommand(
                    @"DELETE FROM SS_ACCESSUSER
                      WHERE ID_DB = @idDb AND ID_PEP = @idPep AND ID_ACCESSNAME = @idAccessName",
                    connection,
                    transaction);
                deleteCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                deleteCommand.Parameters.AddWithValue("@idPep", idPep);
                deleteCommand.Parameters.AddWithValue("@idAccessName", idAccessName);
                deleteCommand.ExecuteNonQuery();
            }

            foreach (var idAccessName in toAdd)
            {
                // ID_ACCESSUSER назначается триггером SS_ACCESSUSER_CREATE_ID
                // (генератор GEN_SS_ACCESSUSER_ID), поэтому колонка не передаётся.
                using var insertCommand = new FbCommand(
                    @"INSERT INTO SS_ACCESSUSER (ID_DB, ID_PEP, ID_ACCESSNAME)
                      VALUES (@idDb, @idPep, @idAccessName)",
                    connection,
                    transaction);
                insertCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                insertCommand.Parameters.AddWithValue("@idPep", idPep);
                insertCommand.Parameters.AddWithValue("@idAccessName", idAccessName);
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            result.Status = PersonAccessOperationStatus.Success;
            return result;
        }

        /// <summary>
        /// Существует ли сотрудник с указанным id_pep в базе API.
        /// </summary>
        private static bool PersonExists(FbConnection connection, int idPep)
        {
            using var command = new FbCommand(
                "SELECT COUNT(*) FROM PEOPLE WHERE ID_PEP = @idPep AND ID_DB = @idDb",
                connection);
            command.Parameters.AddWithValue("@idPep", idPep);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        /// <summary>
        /// Идентификаторы всех категорий доступа базы API.
        /// </summary>
        private static HashSet<int> ReadAccessNameIds(
            FbConnection connection,
            FbTransaction? transaction)
        {
            using var command = new FbCommand(
                "SELECT ID_ACCESSNAME FROM ACCESSNAME WHERE ID_DB = @idDb",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

            var ids = new HashSet<int>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    ids.Add(Convert.ToInt32(reader.GetValue(0)));
                }
            }

            return ids;
        }

        /// <summary>
        /// Текущий набор категорий доступа сотрудника, по возрастанию.
        /// </summary>
        private static List<int> ReadPersonAccessNames(
            FbConnection connection,
            FbTransaction? transaction,
            int idPep)
        {
            using var command = new FbCommand(
                @"SELECT DISTINCT ID_ACCESSNAME FROM SS_ACCESSUSER
                  WHERE ID_DB = @idDb AND ID_PEP = @idPep
                  ORDER BY ID_ACCESSNAME",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);
            command.Parameters.AddWithValue("@idPep", idPep);

            var ids = new List<int>();
            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    ids.Add(Convert.ToInt32(reader.GetValue(0)));
                }
            }

            return ids;
        }

        /// <summary>
        /// Проверяет, что ошибка вызвана указанным кодом Firebird.
        /// </summary>
        private static bool IsViolation(FbException exception, int code)
        {
            if (exception.ErrorCode == code) return true;

            foreach (FbError error in exception.Errors)
            {
                if (error.Number == code) return true;
            }

            return false;
        }

        /// <summary>
        /// Проверяет, что ошибка — временный конфликт блокировок (deadlock,
        /// lock conflict, update conflict). Такую операцию имеет смысл повторить.
        /// </summary>
        private static bool IsLockConflict(FbException exception)
        {
            // isc_deadlock, isc_lock_conflict, isc_update_conflict.
            if (IsViolation(exception, 335544336)
                || IsViolation(exception, 335544345)
                || IsViolation(exception, 335544359))
            {
                return true;
            }

            var message = exception.Message ?? string.Empty;

            return message.Contains("deadlock", StringComparison.OrdinalIgnoreCase)
                || message.Contains("lock conflict", StringComparison.OrdinalIgnoreCase)
                || message.Contains("concurrent update", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Пишет ошибку операции с сотрудником в лог сервиса.
        /// </summary>
        private static void LogError(Exception exception, string operation)
        {
            DatabaseService.Logger?.LogError(exception, "Person DB operation {Operation} failed", operation);
        }
    }
}
