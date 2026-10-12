---
applyTo: "**/sqlserver/**/*.sql,**/mssql/**/*.sql"
description: "SQL Server 2025 and T-SQL rules"
---

# SQL Server and T-SQL Guidelines

## Purpose
Rules for writing and reviewing T-SQL (schema, stored procedures, functions, views, queries, migrations). Target: **SQL Server 2025 (17.x)**, database compatibility level **170**. When a feature needs 2025, say so in a comment so it isn't deployed to older servers by mistake.

Applies to `.sql` files under a `sqlserver/` or `mssql/` folder. In a repository that only uses SQL Server, you can widen `applyTo` to `**/*.sql`.

## General style
- Keywords in UPPER CASE, identifiers in their defined case. One clause per line (`SELECT`, `FROM`, `JOIN`, `WHERE`, `GROUP BY`, `ORDER BY`), indented consistently.
- End every statement with a semicolon. Start a CTE with `WITH`, not `;WITH`.
- Always schema-qualify objects (`dbo.Customer`, never `Customer`).
- Name columns explicitly: no `SELECT *` in production code, and always list columns in `INSERT`.
- Use meaningful table aliases (`c` for `Customer`, `o` for `Order`); prefix every column with its alias in multi-table queries.
- Use `--` comments for intent; add a header block (purpose, parameters, change history) to procedures and functions.
- Prefer ANSI syntax: `JOIN ... ON` (never comma joins), `CAST`/`COALESCE`/`CURRENT_TIMESTAMP` where equivalent.

## Naming
- Tables: singular PascalCase (`Customer`, `OrderLine`). Columns: PascalCase; primary key `<Table>Id` (e.g. `CustomerId`).
- Constraints and indexes: `PK_<Table>`, `FK_<Child>_<Parent>`, `UQ_<Table>_<Cols>`, `CK_<Table>_<Rule>`, `DF_<Table>_<Col>`, `IX_<Table>_<Cols>`.
- Stored procedures: `<Schema>.<Entity>_<Action>` (e.g. `sales.Order_Insert`). Never prefix with `sp_` (reserved for system procedures).
- No spaces, reserved words or Hungarian notation (`tbl`, `vw`) in names.

## Data types and schema
- Choose the smallest correct type. Money: `decimal(19,4)`, not `float` or `money`. Dates: `date`, `datetime2(n)` or `datetimeoffset(n)`, not `datetime`.
- Text: `nvarchar(n)` for user-facing text; `varchar(n)` only with a UTF-8 collation or known ASCII data. Avoid `(max)` unless required. Never `text`, `ntext` or `image`.
- JSON: store in the native `json` type and query with `JSON_VALUE`, `JSON_QUERY`, `OPENJSON`; build output with `JSON_OBJECT`, `JSON_ARRAY`, `JSON_OBJECTAGG`, `JSON_ARRAYAGG`.
- Embeddings: use the `vector(n)` type.
- Every table has a primary key; declare `NOT NULL` wherever possible; enforce integrity with foreign keys, `CHECK` and `UNIQUE` constraints, not only in application code.
- Name all constraints explicitly (never let SQL Server auto-generate names).
- Use `IDENTITY` or `SEQUENCE` for surrogate keys; if using `uniqueidentifier` keys, default with `NEWSEQUENTIALID()`.
- Use temporal tables (`SYSTEM_VERSIONING = ON`) for audit history instead of trigger-based audit tables.

## Modern T-SQL (use these, SQL Server 2025)
- Regex: `REGEXP_LIKE`, `REGEXP_REPLACE`, `REGEXP_SUBSTR`, `REGEXP_INSTR`, `REGEXP_COUNT`, `REGEXP_MATCHES`, `REGEXP_SPLIT_TO_TABLE` instead of complex `LIKE`/`PATINDEX`/CLR code.
- String concatenation with `||` or `CONCAT`/`CONCAT_WS`; `STRING_AGG` for lists; `STRING_SPLIT(..., ..., 1)` when order matters.
- `SUBSTRING(expr, start)` (length optional), `CURRENT_DATE`, `DATETRUNC`, `DATE_BUCKET`, `GREATEST`/`LEAST`, `PRODUCT`, `UNISTR`, `BASE64_ENCODE`/`BASE64_DECODE`, `IS [NOT] DISTINCT FROM`.
- `CREATE OR ALTER` for procedures, functions, views and triggers; `DROP ... IF EXISTS` for idempotent scripts.
- `TRIM`, `TRANSLATE`, `WINDOW` clause for reused window definitions.
- Features requiring `ALTER DATABASE SCOPED CONFIGURATION SET PREVIEW_FEATURES = ON` (vector index, `VECTOR_SEARCH`, `EDIT_DISTANCE`, `JARO_WINKLER_*`, change event streaming) are **preview**: never use them in production code without explicit approval, and mark them with a `-- PREVIEW` comment.

