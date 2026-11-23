/* ============================================================================
   ORGANIZATION: дата создания записи (DATE_CREATE)
   СУБД: Firebird 1.5, диалект 3
   Дата: 22.11.2026

   Зачем: REST API v2 возвращает в организации два поля — createdAt и updatedAt.
   В таблице ORGANIZATION есть только TIME_STAMP (дата последнего изменения),
   поэтому без этой миграции API отдаёт createdAt = updatedAt = TIME_STAMP,
   и createdAt меняется при каждом PUT/PATCH.

   После миграции API пишет DATE_CREATE при создании организации и больше её
   не меняет, а TIME_STAMP обновляет при изменении.

   Миграция необязательная: API определяет наличие колонки при первом запросе
   и работает как с ней, так и без неё.

   Перед выполнением:
   1) сделайте backup (gbak);
   2) остановите API/службу — DDL требует доступа к метаданным.
   ============================================================================ */

SET SQL DIALECT 3;
SET NAMES WIN1251;

/* 1. Колонка уже есть? Должно быть 0 строк. */
SELECT RDB$FIELD_NAME
  FROM RDB$RELATION_FIELDS
 WHERE RDB$RELATION_NAME = 'ORGANIZATION'
   AND RDB$FIELD_NAME = 'DATE_CREATE';

/* 2. Добавляем колонку. */
ALTER TABLE ORGANIZATION ADD DATE_CREATE TIMESTAMP;
COMMIT;

/* 3. Для существующих записей берём TIME_STAMP как наиболее близкую оценку
      даты создания. Записи без TIME_STAMP останутся с NULL: API вернёт
      createdAt = null. */
UPDATE ORGANIZATION
   SET DATE_CREATE = TIME_STAMP
 WHERE DATE_CREATE IS NULL
   AND TIME_STAMP IS NOT NULL;
COMMIT;

/* 4. Контроль: сколько записей осталось без даты создания. */
SELECT COUNT(*) AS CNT_WITHOUT_DATE_CREATE
  FROM ORGANIZATION
 WHERE DATE_CREATE IS NULL;
