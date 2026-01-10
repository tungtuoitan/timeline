-- =============================================
-- Script: Populate workspace keywords from existing workspaces
-- Description:
--   - Optional script to populate workspace keywords after schema migration
--   - Can be run after 05_RECREATE_Keywords_WithWorkspaceId.sql
--   - Safe to run multiple times (checks for existing keywords)
-- Usage: Run this script ONLY if you want to populate workspace keywords for existing workspaces
-- Author: Claude Code
-- Date: 2026-01-08
-- =============================================

USE [Timeline]
GO

BEGIN TRANSACTION;

BEGIN TRY
    PRINT 'Starting population of workspace keywords...';

    -- Create temp table to store workspace keywords with calculated NameIndex
    IF OBJECT_ID('tempdb..#WorkspaceKeywords') IS NOT NULL DROP TABLE #WorkspaceKeywords;

    CREATE TABLE #WorkspaceKeywords (
        WorkspaceId INT,
        UserId INT,
        Name NVARCHAR(255),
        NameIndex INT,
        Link NVARCHAR(2000),
        Description NVARCHAR(MAX),
        CreatedAt DATETIME2(7),
        HardDeletedAt DATETIME2(7)
    );

    -- Calculate NameIndex for each workspace name group
    -- Include both active and soft-deleted workspaces
    WITH WorkspaceNameGroups AS (
        SELECT
            Id AS WorkspaceId,
            UserId,
            Name,
            Description,
            CreatedAt,
            DeletedAt,
            ROW_NUMBER() OVER (PARTITION BY Name ORDER BY CreatedAt) AS NameIndex
        FROM [ws].[workspaces]
    )
    INSERT INTO #WorkspaceKeywords (WorkspaceId, UserId, Name, NameIndex, Link, Description, CreatedAt, HardDeletedAt)
    SELECT
        WorkspaceId,
        UserId,
        Name,
        NameIndex,
        'w' + CAST(WorkspaceId AS NVARCHAR(20)) AS Link,
        Description,
        CreatedAt,
        -- If workspace is soft deleted, set HardDeletedAt on keyword
        CASE WHEN DeletedAt IS NOT NULL THEN GETUTCDATE() ELSE NULL END AS HardDeletedAt
    FROM WorkspaceNameGroups;

    -- Insert workspace keywords (only if not already exists)
    INSERT INTO [dbo].[Keywords] (
        UserId,
        Name,
        NameIndex,
        Type,
        WorkspaceId,
        Link,
        Description,
        CreatedAt,
        UpdatedAt,
        HardDeletedAt,
        TargetItemId,
        NoteItemId,
        ExternalUrl,
        PathIds,
        HeadingPath
    )
    SELECT
        wk.UserId,
        wk.Name,
        wk.NameIndex,
        'workspace' AS Type,
        wk.WorkspaceId,
        wk.Link,
        wk.Description,
        wk.CreatedAt,
        NULL AS UpdatedAt,
        wk.HardDeletedAt,
        NULL AS TargetItemId,
        NULL AS NoteItemId,
        NULL AS ExternalUrl,
        NULL AS PathIds,
        NULL AS HeadingPath
    FROM #WorkspaceKeywords wk
    WHERE NOT EXISTS (
        SELECT 1
        FROM [dbo].[Keywords] k
        WHERE k.WorkspaceId = wk.WorkspaceId AND k.Type = 'workspace'
    );

    DECLARE @insertedCount INT = @@ROWCOUNT;
    PRINT 'Inserted ' + CAST(@insertedCount AS NVARCHAR(20)) + ' workspace keywords';

    -- Clean up temp table
    DROP TABLE #WorkspaceKeywords;

    COMMIT TRANSACTION;
    PRINT 'Population completed successfully!';

END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    DECLARE @ErrorMessage NVARCHAR(4000) = ERROR_MESSAGE();
    DECLARE @ErrorSeverity INT = ERROR_SEVERITY();
    DECLARE @ErrorState INT = ERROR_STATE();

    PRINT 'Population failed: ' + @ErrorMessage;
    RAISERROR(@ErrorMessage, @ErrorSeverity, @ErrorState);
END CATCH
GO