## Queries and performance
- Write set-based code. No cursors or `WHILE` loops over rows unless there is no set-based alternative; justify them in a comment.
- Keep predicates sargable: no functions or implicit conversions on indexed columns in `WHERE`/`JOIN` (e.g. use `OrderDate >= @Start AND OrderDate < @End`, not `YEAR(OrderDate) = 2026`).
- Match parameter and column data types exactly to avoid implicit conversions.
- Use `EXISTS` instead of `COUNT(*) > 0` or `IN` with large subqueries; use `NOT EXISTS` instead of `NOT IN` (NULL-safe).
- Prefer inline table-valued functions over multi-statement TVFs and scalar UDFs in queries.
- Use `OFFSET ... FETCH` with a deterministic `ORDER BY` for paging; keyset paging for large tables.
- Temp tables (`#t`) for larger intermediate results that need statistics; table variables only for small sets.
- No `NOLOCK` / `READ UNCOMMITTED`. Use `READ_COMMITTED_SNAPSHOT` and rely on optimized locking instead.
- Don't add query hints or index hints without a documented reason and measured plan.
- Index design: cluster on a narrow, ever-increasing key; nonclustered indexes with `INCLUDE` columns for covering; filtered indexes for sparse predicates; columnstore for analytics. Don't create duplicate or unused indexes.

## Stored procedures and transactions
- Start procedures with `SET NOCOUNT ON; SET XACT_ABORT ON;`.
- Wrap multi-statement changes in `BEGIN TRY ... BEGIN TRANSACTION ... COMMIT ... END TRY BEGIN CATCH IF @@TRANCOUNT > 0 ROLLBACK; THROW; END CATCH`.
- Use `THROW` (not `RAISERROR`) for errors; custom error numbers ≥ 50000 with clear messages.
- Keep transactions short; never wait for user input or external calls inside a transaction.
- Use `MERGE` only with care (always `HOLDLOCK` and a single-row match guarantee); prefer separate `UPDATE` + `INSERT` for upserts.
- Use `OUTPUT` to return changed rows instead of re-querying.
- Return result sets with explicit column lists; use output parameters or a status result, not `RETURN` values, for data.

## Security
- Never build SQL by concatenating user input. For dynamic SQL use `sp_executesql` with typed parameters and wrap identifiers in `QUOTENAME()`.
- Least privilege: grant `EXECUTE` on schemas/procedures to roles; never grant rights to individual users or use `db_owner` for applications.
- Never hardcode passwords, keys or connection strings in scripts. Prefer Microsoft Entra authentication or managed identity over SQL logins.
- Protect sensitive columns with Always Encrypted, Dynamic Data Masking or Row-Level Security as appropriate.
- Clients connect with encryption (`Encrypt=Strict`, TDS 8.0 / TLS 1.3 where supported).
- `sp_invoke_external_rest_endpoint` and external AI models: use database-scoped credentials with managed identity, never inline keys.

## Migrations and deployment
- Scripts are idempotent and rerunnable (`IF NOT EXISTS`, `CREATE OR ALTER`, `DROP ... IF EXISTS`).
- One logical change per migration file, named with a sortable prefix (e.g. `20260926_0730_add_order_status.sql`).
- Never drop or rename columns in the same release that stops using them; use expand-and-contract.
- Large data changes run in batches (e.g. `TOP (5000)` loops) to limit log growth and blocking.
- Create indexes with `ONLINE = ON` and `RESUMABLE = ON` where the edition allows.
- Prefer SQL Database Projects (SDK-style `.sqlproj`) as the source of truth for schema.

## Testing
- Unit test procedures and functions with tSQLt.
- Check execution plans and `SET STATISTICS IO, TIME ON` output for new or changed queries; use Query Store to compare before and after.

## Code review focus
Flag in this order: SQL injection risks (concatenated dynamic SQL), missing transaction or error handling, non-sargable predicates and implicit conversions, `SELECT *`, `NOLOCK`, cursors, missing constraints or keys, unqualified object names, preview features in production code.
