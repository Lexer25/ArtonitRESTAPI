/* ============================================================================
   ORGANIZATION: уникальность DIVCODE в рамках базы (ID_DB)
   СУБД: Firebird 1.5, диалект 3
   Дата: 22.11.2026

   Перед выполнением:
   1) сделайте backup (gbak);
   2) остановите API/службу — DDL требует доступа к метаданным;
   3) выполните проверки ниже. Если любой запрос вернул строки,
      сначала устраните дубликаты: CREATE UNIQUE INDEX упадёт.
   ============================================================================ */

SET SQL DIALECT 3;
SET NAMES WIN1251;

/* 1. Индекс уже существует? Должно быть 0 строк. */
SELECT RDB$INDEX_NAME
  FROM RDB$INDICES
 WHERE RDB$INDEX_NAME = 'UNQ_ORGANIZATION_DIVCODE';

/* 2. Дубликаты с учётом регистра. Должно быть 0 строк. */
SELECT DIVCODE, ID_DB, COUNT(*) AS CNT
  FROM ORGANIZATION
 GROUP BY DIVCODE, ID_DB
HAVING COUNT(*) > 1;

/* 3. Дубликаты без учёта регистра. Должно быть 0 строк,
      если уникальность должна быть регистронезависимой. */
SELECT UPPER(DIVCODE) AS DIVCODE_UC, ID_DB, COUNT(*) AS CNT
  FROM ORGANIZATION
 GROUP BY UPPER(DIVCODE), ID_DB
HAVING COUNT(*) > 1;

/* 4. Уникальный индекс по (DIVCODE, ID_DB).
      Индекс регистрозависимый: 'ORG-1' и 'org-1' — разные значения. */
CREATE UNIQUE INDEX UNQ_ORGANIZATION_DIVCODE ON ORGANIZATION (DIVCODE, ID_DB);
COMMIT;

/* 5. После создания уникального индекса неуникальный
      IDX_ORGANIZATION_DIVCODE (если он есть) можно удалить:
-- DROP INDEX IDX_ORGANIZATION_DIVCODE;
-- COMMIT;
*/

/* 6. Альтернатива вместо индекса — именованный constraint
      (создаёт такой же уникальный индекс):
-- ALTER TABLE ORGANIZATION ADD CONSTRAINT UNQ_ORGANIZATION_DIVCODE UNIQUE (DIVCODE, ID_DB);
-- COMMIT;
*/
