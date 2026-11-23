using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using FirebirdSql.Data.FirebirdClient;
using OpenAPIArtonit.DB;
using System.Data;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Поле сортировки списка организаций.
    /// </summary>
    public enum OrganizationSortField
    {
        /// <summary>По названию.</summary>
        Name,

        /// <summary>По коду организации.</summary>
        DivCode,

        /// <summary>По дате создания.</summary>
        CreatedAt
    }

    /// <summary>
    /// Состояние операции над организацией.
    /// </summary>
    public enum OrganizationOperationStatus
    {
        /// <summary>Операция выполнена.</summary>
        Success,

        /// <summary>Организация не найдена.</summary>
        NotFound,

        /// <summary>divcode уже существует (без учёта регистра).</summary>
        DuplicateDivCode,

        /// <summary>Родительская организация не существует.</summary>
        ParentNotFound,

        /// <summary>Ссылка parent образует цикл.</summary>
        Cycle,

        /// <summary>У организации есть дочерние организации.</summary>
        HasChildren,

        /// <summary>В организации есть активные сотрудники.</summary>
        HasPeople,

        /// <summary>Есть и дочерние организации, и активные сотрудники.</summary>
        HasChildrenAndPeople,

        /// <summary>Корневую организацию удалить нельзя.</summary>
        RootOrganization,

        /// <summary>Категория доступа не существует.</summary>
        AccessNameNotFound,

        /// <summary>В базе отсутствует таблица связей SS_ACCESSORG.</summary>
        AccessOrgUnavailable,

        /// <summary>Ошибка базы данных.</summary>
        DatabaseError
    }

    /// <summary>
    /// Запись таблицы ORGANIZATION в терминах REST API.
    /// </summary>
    public sealed class OrganizationRecord
    {
        /// <summary>Идентификатор организации (ID_ORG).</summary>
        public int IdOrg { get; set; }

        /// <summary>Идентификатор родителя (ID_PARENT).</summary>
        public int IdParent { get; set; }

        /// <summary>Код организации (DIVCODE).</summary>
        public string DivCode { get; set; } = string.Empty;

        /// <summary>Название организации (NAME).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Флаг организации (FLAG).</summary>
        public int Flag { get; set; }

        /// <summary>Код родительской организации (null для корневых).</summary>
        public string? ParentDivCode { get; set; }

        /// <summary>Дата создания.</summary>
        public DateTime? CreatedAt { get; set; }

        /// <summary>Дата последнего изменения.</summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Преобразует запись БД в модель ответа.
        /// </summary>
        public OrganizationGet ToApi()
        {
            return new OrganizationGet
            {
                Id = IdOrg,
                DivCode = DivCode,
                Name = Name,
                Parent = ParentDivCode,
                Flag = Flag,
                CreatedAt = CreatedAt,
                UpdatedAt = UpdatedAt
            };
        }
    }

    /// <summary>
    /// Результат поиска одной организации.
    /// </summary>
    public sealed class OrganizationLookupResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Найденная запись.</summary>
        public OrganizationRecord? Record { get; set; }

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат выборки списка организаций.
    /// </summary>
    public sealed class OrganizationListResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Записи текущей страницы.</summary>
        public List<OrganizationRecord> Items { get; set; } = new List<OrganizationRecord>();

        /// <summary>Общее число записей, удовлетворяющих фильтру.</summary>
        public int Total { get; set; }

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат выборки категорий доступа организации.
    /// </summary>
    public sealed class OrganizationAccessNamesResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Идентификаторы категорий доступа (ID_ACCESSNAME).</summary>
        public List<int> Items { get; set; } = new List<int>();

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат сохранения набора категорий доступа организации.
    /// </summary>
    public sealed class OrganizationAccessNamesSaveResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Сохранённый набор идентификаторов категорий доступа по возрастанию.</summary>
        public List<int> Items { get; set; } = new List<int>();

        /// <summary>Идентификаторы категорий доступа, которых нет в ACCESSNAME.</summary>
        public List<int> MissingAccessNames { get; set; } = new List<int>();

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат создания или изменения организации.
    /// </summary>
    public sealed class OrganizationMutationResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Актуальная запись организации.</summary>
        public OrganizationRecord? Record { get; set; }

        /// <summary>Код родительской организации, по которому не найдена запись.</summary>
        public string? ParentDivCode { get; set; }

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Результат удаления организации.
    /// </summary>
    public sealed class OrganizationDeleteResult
    {
        /// <summary>Состояние операции.</summary>
        public OrganizationOperationStatus Status { get; set; }

        /// <summary>Код удаляемой организации.</summary>
        public string DivCode { get; set; } = string.Empty;

        /// <summary>Коды дочерних организаций.</summary>
        public List<string> Children { get; set; } = new List<string>();

        /// <summary>Табельные номера активных сотрудников.</summary>
        public List<string> People { get; set; } = new List<string>();

        /// <summary>Текст ошибки БД для журнала.</summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Операции над организациями поверх таблицы ORGANIZATION (Firebird 1.5, диалект 3).
    /// Все изменения выполняются в транзакции; удаление и изменение — под блокировкой строки.
    /// </summary>
    public static class OrganizationDBController
    {
        /// <summary>
        /// Идентификатор базы данных, с которой работает API (ID_DB).
        /// </summary>
        public const int DatabaseId = 1;

        /// <summary>
        /// Идентификатор корневой организации (ID_ORG = 1, DIVCODE = 'dv_All').
        /// </summary>
        public const int RootOrganizationId = 1;

        private const int UniqueViolationCode = 335544665;
        private const int ForeignKeyViolationCode = 335544466;
        private const int MaxHierarchyDepth = 10000;
        private const int MaxLockRetries = 5;
        private const int LockRetryDelayMilliseconds = 50;

        private static readonly Lazy<bool> HasDateCreate = new Lazy<bool>(DetectDateCreateColumn);

        private static readonly Lazy<bool> HasAccessOrgTable = new Lazy<bool>(DetectAccessOrgTable);

        /// <summary>
        /// Находит организацию по коду без учёта регистра.
        /// </summary>
        public static OrganizationLookupResult Find(string divCode)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                var sql = BuildSelect("WHERE o.ID_DB = @idDb AND UPPER(o.DIVCODE) = UPPER(@divcode) ORDER BY o.ID_ORG");

                using var command = new FbCommand(sql, connection);
                command.Parameters.AddWithValue("@idDb", DatabaseId);
                command.Parameters.AddWithValue("@divcode", divCode.Trim());

                using var reader = command.ExecuteReader();
                if (!reader.Read())
                {
                    return new OrganizationLookupResult { Status = OrganizationOperationStatus.NotFound };
                }

                return new OrganizationLookupResult
                {
                    Status = OrganizationOperationStatus.Success,
                    Record = ReadRecord(reader)
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new OrganizationLookupResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Возвращает постраничный список организаций с фильтрами.
        /// </summary>
        /// <param name="parentSpecified">Передан ли фильтр по родителю.</param>
        /// <param name="parentDivCode">Код родителя; пустая строка — только корневые организации.</param>
        /// <param name="nameContains">Подстрока поиска по названию без учёта регистра.</param>
        /// <param name="page">Номер страницы, начиная с 1.</param>
        /// <param name="pageSize">Размер страницы.</param>
        /// <param name="sort">Поле сортировки.</param>
        /// <param name="descending">Признак сортировки по убыванию.</param>
        public static OrganizationListResult GetList(
            bool parentSpecified,
            string? parentDivCode,
            string? nameContains,
            int page,
            int pageSize,
            OrganizationSortField sort,
            bool descending)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                var where = new List<string> { "o.ID_DB = @idDb" };
                var parameters = new List<(string Name, object Value)>
                {
                    ("@idDb", DatabaseId)
                };

                if (parentSpecified)
                {
                    if (string.IsNullOrWhiteSpace(parentDivCode))
                    {
                        where.Add("o.ID_PARENT = @idParent");
                        parameters.Add(("@idParent", RootOrganizationId));
                    }
                    else
                    {
                        using var parentCommand = new FbCommand(
                            "SELECT ID_ORG FROM ORGANIZATION WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode) ORDER BY ID_ORG",
                            connection);
                        parentCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                        parentCommand.Parameters.AddWithValue("@divcode", parentDivCode.Trim());

                        var parentId = parentCommand.ExecuteScalar();
                        if (parentId == null || parentId is DBNull)
                        {
                            // Родитель не найден: пустой список, как и любой фильтр без совпадений.
                            return new OrganizationListResult
                            {
                                Status = OrganizationOperationStatus.Success,
                                Items = new List<OrganizationRecord>(),
                                Total = 0
                            };
                        }

                        where.Add("o.ID_PARENT = @idParent");
                        parameters.Add(("@idParent", Convert.ToInt32(parentId)));
                    }
                }

                if (!string.IsNullOrWhiteSpace(nameContains))
                {
                    where.Add(@"UPPER(o.NAME) LIKE @namePattern ESCAPE '\'");
                    parameters.Add(("@namePattern", "%" + EscapeLike(nameContains.Trim().ToUpperInvariant()) + "%"));
                }

                var whereSql = "WHERE " + string.Join(" AND ", where);
                var orderSql = BuildOrderBy(sort, descending);
                var skip = (long)(page - 1) * pageSize;

                var total = CountOrganizations(connection, whereSql, parameters);

                var sql = BuildSelect(whereSql, $"ORDER BY {orderSql}", pageSize, skip);

                using var command = new FbCommand(sql, connection);
                foreach (var parameter in parameters)
                {
                    command.Parameters.AddWithValue(parameter.Name, parameter.Value);
                }

                var items = new List<OrganizationRecord>();
                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        items.Add(ReadRecord(reader));
                    }
                }

                return new OrganizationListResult
                {
                    Status = OrganizationOperationStatus.Success,
                    Items = items,
                    Total = total
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new OrganizationListResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Возвращает идентификаторы категорий доступа, привязанных к организации
        /// (таблица SS_ACCESSORG), по её коду divcode.
        /// </summary>
        /// <param name="divCode">Код организации.</param>
        public static OrganizationAccessNamesResult GetAccessNames(string divCode)
        {
            var result = new OrganizationAccessNamesResult();

            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                int idOrg;
                using (var orgCommand = new FbCommand(
                           "SELECT ID_ORG FROM ORGANIZATION WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode) ORDER BY ID_ORG",
                           connection))
                {
                    orgCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    orgCommand.Parameters.AddWithValue("@divcode", divCode.Trim());

                    var value = orgCommand.ExecuteScalar();
                    if (value == null || value is DBNull)
                    {
                        result.Status = OrganizationOperationStatus.NotFound;
                        return result;
                    }

                    idOrg = Convert.ToInt32(value);
                }

                // В старых базах (шаблон EMPTY.gdb) таблицы связей может не быть:
                // тогда связей нет и список пуст.
                if (!HasAccessOrgTable.Value)
                {
                    result.Status = OrganizationOperationStatus.Success;
                    return result;
                }

                using var command = new FbCommand(
                    @"SELECT DISTINCT ID_ACCESSNAME FROM SS_ACCESSORG
                      WHERE ID_DB = @idDb AND ID_ORG = @idOrg
                      ORDER BY ID_ACCESSNAME",
                    connection);
                command.Parameters.AddWithValue("@idDb", DatabaseId);
                command.Parameters.AddWithValue("@idOrg", idOrg);

                using var reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (!reader.IsDBNull(0))
                    {
                        result.Items.Add(Convert.ToInt32(reader.GetValue(0)));
                    }
                }

                result.Status = OrganizationOperationStatus.Success;
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);
                result.Status = OrganizationOperationStatus.DatabaseError;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        /// <summary>
        /// Сохраняет набор категорий доступа организации: недостающие связи добавляются,
        /// лишние удаляются. Изменения выполняются одной транзакцией, строка организации
        /// блокируется, поэтому параллельные сохранения не пересекаются.
        /// </summary>
        /// <param name="divCode">Код организации.</param>
        /// <param name="accessNames">Полный набор идентификаторов категорий доступа.</param>
        public static OrganizationAccessNamesSaveResult SaveAccessNames(
            string divCode,
            IReadOnlyCollection<int> accessNames)
        {
            // Набор храним без дубликатов и по возрастанию: дубликаты запрещены
            // уникальным ограничением UNQ1_SS_ACCESSORG, а порядок нужен для ответа.
            var requested = accessNames.Distinct().OrderBy(id => id).ToList();

            var result = new OrganizationAccessNamesSaveResult { Items = new List<int>(requested) };

            // Блокировка строки организации в READ COMMITTED работает в режиме no wait:
            // при одновременных сохранениях Firebird сообщает о конфликте блокировок.
            // Конфликт кратковременный, поэтому операцию повторяем: последний запрос
            // применит свой набор целиком.
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return SaveAccessNamesOnce(divCode, requested);
                }
                catch (FbException ex) when (IsLockConflict(ex) && attempt < MaxLockRetries)
                {
                    DatabaseService.Logger?.LogWarning(
                        ex,
                        "Access set save lock conflict for divcode {DivCode}, attempt {Attempt}",
                        divCode,
                        attempt);

                    Thread.Sleep(LockRetryDelayMilliseconds * attempt);
                }
                catch (FbException ex)
                {
                    LogError(ex);

                    // Категорию удалили между проверкой и вставкой.
                    if (IsViolation(ex, ForeignKeyViolationCode))
                    {
                        result.Status = OrganizationOperationStatus.AccessNameNotFound;
                        result.ErrorMessage = ex.Message;
                        return result;
                    }

                    result.Status = OrganizationOperationStatus.DatabaseError;
                    result.ErrorMessage = ex.Message;
                    return result;
                }
                catch (Exception ex)
                {
                    LogError(ex);
                    result.Status = OrganizationOperationStatus.DatabaseError;
                    result.ErrorMessage = ex.Message;
                    return result;
                }
            }
        }

        /// <summary>
        /// Одна попытка сохранения набора категорий доступа в отдельной транзакции.
        /// Конфликты блокировок не перехватывает: их обрабатывает вызывающий метод.
        /// </summary>
        private static OrganizationAccessNamesSaveResult SaveAccessNamesOnce(
            string divCode,
            List<int> requested)
        {
            var result = new OrganizationAccessNamesSaveResult { Items = new List<int>(requested) };
            var requestedSet = new HashSet<int>(requested);

            using var connection = new FbConnection(DatabaseService.ConnectionString);
            connection.Open();
            using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

            // 1. Блокируем организацию: проверки и запись SS_ACCESSORG идут в одной транзакции.
            int idOrg;
            using (var lockCommand = new FbCommand(
                       @"SELECT ID_ORG FROM ORGANIZATION
                         WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode) WITH LOCK",
                       connection,
                       transaction))
            {
                lockCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                lockCommand.Parameters.AddWithValue("@divcode", divCode.Trim());

                var value = lockCommand.ExecuteScalar();
                if (value == null || value is DBNull)
                {
                    result.Status = OrganizationOperationStatus.NotFound;
                    return result;
                }

                idOrg = Convert.ToInt32(value);
            }

            // В старых базах (шаблон EMPTY.gdb) таблицы связей нет: сохранять некуда.
            if (!HasAccessOrgTable.Value)
            {
                result.Status = OrganizationOperationStatus.AccessOrgUnavailable;
                return result;
            }

            // 2. Все категории должны существовать, иначе вставка нарушит FK_SS_ACCESSORG_1.
            var knownIds = ReadAccessNameIds(connection, transaction);
            result.MissingAccessNames = requested.Where(id => !knownIds.Contains(id)).ToList();
            if (result.MissingAccessNames.Count > 0)
            {
                result.Status = OrganizationOperationStatus.AccessNameNotFound;
                return result;
            }

            // 3. Меняем только отличия от текущего набора: строки SS_ACCESSORG нельзя
            // обновлять (BEFORE UPDATE trigger SS_ACCESSORG_BU), а удаление и повторная
            // вставка совпадающих связей без необходимости нагружают базу.
            var currentIds = ReadOrganizationAccessNames(connection, transaction, idOrg);

            var toRemove = currentIds
                .Where(id => !requestedSet.Contains(id))
                .OrderBy(id => id)
                .ToList();
            var toAdd = requested.Where(id => !currentIds.Contains(id)).ToList();

            foreach (var idAccessName in toRemove)
            {
                using var deleteCommand = new FbCommand(
                    @"DELETE FROM SS_ACCESSORG
                      WHERE ID_DB = @idDb AND ID_ORG = @idOrg AND ID_ACCESSNAME = @idAccessName",
                    connection,
                    transaction);
                deleteCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                deleteCommand.Parameters.AddWithValue("@idOrg", idOrg);
                deleteCommand.Parameters.AddWithValue("@idAccessName", idAccessName);
                deleteCommand.ExecuteNonQuery();
            }

            foreach (var idAccessName in toAdd)
            {
                // ID_ACCESSORG назначается генератором; триггер SS_ACCESSORG_CREATE_ID
                // оставляет переданное значение без изменений.
                int idAccessOrg;
                using (var idCommand = new FbCommand(
                           "SELECT GEN_ID(GEN_SS_ACCESSORG_ID, 1) FROM RDB$DATABASE",
                           connection,
                           transaction))
                {
                    idAccessOrg = Convert.ToInt32(idCommand.ExecuteScalar());
                }

                using var insertCommand = new FbCommand(
                    @"INSERT INTO SS_ACCESSORG (ID_ACCESSORG, ID_DB, ID_ORG, ID_ACCESSNAME)
                      VALUES (@idAccessOrg, @idDb, @idOrg, @idAccessName)",
                    connection,
                    transaction);
                insertCommand.Parameters.AddWithValue("@idAccessOrg", idAccessOrg);
                insertCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                insertCommand.Parameters.AddWithValue("@idOrg", idOrg);
                insertCommand.Parameters.AddWithValue("@idAccessName", idAccessName);
                insertCommand.ExecuteNonQuery();
            }

            transaction.Commit();

            result.Status = OrganizationOperationStatus.Success;
            return result;
        }

        /// <summary>
        /// Создаёт организацию в транзакции.
        /// </summary>
        public static OrganizationMutationResult Create(string divCode, string name, string? parentDivCode, int flag)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                if (DivCodeExists(connection, transaction, divCode))
                {
                    return new OrganizationMutationResult { Status = OrganizationOperationStatus.DuplicateDivCode };
                }

                int parentId;
                if (string.IsNullOrWhiteSpace(parentDivCode))
                {
                    parentId = RootOrganizationId;
                }
                else if (!TryResolveParent(connection, transaction, parentDivCode, out parentId))
                {
                    return new OrganizationMutationResult
                    {
                        Status = OrganizationOperationStatus.ParentNotFound,
                        ParentDivCode = parentDivCode.Trim()
                    };
                }

                var idOrg = NextOrganizationId(connection, transaction);

                var dateColumns = HasDateCreate.Value ? ", DATE_CREATE, TIME_STAMP" : string.Empty;
                var dateValues = HasDateCreate.Value ? ", CURRENT_TIMESTAMP, CURRENT_TIMESTAMP" : string.Empty;
                var insertSql = $@"INSERT INTO ORGANIZATION
                    (ID_ORG, ID_DB, NAME, ID_PARENT, FLAG, DIVCODE{dateColumns})
                    VALUES (@idOrg, @idDb, @name, @idParent, @flag, @divcode{dateValues})";

                using (var command = new FbCommand(insertSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@idOrg", idOrg);
                    command.Parameters.AddWithValue("@idDb", DatabaseId);
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@idParent", parentId);
                    command.Parameters.AddWithValue("@flag", (short)flag);
                    command.Parameters.AddWithValue("@divcode", divCode);
                    command.ExecuteNonQuery();
                }

                var record = FindById(connection, transaction, idOrg);
                transaction.Commit();

                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.Success,
                    Record = record
                };
            }
            catch (FbException ex)
            {
                LogError(ex);

                if (IsViolation(ex, UniqueViolationCode))
                {
                    return new OrganizationMutationResult { Status = OrganizationOperationStatus.DuplicateDivCode };
                }

                if (IsViolation(ex, ForeignKeyViolationCode))
                {
                    return new OrganizationMutationResult
                    {
                        Status = OrganizationOperationStatus.ParentNotFound,
                        ParentDivCode = parentDivCode?.Trim()
                    };
                }

                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Изменяет организацию: name, parent и flag заменяются переданными значениями.
        /// </summary>
        public static OrganizationMutationResult Update(int idOrg, string name, string? parentDivCode, int flag)
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                if (!LockOrganizationById(connection, transaction, idOrg))
                {
                    return new OrganizationMutationResult { Status = OrganizationOperationStatus.NotFound };
                }

                int parentId;
                if (string.IsNullOrWhiteSpace(parentDivCode))
                {
                    parentId = RootOrganizationId;
                }
                else if (!TryResolveParent(connection, transaction, parentDivCode, out parentId))
                {
                    return new OrganizationMutationResult
                    {
                        Status = OrganizationOperationStatus.ParentNotFound,
                        ParentDivCode = parentDivCode.Trim()
                    };
                }

                if (WouldCreateCycle(connection, transaction, idOrg, parentId))
                {
                    return new OrganizationMutationResult { Status = OrganizationOperationStatus.Cycle };
                }

                var updateSql = @"UPDATE ORGANIZATION
                    SET NAME = @name, ID_PARENT = @idParent, FLAG = @flag, TIME_STAMP = CURRENT_TIMESTAMP
                    WHERE ID_ORG = @idOrg AND ID_DB = @idDb";

                using (var command = new FbCommand(updateSql, connection, transaction))
                {
                    command.Parameters.AddWithValue("@name", name);
                    command.Parameters.AddWithValue("@idParent", parentId);
                    command.Parameters.AddWithValue("@flag", (short)flag);
                    command.Parameters.AddWithValue("@idOrg", idOrg);
                    command.Parameters.AddWithValue("@idDb", DatabaseId);
                    command.ExecuteNonQuery();
                }

                var record = FindById(connection, transaction, idOrg);
                transaction.Commit();

                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.Success,
                    Record = record
                };
            }
            catch (FbException ex)
            {
                LogError(ex);

                if (IsViolation(ex, UniqueViolationCode))
                {
                    return new OrganizationMutationResult { Status = OrganizationOperationStatus.DuplicateDivCode };
                }

                if (IsViolation(ex, ForeignKeyViolationCode))
                {
                    return new OrganizationMutationResult
                    {
                        Status = OrganizationOperationStatus.ParentNotFound,
                        ParentDivCode = parentDivCode?.Trim()
                    };
                }

                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
            catch (Exception ex)
            {
                LogError(ex);
                return new OrganizationMutationResult
                {
                    Status = OrganizationOperationStatus.DatabaseError,
                    ErrorMessage = ex.Message
                };
            }
        }

        /// <summary>
        /// Удаляет организацию, если у неё нет дочерних организаций и активных сотрудников.
        /// </summary>
        public static OrganizationDeleteResult Delete(string divCode)
        {
            var result = new OrganizationDeleteResult { DivCode = divCode.Trim() };

            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();
                using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);

                // 1. Блокируем строку организации: проверки и удаление — в одной транзакции.
                int idOrg;
                using (var lockCommand = new FbCommand(
                           @"SELECT ID_ORG, DIVCODE FROM ORGANIZATION
                             WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode) WITH LOCK",
                           connection,
                           transaction))
                {
                    lockCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    lockCommand.Parameters.AddWithValue("@divcode", result.DivCode);

                    using var reader = lockCommand.ExecuteReader();
                    if (!reader.Read())
                    {
                        result.Status = OrganizationOperationStatus.NotFound;
                        return result;
                    }

                    idOrg = reader.GetInt32(0);
                    result.DivCode = reader.IsDBNull(1) ? result.DivCode : reader.GetString(1);
                }

                if (idOrg == RootOrganizationId)
                {
                    result.Status = OrganizationOperationStatus.RootOrganization;
                    return result;
                }

                // 2. Проверяем дочерние организации и активных сотрудников.
                result.Children = ReadStringList(
                    connection,
                    transaction,
                    "SELECT DIVCODE FROM ORGANIZATION WHERE ID_PARENT = @idOrg AND ID_DB = @idDb ORDER BY DIVCODE",
                    idOrg);

                result.People = ReadStringList(
                    connection,
                    transaction,
                    "SELECT TABNUM FROM PEOPLE WHERE ID_ORG = @idOrg AND ID_DB = @idDb AND \"ACTIVE\" = 1 ORDER BY TABNUM",
                    idOrg);

                if (result.Children.Count > 0 && result.People.Count > 0)
                {
                    result.Status = OrganizationOperationStatus.HasChildrenAndPeople;
                    return result;
                }

                if (result.Children.Count > 0)
                {
                    result.Status = OrganizationOperationStatus.HasChildren;
                    return result;
                }

                if (result.People.Count > 0)
                {
                    result.Status = OrganizationOperationStatus.HasPeople;
                    return result;
                }

                // 3. Препятствий нет — удаляем строку.
                using (var deleteCommand = new FbCommand(
                           "DELETE FROM ORGANIZATION WHERE ID_ORG = @idOrg AND ID_DB = @idDb",
                           connection,
                           transaction))
                {
                    deleteCommand.Parameters.AddWithValue("@idOrg", idOrg);
                    deleteCommand.Parameters.AddWithValue("@idDb", DatabaseId);
                    deleteCommand.ExecuteNonQuery();
                }

                transaction.Commit();
                result.Status = OrganizationOperationStatus.Success;
                return result;
            }
            catch (FbException ex)
            {
                LogError(ex);
                result.Status = OrganizationOperationStatus.DatabaseError;
                result.ErrorMessage = ex.Message;
                return result;
            }
            catch (Exception ex)
            {
                LogError(ex);
                result.Status = OrganizationOperationStatus.DatabaseError;
                result.ErrorMessage = ex.Message;
                return result;
            }
        }

        private static string CreatedAtSql =>
            HasDateCreate.Value ? "COALESCE(o.DATE_CREATE, o.TIME_STAMP)" : "o.TIME_STAMP";

        private static string BuildSelect(string whereSql, string orderSql = "", int? pageSize = null, long skip = 0)
        {
            // Firebird 1.5 не поддерживает производные таблицы, поэтому FIRST/SKIP
            // указываются в основном запросе, а не во внешней обёртке.
            // Слишком большой SKIP ограничиваем int: запрос вернёт пустую страницу.
            var skipValue = skip > int.MaxValue ? int.MaxValue : skip;
            var firstSkip = pageSize.HasValue ? $"FIRST {pageSize.Value} SKIP {skipValue} " : string.Empty;

            return $@"SELECT {firstSkip}o.ID_ORG, o.ID_PARENT, o.NAME, o.FLAG, o.DIVCODE,
       (SELECT p.DIVCODE FROM ORGANIZATION p WHERE p.ID_ORG = o.ID_PARENT AND p.ID_DB = o.ID_DB) AS PARENT_DIVCODE,
       {CreatedAtSql} AS CREATED_AT, o.TIME_STAMP
  FROM ORGANIZATION o
  {whereSql}
  {(string.IsNullOrEmpty(orderSql) ? string.Empty : orderSql)}";
        }

        private static string BuildOrderBy(OrganizationSortField sort, bool descending)
        {
            var direction = descending ? "DESC" : "ASC";

            return sort switch
            {
                OrganizationSortField.DivCode => $"UPPER(o.DIVCODE) {direction}, o.ID_ORG {direction}",
                OrganizationSortField.CreatedAt => $"{CreatedAtSql} {direction}, o.ID_ORG {direction}",
                _ => $"UPPER(o.NAME) {direction}, o.ID_ORG {direction}"
            };
        }

        private static int CountOrganizations(
            FbConnection connection,
            string whereSql,
            List<(string Name, object Value)> parameters)
        {
            using var command = new FbCommand(
                $"SELECT COUNT(*) FROM ORGANIZATION o {whereSql}",
                connection);

            foreach (var parameter in parameters)
            {
                command.Parameters.AddWithValue(parameter.Name, parameter.Value);
            }

            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static bool DivCodeExists(FbConnection connection, FbTransaction transaction, string divCode)
        {
            using var command = new FbCommand(
                "SELECT COUNT(*) FROM ORGANIZATION WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode)",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);
            command.Parameters.AddWithValue("@divcode", divCode);

            return Convert.ToInt32(command.ExecuteScalar()) > 0;
        }

        private static bool TryResolveParent(
            FbConnection connection,
            FbTransaction transaction,
            string parentDivCode,
            out int parentId)
        {
            parentId = RootOrganizationId;

            using var command = new FbCommand(
                "SELECT ID_ORG FROM ORGANIZATION WHERE ID_DB = @idDb AND UPPER(DIVCODE) = UPPER(@divcode) ORDER BY ID_ORG",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);
            command.Parameters.AddWithValue("@divcode", parentDivCode.Trim());

            var value = command.ExecuteScalar();
            if (value == null || value is DBNull)
            {
                return false;
            }

            parentId = Convert.ToInt32(value);
            return true;
        }

        private static int NextOrganizationId(FbConnection connection, FbTransaction transaction)
        {
            using var command = new FbCommand("SELECT GEN_ID(GEN_ORG_ID, 1) FROM RDB$DATABASE", connection, transaction);
            return Convert.ToInt32(command.ExecuteScalar());
        }

        private static bool LockOrganizationById(FbConnection connection, FbTransaction transaction, int idOrg)
        {
            using var command = new FbCommand(
                "SELECT ID_ORG FROM ORGANIZATION WHERE ID_ORG = @idOrg AND ID_DB = @idDb WITH LOCK",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idOrg", idOrg);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

            return command.ExecuteScalar() != null;
        }

        private static OrganizationRecord? FindById(FbConnection connection, FbTransaction transaction, int idOrg)
        {
            var sql = BuildSelect("WHERE o.ID_ORG = @idOrg AND o.ID_DB = @idDb");

            using var command = new FbCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@idOrg", idOrg);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

            using var reader = command.ExecuteReader();
            return reader.Read() ? ReadRecord(reader) : null;
        }

        /// <summary>
        /// Проверяет, что новый родитель не является самой организацией или её потомком.
        /// </summary>
        private static bool WouldCreateCycle(
            FbConnection connection,
            FbTransaction transaction,
            int idOrg,
            int parentId)
        {
            if (parentId == idOrg)
            {
                return true;
            }

            var current = parentId;

            for (var depth = 0; depth <= MaxHierarchyDepth; depth++)
            {
                if (current == idOrg)
                {
                    return true;
                }

                if (current == RootOrganizationId)
                {
                    return false;
                }

                using var command = new FbCommand(
                    "SELECT ID_PARENT FROM ORGANIZATION WHERE ID_ORG = @idOrg AND ID_DB = @idDb",
                    connection,
                    transaction);
                command.Parameters.AddWithValue("@idOrg", current);
                command.Parameters.AddWithValue("@idDb", DatabaseId);

                var value = command.ExecuteScalar();
                if (value == null || value is DBNull)
                {
                    return false;
                }

                current = Convert.ToInt32(value);
            }

            // Цепочка родителей длиннее допустимой: считаем данные испорченными.
            return true;
        }

        private static List<string> ReadStringList(
            FbConnection connection,
            FbTransaction transaction,
            string sql,
            int idOrg)
        {
            var items = new List<string>();

            using var command = new FbCommand(sql, connection, transaction);
            command.Parameters.AddWithValue("@idOrg", idOrg);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                if (!reader.IsDBNull(0))
                {
                    items.Add(reader.GetString(0));
                }
            }

            return items;
        }

        /// <summary>
        /// Возвращает идентификаторы всех существующих категорий доступа (ID_DB = 1).
        /// </summary>
        private static HashSet<int> ReadAccessNameIds(FbConnection connection, FbTransaction transaction)
        {
            var ids = new HashSet<int>();

            using var command = new FbCommand(
                "SELECT ID_ACCESSNAME FROM ACCESSNAME WHERE ID_DB = @idDb",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);

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
        /// Возвращает идентификаторы категорий доступа, уже привязанных к организации.
        /// </summary>
        private static HashSet<int> ReadOrganizationAccessNames(
            FbConnection connection,
            FbTransaction transaction,
            int idOrg)
        {
            var ids = new HashSet<int>();

            using var command = new FbCommand(
                "SELECT ID_ACCESSNAME FROM SS_ACCESSORG WHERE ID_DB = @idDb AND ID_ORG = @idOrg",
                connection,
                transaction);
            command.Parameters.AddWithValue("@idDb", DatabaseId);
            command.Parameters.AddWithValue("@idOrg", idOrg);

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

        private static OrganizationRecord ReadRecord(FbDataReader reader)
        {
            var idParent = reader.GetInt32(1);

            return new OrganizationRecord
            {
                IdOrg = reader.GetInt32(0),
                IdParent = idParent,
                Name = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                Flag = reader.IsDBNull(3) ? 0 : Convert.ToInt32(reader.GetValue(3)),
                DivCode = reader.IsDBNull(4) ? string.Empty : reader.GetString(4),
                // ID_PARENT = 1 — корневой уровень, в API это parent = null.
                ParentDivCode = idParent == RootOrganizationId || reader.IsDBNull(5)
                    ? null
                    : reader.GetString(5),
                CreatedAt = ReadTimestamp(reader, 6),
                UpdatedAt = ReadTimestamp(reader, 7)
            };
        }

        private static DateTime? ReadTimestamp(FbDataReader reader, int ordinal)
        {
            if (reader.IsDBNull(ordinal))
            {
                return null;
            }

            var value = reader.GetDateTime(ordinal);

            // Firebird хранит время без зоны: считаем его UTC и отбрасываем доли секунды.
            var ticks = value.Ticks - value.Ticks % TimeSpan.TicksPerSecond;
            return new DateTime(ticks, DateTimeKind.Utc);
        }

        private static bool DetectDateCreateColumn()
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                using var command = new FbCommand(
                    @"SELECT COUNT(*) FROM RDB$RELATION_FIELDS
                       WHERE RDB$RELATION_NAME = 'ORGANIZATION' AND RDB$FIELD_NAME = 'DATE_CREATE'",
                    connection);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private static bool DetectAccessOrgTable()
        {
            try
            {
                using var connection = new FbConnection(DatabaseService.ConnectionString);
                connection.Open();

                using var command = new FbCommand(
                    @"SELECT COUNT(*) FROM RDB$RELATION_FIELDS
                       WHERE RDB$RELATION_NAME = 'SS_ACCESSORG'",
                    connection);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
            catch (Exception ex)
            {
                LogError(ex);
                return false;
            }
        }

        private static string EscapeLike(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }

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
        /// Проверяет, что ошибка — временный конфликт блокировок (deadlock, lock conflict,
        /// update conflict). Такие ошибки имеет смысл повторить.
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

        private static void LogError(Exception exception)
        {
            DatabaseService.Logger?.LogError(exception, "Organization DB operation failed");
        }
    }
}
