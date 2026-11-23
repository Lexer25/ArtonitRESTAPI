using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using FirebirdSql.Data.FirebirdClient;
using Microsoft.Extensions.Logging;
using OpenAPIArtonit.DB;
using System.Data;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Операции над категориями доступа (таблицы ACCESSNAME и SS_ACCESSUSER,
    /// Firebird 1.5, диалект 3). API работает с одной базой: ID_DB = 1.
    /// </summary>
    public static class AccessDBController
    {
        /// <summary>
        /// Идентификатор базы данных, с которой работает API (ID_DB).
        /// </summary>
        public const int DatabaseId = 1;

        /// <summary>
        /// Максимальная длина названия категории (ACCESSNAME.NAME — VARCHAR(50)).
        /// </summary>
        public const int MaxNameLength = 50;

        /// <summary>
        /// Системная категория «вездеход»: удаление запрещено триггером ACCESSNAME_BD.
        /// </summary>
        private const int ProtectedAccessNameId = 1;

        /// <summary>
        /// Возвращает постраничный список категорий доступа.
        /// </summary>
        public static Tuple<DatabaseResult, int> GetAll(int pageIndex, int pageSize, string? accessName)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                var where = " where an.id_db = @idDb";
                var parameters = new Dictionary<string, object> { ["@idDb"] = DatabaseId };

                if (!string.IsNullOrWhiteSpace(accessName))
                {
                    where += " and an.name containing @name";
                    parameters["@name"] = accessName.Trim();
                }

                var total = 0;
                using (var countCommand = new FbCommand($"select count(*) from accessname an{where}", connection))
                {
                    AddParameters(countCommand, parameters);
                    total = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                var skip = Math.Max(0L, (long)pageIndex * pageSize);
                var query = $@"select first {pageSize} skip {skip}
                    an.id_accessname, an.name
                    from accessname an{where}
                    order by an.name, an.id_accessname";

                var categories = new List<AccessList>();
                using (var command = new FbCommand(query, connection))
                {
                    AddParameters(command, parameters);
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        categories.Add(new AccessList
                        {
                            Id_accessname = reader.GetInt32(0),
                            Name = reader.GetString(1)
                        });
                    }
                }

                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.Successes, Value = categories },
                    total);
            }
            catch (FbException ex)
            {
                LogError(ex, nameof(GetAll));
                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.BadSQLRequest, ErrorMessage = $"Database error: {ex.Message}" },
                    0);
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetAll));
                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.NullDataBase, ErrorMessage = $"Unexpected error: {ex.Message}" },
                    0);
            }
        }

        /// <summary>
        /// Добавляет категорию доступа. Идентификатор назначается генератором
        /// GEN_ACCESSNAME_ID; название обрезается от пробелов.
        /// </summary>
        public static DatabaseResult Add(string name)
        {
            try
            {
                var normalizedName = name.Trim();

                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                int idAccessName;
                using (var idCommand = new FbCommand(
                    "select gen_id(gen_accessname_id, 1) from rdb$database", connection, transaction))
                {
                    idAccessName = Convert.ToInt32(idCommand.ExecuteScalar());
                }

                using (var insertCommand = new FbCommand(
                    "insert into accessname (id_accessname, id_db, name) values (@id, @idDb, @name)",
                    connection, transaction))
                {
                    insertCommand.Parameters.AddWithValue("@id", idAccessName);
                    insertCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    insertCommand.Parameters.AddWithValue("@name", normalizedName);
                    insertCommand.ExecuteNonQuery();
                }

                transaction.Commit();

                return new DatabaseResult
                {
                    State = State.Successes,
                    Value = new AccessCategory { Id_accessname = idAccessName, Name = normalizedName }
                };
            }
            catch (FbException ex)
            {
                LogError(ex, nameof(Add));
                return new DatabaseResult
                {
                    State = State.BadSQLRequest,
                    ErrorMessage = $"Database error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(Add));
                return new DatabaseResult
                {
                    State = State.NullDataBase,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Удаляет категорию доступа и её правила доступа. Связи с сотрудниками
        /// (SS_ACCESSUSER) удаляются каскадно внешним ключом базы данных.
        /// </summary>
        public static DatabaseResult Delete(int idAccessName)
        {
            if (idAccessName == ProtectedAccessNameId)
            {
                return new DatabaseResult
                {
                    State = State.Conflict,
                    ErrorMessage = "Категорию доступа с id_accessname=1 («вездеход») удалить нельзя"
                };
            }

            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                string? name;
                using (var selectCommand = new FbCommand(
                    "select name from accessname where id_db = @idDb and id_accessname = @id",
                    connection, transaction))
                {
                    selectCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    selectCommand.Parameters.AddWithValue("@id", idAccessName);

                    var value = selectCommand.ExecuteScalar();
                    if (value == null || value is DBNull)
                    {
                        return new DatabaseResult
                        {
                            State = State.NotFound,
                            ErrorMessage = $"Категория доступа с id_accessname={idAccessName} не найдена"
                        };
                    }

                    name = Convert.ToString(value) ?? string.Empty;
                }

                // В ACCESS нет внешнего ключа на ACCESSNAME, поэтому правила доступа
                // удаляем явно — иначе останутся «осиротевшие» строки.
                using (var deleteAccessCommand = new FbCommand(
                    "delete from access where id_db = @idDb and id_accessname = @id",
                    connection, transaction))
                {
                    deleteAccessCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    deleteAccessCommand.Parameters.AddWithValue("@id", idAccessName);
                    deleteAccessCommand.ExecuteNonQuery();
                }

                int deleted;
                using (var deleteCommand = new FbCommand(
                    "delete from accessname where id_db = @idDb and id_accessname = @id",
                    connection, transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    deleteCommand.Parameters.AddWithValue("@id", idAccessName);
                    deleted = deleteCommand.ExecuteNonQuery();
                }

                if (deleted == 0)
                {
                    return new DatabaseResult
                    {
                        State = State.NotFound,
                        ErrorMessage = $"Категория доступа с id_accessname={idAccessName} не найдена"
                    };
                }

                transaction.Commit();

                return new DatabaseResult
                {
                    State = State.Successes,
                    Value = new AccessCategory { Id_accessname = idAccessName, Name = name }
                };
            }
            catch (FbException ex)
            {
                LogError(ex, nameof(Delete));

                if (IsIntegrityViolation(ex))
                {
                    return new DatabaseResult
                    {
                        State = State.Conflict,
                        ErrorMessage = $"Категорию доступа с id_accessname={idAccessName} удалить нельзя: {ex.Message}"
                    };
                }

                return new DatabaseResult
                {
                    State = State.BadSQLRequest,
                    ErrorMessage = $"Database error: {ex.Message}"
                };
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(Delete));
                return new DatabaseResult
                {
                    State = State.NullDataBase,
                    ErrorMessage = $"Unexpected error: {ex.Message}"
                };
            }
        }

        /// <summary>
        /// Возвращает постраничный список tabnum сотрудников, которым назначена
        /// указанная категория (SS_ACCESSUSER + PEOPLE).
        /// </summary>
        public static Tuple<DatabaseResult, int> GetPeople(int idAccessName, int pageIndex, int pageSize)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                using (var categoryCommand = new FbCommand(
                    "select name from accessname where id_db = @idDb and id_accessname = @id",
                    connection))
                {
                    categoryCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    categoryCommand.Parameters.AddWithValue("@id", idAccessName);

                    var value = categoryCommand.ExecuteScalar();
                    if (value == null || value is DBNull)
                    {
                        return new Tuple<DatabaseResult, int>(
                            new DatabaseResult
                            {
                                State = State.NotFound,
                                ErrorMessage = $"Категория доступа с id_accessname={idAccessName} не найдена"
                            },
                            0);
                    }
                }

                const string fromSql = @"
                    from ss_accessuser ss
                    join people p on p.id_pep = ss.id_pep and p.id_db = ss.id_db
                    where ss.id_db = @idDb
                      and ss.id_accessname = @id
                      and p.tabnum is not null
                      and p.tabnum <> ''";

                var parameters = new Dictionary<string, object>
                {
                    ["@idDb"] = DatabaseId,
                    ["@id"] = idAccessName
                };

                var total = 0;
                using (var countCommand = new FbCommand($"select count(distinct p.tabnum) {fromSql}", connection))
                {
                    AddParameters(countCommand, parameters);
                    total = Convert.ToInt32(countCommand.ExecuteScalar());
                }

                var skip = Math.Max(0L, (long)pageIndex * pageSize);
                var query = $@"select first {pageSize} skip {skip} distinct p.tabnum {fromSql}
                    order by p.tabnum";

                var people = new List<AccessPerson>();
                using (var command = new FbCommand(query, connection))
                {
                    AddParameters(command, parameters);
                    using var reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        people.Add(new AccessPerson { TabNum = reader.GetString(0) });
                    }
                }

                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.Successes, Value = people },
                    total);
            }
            catch (FbException ex)
            {
                LogError(ex, nameof(GetPeople));
                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.BadSQLRequest, ErrorMessage = $"Database error: {ex.Message}" },
                    0);
            }
            catch (Exception ex)
            {
                LogError(ex, nameof(GetPeople));
                return new Tuple<DatabaseResult, int>(
                    new DatabaseResult { State = State.NullDataBase, ErrorMessage = $"Unexpected error: {ex.Message}" },
                    0);
            }
        }

        private static void AddParameters(FbCommand command, IReadOnlyDictionary<string, object> parameters)
        {
            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Key, parameter.Value);
            }
        }

        private static bool IsIntegrityViolation(FbException exception)
        {
            var message = exception.Message ?? string.Empty;

            return message.Contains("DELETENOTALLOWED", StringComparison.OrdinalIgnoreCase)
                || message.Contains("FOREIGN KEY", StringComparison.OrdinalIgnoreCase)
                || message.Contains("violation of FOREIGN KEY", StringComparison.OrdinalIgnoreCase);
        }

        private static void LogError(Exception exception, string operation)
        {
            DatabaseService.Logger?.LogError(exception, "Access DB error in {Operation}", operation);
        }
    }
}
