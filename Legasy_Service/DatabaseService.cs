using ArtonitRESTAPI.Legasy_Service;
using FirebirdSql.Data.FirebirdClient;
using OpenAPIArtonit.Anotation;
using OpenAPIArtonit.Legasy_Service;
using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace OpenAPIArtonit.DB
{
    public class DatabaseService
    {
        private static ILogger _logger;
        private static SettingsService _settingsService;

        static DatabaseService()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _settingsService = serviceProvider.GetRequiredService<SettingsService>();
            _logger = serviceProvider.GetRequiredService<ILogger<DatabaseService>>();
        }

        public static DatabaseResult GetList<T>(string query)
        {
            query = query.ToUpper();
            _logger.LogInformation("Executing query: {Query}", query);

            var rows = new List<T>();

            using (var connection = new FbConnection(_settingsService.DatabaseConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var cmd = new FbCommand(query, connection))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            while (dr.Read())
                            {
                                var instance = (T)Activator.CreateInstance(typeof(T));
                                var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

                                foreach (var property in properties)
                                {
                                    var databaseNameAttribute = (DatabaseNameAttribute)
                                        Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                                    if (databaseNameAttribute == null) continue;

                                    try
                                    {
                                        var dbValue = dr[databaseNameAttribute.Value.ToUpper()];
                                        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                        bool isNullable = underlyingType != null;

                                        if (isNullable && dbValue == DBNull.Value)
                                        {
                                            property.SetValue(instance, null);
                                        }
                                        else
                                        {
                                            var convertedValue = Convert.ChangeType(dbValue, underlyingType ?? property.PropertyType);
                                            property.SetValue(instance, convertedValue);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error parsing data for property {PropertyName} in query: {Query}", property.Name, query);
                                        return new DatabaseResult
                                        {
                                            ErrorMessage = $"Error parsing data for {property.Name}: {ex.Message}",
                                            State = State.NullSQLRequest
                                        };
                                    }
                                }

                                rows.Add(instance);
                            }
                        }
                    }

                    return new DatabaseResult
                    {
                        State = State.Successes,
                        Value = rows
                    };
                }
                catch (FbException ex)
                {
                    _logger.LogError(ex, "Database error executing query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Database error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error executing query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
            }
        }

        public static DatabaseResult Get<T>(string query)
        {
            query = query.ToUpper();
            _logger.LogInformation("Executing query: {Query}", query);

            using (var connection = new FbConnection(_settingsService.DatabaseConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var cmd = new FbCommand(query, connection))
                    {
                        using (var dr = cmd.ExecuteReader())
                        {
                            if (dr.Read())
                            {
                                var instance = (T)Activator.CreateInstance(typeof(T));
                                var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);

                                foreach (var property in properties)
                                {
                                    var databaseNameAttribute = (DatabaseNameAttribute)
                                        Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                                    if (databaseNameAttribute == null) continue;

                                    try
                                    {
                                        var dbValue = dr[databaseNameAttribute.Value.ToUpper()];
                                        var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                        bool isNullable = underlyingType != null;

                                        if (isNullable && dbValue == DBNull.Value)
                                        {
                                            property.SetValue(instance, null);
                                        }
                                        else
                                        {
                                            var convertedValue = Convert.ChangeType(dbValue, underlyingType ?? property.PropertyType);
                                            property.SetValue(instance, convertedValue);
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        _logger.LogError(ex, "Error parsing data for property {PropertyName} in query: {Query}", property.Name, query);
                                        return new DatabaseResult
                                        {
                                            ErrorMessage = $"Error parsing data for {property.Name}: {ex.Message}",
                                            State = State.BadSQLRequest
                                        };
                                    }
                                }

                                return new DatabaseResult
                                {
                                    State = State.Successes,
                                    Value = instance
                                };
                            }

                            return new DatabaseResult
                            {
                                State = State.NullSQLRequest,
                                ErrorMessage = "No results found"
                            };
                        }
                    }
                }
                catch (FbException ex)
                {
                    _logger.LogError(ex, "Database error executing query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Database error: {ex.Message}",
                        State = State.NullDataBase
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error executing query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        State = State.NullDataBase
                    };
                }
            }
        }

        public static DatabaseResult ExecuteNonQuery(string query)
        {
            //query = query.ToUpper();
            _logger.LogInformation("Executing non-query: {Query}", query);

            using (var connection = new FbConnection(_settingsService.DatabaseConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var cmd = new FbCommand(query, connection))
                    {
                        var result = cmd.ExecuteNonQuery();

                        return new DatabaseResult
                        {
                            Value = result,
                            State = result == 0 ? State.NullSQLRequest : State.Successes
                        };
                    }
                }
                catch (FbException ex)
                {
                    _logger.LogError(ex, "Database error executing non-query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Database error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error executing non-query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
            }
        }

        public static DatabaseResult Create<T>(T instance)
        {
            var (query, parameters) = GenerateCreateQuery(instance);
            _logger.LogDebug("Generated create query: {Query}", query);
            return ExecuteParameterizedNonQuery(query, parameters);
        }

        public static DatabaseResult Update<T>(T instance, string condition)
        {
            var (query, parameters) = GenerateUpdateQuery(instance, condition);
            _logger.LogDebug("Generated update query: {Query}", query);
            return ExecuteParameterizedNonQuery(query, parameters);
        }

        private static DatabaseResult ExecuteParameterizedNonQuery(string query, Dictionary<string, object> parameters)
        {
            query = query.ToUpper();
            _logger.LogInformation("Executing parameterized non-query: {Query}", query);

            using (var connection = new FbConnection(_settingsService.DatabaseConnectionString))
            {
                try
                {
                    connection.Open();
                    using (var cmd = new FbCommand(query, connection))
                    {
                        foreach (var param in parameters)
                        {
                            cmd.Parameters.AddWithValue(param.Key, param.Value ?? DBNull.Value);
                        }

                        var result = cmd.ExecuteNonQuery();

                        return new DatabaseResult
                        {
                            Value = result,
                            State = result == 0 ? State.NullSQLRequest : State.Successes
                        };
                    }
                }
                catch (FbException ex)
                {
                    _logger.LogError(ex, "Database error executing parameterized non-query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Database error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Unexpected error executing parameterized non-query: {Query}", query);
                    return new DatabaseResult
                    {
                        ErrorMessage = $"Unexpected error: {ex.Message}",
                        State = State.BadSQLRequest
                    };
                }
            }
        }

        private static (string Query, Dictionary<string, object> Parameters) GenerateUpdateQuery<T>(T instance, string condition)
        {
            var parameters = new Dictionary<string, object>();
            var queryBuilder = new StringBuilder();
            var tableAttr = Attribute.GetCustomAttribute(typeof(T), typeof(DatabaseNameAttribute));

            queryBuilder.Append(tableAttr is DatabaseNameAttribute dbName
                ? $"UPDATE {dbName.Value} SET "
                : $"UPDATE {typeof(T).Name} SET ");

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var setClauses = new List<string>();
            int paramIndex = 0;

            foreach (var property in properties)
            {
                var value = property.GetValue(instance);
                if (value == null) continue;

                var dbNameAttr = (DatabaseNameAttribute)
                    Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                if (dbNameAttr == null) continue;

                string columnName = dbNameAttr.Value.ToUpper();
                var systemWord = Attribute.IsDefined(property, typeof(DataBaseSystemWordAttribute));
                string quote = systemWord ? $"\"{columnName}\"" : columnName;
                string paramName = $"@p{paramIndex++}";

                setClauses.Add($"{quote} = {paramName}");
                parameters.Add(paramName, value);
            }

            if (setClauses.Count == 0)
            {
                throw new InvalidOperationException("No valid properties found to update.");
            }

            queryBuilder.Append(string.Join(", ", setClauses));
            queryBuilder.Append($" WHERE {condition}");

            return (queryBuilder.ToString(), parameters);
        }

        private static (string Query, Dictionary<string, object> Parameters) GenerateCreateQuery<T>(T instance)
        {
            var parameters = new Dictionary<string, object>();
            var queryBuilder = new StringBuilder();
            var tableAttr = Attribute.GetCustomAttribute(typeof(T), typeof(DatabaseNameAttribute));

            queryBuilder.Append(tableAttr is DatabaseNameAttribute dbName
                ? $"INSERT INTO {dbName.Value} ("
                : $"INSERT INTO {typeof(T).Name} (");

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public);
            var columns = new List<string>();
            var paramNames = new List<string>();
            int paramIndex = 0;

            foreach (var property in properties)
            {
                if (Attribute.IsDefined(property, typeof(DatabasePrimaryKeyAttribute)))
                    continue;

                var dbAttr = (DatabaseNameAttribute)Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));
                if (dbAttr == null) continue;

                string column = Attribute.IsDefined(property, typeof(DataBaseSystemWordAttribute))
                    ? $"\"{dbAttr.Value}\""
                    : dbAttr.Value;

                var value = property.GetValue(instance);
                string paramName = $"@p{paramIndex++}";

                columns.Add(column);
                paramNames.Add(paramName);
                parameters.Add(paramName, value);
            }

            if (columns.Count == 0)
            {
                throw new InvalidOperationException("No valid properties found to insert.");
            }

            queryBuilder.Append(string.Join(", ", columns));
            queryBuilder.Append(") VALUES (");
            queryBuilder.Append(string.Join(", ", paramNames));
            queryBuilder.Append(");");

            return (queryBuilder.ToString(), parameters);
        }
    }
}