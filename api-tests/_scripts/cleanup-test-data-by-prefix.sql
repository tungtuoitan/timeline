/*
**Fullpath:** /api-tests/_scripts/cleanup-test-data-by-prefix.sql

Dọn TOÀN BỘ data do api-tests để lại trên SuperApp-dev, tìm theo email user test
`apitest+%@test.local` (mọi data test đều thuộc các user này — không đụng user thật).

Xoá: comment, checklist history, task, project, workspace (+ item cascade), keyword, refresh token,
profile, user — và các dòng lịch sử temporal (pro.project_history / task_history /
task_comment_history) của chúng (tạm tắt SYSTEM_VERSIONING trong transaction rồi bật lại).

AN TOÀN: -v APPLY=0 -> chạy hết rồi ROLLBACK (dry-run, in số dòng). -v APPLY=1 mới COMMIT.
History table phải xoá ở batch riêng (GO) sau khi tắt versioning -> id giữ trong bảng tạm #u/#p/#t/#w. Chỉ chạy trên SuperApp-dev — script tự dừng nếu DB_NAME() khác.

Chạy (từ máy nhà, qua tunnel; password lấy từ vault, không in ra — xem skill find-credential):
  secret run -e SQLCMDPASSWORD=vps/sql_server.sa_password -- sqlcmd -S 127.0.0.1,14331 -U sa -C \
    -d SuperApp-dev -v APPLY=0 -i cleanup-test-data-by-prefix.sql
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON; -- filtered index trên ws.workspaces cần option này

DECLARE @db sysname = DB_NAME();
IF @db <> N'SuperApp-dev'
BEGIN
    RAISERROR(N'Chỉ chạy trên SuperApp-dev (đang ở %s).', 16, 1, @db);
    RETURN;
END

CREATE TABLE #u (id int PRIMARY KEY);
INSERT #u SELECT id FROM urm.users WHERE email LIKE N'apitest+%@test.local';
CREATE TABLE #p (id int PRIMARY KEY);
INSERT #p SELECT id FROM pro.project WHERE user_id IN (SELECT id FROM #u);
CREATE TABLE #t (id int PRIMARY KEY);
INSERT #t SELECT id FROM pro.task WHERE project_id IN (SELECT id FROM #p);
CREATE TABLE #w (id int PRIMARY KEY);
INSERT #w SELECT id FROM ws.workspaces WHERE user_id IN (SELECT id FROM #u);

DECLARE @nu int = (SELECT COUNT(*) FROM #u), @np int = (SELECT COUNT(*) FROM #p),
        @nt int = (SELECT COUNT(*) FROM #t), @nw int = (SELECT COUNT(*) FROM #w);
PRINT CONCAT(N'users=', @nu, N' projects=', @np, N' tasks=', @nt, N' workspaces=', @nw);

BEGIN TRAN;

-- comment (gỡ self-ref trước)
UPDATE pro.task_comment SET parent_comment_id = NULL WHERE task_id IN (SELECT id FROM #t) OR user_id IN (SELECT id FROM #u);
DELETE FROM pro.task_comment WHERE task_id IN (SELECT id FROM #t) OR user_id IN (SELECT id FROM #u);
PRINT CONCAT(N'task_comment: ', @@ROWCOUNT);

DELETE FROM pro.task_checklist_history WHERE task_id IN (SELECT id FROM #t);
PRINT CONCAT(N'task_checklist_history: ', @@ROWCOUNT);

-- task (gỡ parent + folder link trước)
UPDATE pro.task SET parent_task_id = NULL, folder_workspace_item_id = NULL WHERE id IN (SELECT id FROM #t);
DELETE FROM pro.task WHERE id IN (SELECT id FROM #t);
PRINT CONCAT(N'task: ', @@ROWCOUNT);

DELETE FROM pro.project WHERE id IN (SELECT id FROM #p);
PRINT CONCAT(N'project: ', @@ROWCOUNT);

DELETE FROM ws.workspaces WHERE id IN (SELECT id FROM #w); -- workspace_items: ON DELETE CASCADE
PRINT CONCAT(N'workspaces: ', @@ROWCOUNT);

-- users: Keywords, refresh_tokens, user_profiles đều ON DELETE CASCADE
DELETE FROM urm.users WHERE id IN (SELECT id FROM #u);
PRINT CONCAT(N'users: ', @@ROWCOUNT);
GO

-- lịch sử temporal: phải tắt versioning mới xoá được history
ALTER TABLE pro.task_comment SET (SYSTEM_VERSIONING = OFF);
GO
DELETE FROM pro.task_comment_history WHERE task_id IN (SELECT id FROM #t) OR user_id IN (SELECT id FROM #u);
PRINT CONCAT(N'task_comment_history: ', @@ROWCOUNT);
ALTER TABLE pro.task_comment SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_comment_history));

ALTER TABLE pro.task SET (SYSTEM_VERSIONING = OFF);
GO
DELETE FROM pro.task_history WHERE id IN (SELECT id FROM #t) OR project_id IN (SELECT id FROM #p);
PRINT CONCAT(N'task_history: ', @@ROWCOUNT);
ALTER TABLE pro.task SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.task_history));

ALTER TABLE pro.project SET (SYSTEM_VERSIONING = OFF);
GO
DELETE FROM pro.project_history WHERE id IN (SELECT id FROM #p) OR user_id IN (SELECT id FROM #u);
PRINT CONCAT(N'project_history: ', @@ROWCOUNT);
ALTER TABLE pro.project SET (SYSTEM_VERSIONING = ON (HISTORY_TABLE = pro.project_history));

GO
IF $(APPLY) = 1
BEGIN
    COMMIT;
    PRINT N'==> COMMITTED';
END
ELSE
BEGIN
    ROLLBACK;
    PRINT N'==> DRY-RUN, rolled back (chạy lại với -v APPLY=1 để xoá thật)';
END
