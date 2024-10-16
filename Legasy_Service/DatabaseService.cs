using FirebirdSql.Data.FirebirdClient;
using OpenAPIArtonit.Anotation;
using OpenAPIArtonit.Legasy_Service;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace OpenAPIArtonit.DB
{
    public class DatabaseService
    {
        public static DatabaseResult GetList<T>(string query)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            query = query.ToUpper();
            LoggerService.Log<DatabaseService>("Info", query);

            var rows = new List<T>();

            var connectionString = SettingsService.DatabaseConnectionString;

            using (var connection = new FbConnection(connectionString))
            {
                try
                {
                    connection.Open();

                    var cmd = new FbCommand(query, connection);

                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var instance = (T)Activator.CreateInstance(typeof(T));

                            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.NonPublic | BindingFlags.Public);//СВОЙСТВА МОДЕЛИ

                            foreach (var property in properties)
                            {
                                var databaseNameAttribute = (DatabaseNameAttribute)
                                    Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                                if (databaseNameAttribute == null) continue;

                                LoggerService.Log<DatabaseService>("INFO", $"{property.PropertyType.Name} | V: {dr[databaseNameAttribute.Value.ToUpper()]}");
                                try
                                {
                                    var dbValue = dr[databaseNameAttribute.Value.ToUpper()];
                                    var underlyingType = Nullable.GetUnderlyingType(property.PropertyType);
                                    bool isNullable = underlyingType != null;

                                    if (isNullable && dbValue == DBNull.Value)
                                    {
                                        property.SetValue(instance, null); // Установка значения null для nullable типа
                                    }
                                    else
                                    {
                                        var convertedValue = Convert.ChangeType(dbValue, underlyingType ?? property.PropertyType);
                                        property.SetValue(instance, convertedValue);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggerService.Log<DatabaseService>("Exception", ex.Message);
                                    return new DatabaseResult()
                                    {
                                        ErrorMessage = ex.Message,
                                        State = State.Error,
                                    };
                                }
                            }

                            rows.Add(instance);
                        }
                    }

                    return new DatabaseResult()
                    {
                        State = State.Successes,
                        Value = rows
                    };
                }
                catch (Exception ex)
                {
                    LoggerService.Log<DatabaseService>("Exception", $"{ex.Message}");
                    return new DatabaseResult()
                    {
                        ErrorMessage = ex.Message,
                        State = State.Error,
                    };
                }
            }
        }


        public static DatabaseResult Get<T>(string query)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            query = query.ToUpper();

            LoggerService.Log<DatabaseService>("Info", query);

            using (var connection = new FbConnection(SettingsService.DatabaseConnectionString))
            {
               
                try
                {
                    connection.Open();
                    var cmd = new FbCommand(query, connection);
                    using (var dr = cmd.ExecuteReader())
                    {
                        while (dr.Read())
                        {
                            var instance = (T)Activator.CreateInstance(typeof(T));

                            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Static |
                                BindingFlags.NonPublic | BindingFlags.Public); //СВОЙСТВА МОДЕЛИ

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
                                        property.SetValue(instance, null); // Установка значения null для nullable типа
                                    }
                                    else
                                    {
                                        var convertedValue = Convert.ChangeType(dbValue, underlyingType ?? property.PropertyType);
                                        property.SetValue(instance, convertedValue);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    LoggerService.Log<DatabaseService>("Exception", ex.Message);
                                    return new DatabaseResult()
                                    {
                                        ErrorMessage = ex.Message,
                                        State = State.Error,
                                    };
                                }
                            }

                            return new DatabaseResult()
                            {
                                State = State.Successes,
                                Value = instance
                            };
                        }
                        return new DatabaseResult()
                        {
                            State = State.Error,
                            ErrorMessage = "Запрос в базу данных не дал результата"
                        };
                    }


                }
                catch (Exception ex)
                {
                    LoggerService.Log<DatabaseService>("Exception", $"{ex.Message}");
                    return new DatabaseResult()
                    {
                        ErrorMessage = ex.Message,
                        State = State.Error,
                    };
                }
            }
        }

        public static DatabaseResult ExecuteNonQuery(string query)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            LoggerService.Log<DatabaseService>("Info", query);

            var connectionString = SettingsService.DatabaseConnectionString;

            using (var connection = new FbConnection(connectionString))
            {
                try
                {
                    connection.Open();
                    Console.WriteLine(query);
                    var cmd = new FbCommand(query, connection);
                    var result = cmd.ExecuteNonQuery();
                    Console.WriteLine(result);
                    return new DatabaseResult()
                    {
                        Value = result,
                        State = State.Successes
                    };
                }
                catch (Exception ex)
                {
                    LoggerService.Log<DatabaseService>("Exception", $"{ex.Message}");
                    return new DatabaseResult()
                    {
                        State = State.Error,
                        ErrorMessage = ex.Message
                    };
                }
            }
        }

        public static DatabaseResult Create<T>(T instance)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string query = GenerateCreateQuery(instance);
            LoggerService.Log<DatabaseService>("DEBUG", query);
            Console.WriteLine(query);
            return ExecuteNonQuery(query);
        }

        public static DatabaseResult Update<T>(T instance, string condition)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string query = GenerateUpdateQuery(instance, condition);

            return ExecuteNonQuery(query);
        }

        public static string GenerateUpdateQuery<T>(T instance, string condition)
        {
            string query;

            var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(DatabaseNameAttribute));

            if (attribute is DatabaseNameAttribute databaseName)
            {
                query = $"update {databaseName.Value} set ";
            }
            else
            {
                query = $"update {typeof(T).Name} set ";
            }

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Static |
               BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var property in properties)
            {
                var value = property.GetValue(instance);

                if (value != null)
                {
                    var databaseNameAttribute = (DatabaseNameAttribute)
                         Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                    if (databaseNameAttribute != null)
                    {
                        var systemWord = (DataBaseSystemWordAttribute)
                            Attribute.GetCustomAttribute(property, typeof(DataBaseSystemWordAttribute));

                        switch (value.GetType().Name)
                        {
                            case "String":
                                {
                                    if (systemWord != null)
                                        query += $@"""{databaseNameAttribute.Value}"" = ";
                                    else
                                        query += databaseNameAttribute.Value + " = ";

                                    query += $"'{value}', ";
                                    break;
                                }
                            case "Int32":
                                {
                                    var valueInt = Convert.ToInt32(value);

                                    if (systemWord != null)
                                        query += $@"""{databaseNameAttribute.Value.ToUpper()}"" = ";
                                    else
                                        query += databaseNameAttribute.Value.ToUpper() + " = ";

                                    query += value + ", ";
                                    break;
                                }
                            case "DateTime":
                                {
                                    DateTime parsedDate = DateTime.ParseExact(value.ToString(), "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                                    string output = parsedDate.ToString("dd.MM.yyyy");

                                    if (output == "01.01.0001") continue;

                                    if (systemWord != null)
                                        query += $@"""{databaseNameAttribute.Value.ToUpper()}"" = ";
                                    else
                                        query += databaseNameAttribute.Value.ToUpper() + " = ";

                                    query += $"'{output}', ";
                                    break;
                                }
                            case "TimeSpan":
                                {
                                    if (value.ToString() == "00:00:00") continue;

                                    if (systemWord != null)
                                        query += $@"""{databaseNameAttribute.Value.ToUpper()}"" = ";
                                    else
                                        query += databaseNameAttribute.Value.ToUpper() + " = ";

                                    query += $"'{value}', ";

                                    break;
                                }
                            default:
                                query += value + ", ";
                                break;
                        }
                    }
                }
            }


            query = query.Remove(query.Length - 2);

            query += $" where {condition}";


            return query;
        }

        public static string GenerateCreateQuery<T>(T instance)
        {
            string query;

            var attribute = Attribute.GetCustomAttribute(typeof(T), typeof(DatabaseNameAttribute));

            if (attribute is DatabaseNameAttribute databaseName)
            {
                query = $"insert into {databaseName.Value} (";
            }
            else
            {
                query = $"insert into {typeof(T).Name} (";
            }

            var properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Static |
                BindingFlags.NonPublic | BindingFlags.Public);

            foreach (var property in properties)
            {
                var databasePrimaryKeyAttribute = (DatabasePrimaryKeyAttribute)
                    Attribute.GetCustomAttribute(property, typeof(DatabasePrimaryKeyAttribute));

                if (databasePrimaryKeyAttribute != null) continue;

                var databaseNameAttribute = (DatabaseNameAttribute)
                    Attribute.GetCustomAttribute(property, typeof(DatabaseNameAttribute));

                if (databaseNameAttribute != null)
                {
                    var systemWord = (DataBaseSystemWordAttribute)
                        Attribute.GetCustomAttribute(property, typeof(DataBaseSystemWordAttribute));

                    if (systemWord != null)
                    {
                        query += $@"""{databaseNameAttribute.Value}"", ";
                    }
                    else
                    {
                        query += databaseNameAttribute.Value + ", ";
                    }
                }
            }

            query = query.Remove(query.Length - 2);

            query += ") values (";

            foreach (var property in properties)
            {
                var databasePrimaryKeyAttribute = (DatabasePrimaryKeyAttribute)
                   Attribute.GetCustomAttribute(property, typeof(DatabasePrimaryKeyAttribute));

                if (databasePrimaryKeyAttribute != null) continue;

                var value = property.GetValue(instance);

                if (value != null)
                {
                    switch (value.GetType().Name)
                    {
                        case "String":
                            {
                                query += $"'{value}', ";
                                break;
                            }
                        case "Int32":
                            {
                                query += value + ", ";
                                break;
                            }
                        case "DateTime":
                            {
                                DateTime parsedDate = DateTime.ParseExact(value.ToString(), "dd.MM.yyyy H:mm:ss", CultureInfo.InvariantCulture);
                                string output = parsedDate.ToString("dd.MM.yyyy");
                                query += $"'{output}', ";
                                break;
                            }
                        case "TimeSpan":
                            {
                                query += $"'{value}', ";
                                break;
                            }
                        default:
                            query += value + ", ";
                            break;
                    }
                }
                else
                    query += "null, ";
            }

            query = query.Remove(query.Length - 2);

            query += ");";

            LoggerService.Log<DatabaseService>("INFO", query);
            return query;
        }
    }
}
