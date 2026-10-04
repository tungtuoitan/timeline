-- =============================================
-- Rename pro.task.note -> pro.task.description (TungRoot issue 0095, step 3)
-- The UI already labels this field "Description". Idempotent: safe to re-run.
-- Checked 2026-10-04 on dev + pro: no view/proc/index/full-text uses the column.
--
-- Rollback:
--   EXEC sp_rename 'pro.task.description', 'note', 'COLUMN';
-- =============================================

IF COL_LENGTH('pro.task', 'note') IS NOT NULL
   AND COL_LENGTH('pro.task', 'description') IS NULL
BEGIN
    EXEC sp_rename 'pro.task.note', 'description', 'COLUMN';
    PRINT 'Renamed pro.task.note -> description';
END
ELSE
BEGIN
    PRINT 'Skip: pro.task.note already renamed (or description exists)';
END
GO
