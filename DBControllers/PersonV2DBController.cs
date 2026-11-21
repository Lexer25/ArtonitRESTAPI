using ArtonitRESTAPI.Legasy_Service;
using ArtonitRESTAPI.Model;
using OpenAPIArtonit.DB;
using OpenAPIArtonit.DBControllers;
using OpenAPIArtonit.Model;

namespace ArtonitRESTAPI.DBControllers
{
    /// <summary>
    /// Операции над сотрудником для REST API v2 (идентификация по tabnum).
    /// </summary>
    public static class PersonV2DBController
    {
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
    }
}
