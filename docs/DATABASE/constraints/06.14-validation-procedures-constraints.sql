-- ============================================
-- FILE: constraints/06.14-validation-procedures.sql
-- PURPOSE: Data validation and fix procedures
-- DEPENDENCIES: 02-tables-core.sql, 03-tables-workspace.sql, 07-tables-entities.sql
-- ============================================

PRINT '🔒 Creating data validation procedures...';
GO

-- Procedure: Validate data integrity
CREATE OR ALTER PROCEDURE usp_validate_data_integrity
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @errors TABLE (
        error_type NVARCHAR(100),
        error_message NVARCHAR(500),
        record_count INT
    );
    
    PRINT 'Running data integrity checks...';
    PRINT '';
    
    -- Check 1: Orphaned workspace members (workspace deleted)
    INSERT INTO @errors
    SELECT 
        'Orphaned Members',
        'Workspace members without workspace',
        COUNT(*)
    FROM workspace_members wm
    WHERE wm.deleted_at IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM workspaces w 
        WHERE w.id = wm.workspace_id 
        AND w.deleted_at IS NULL
    );
    
    -- Check 2: Orphaned relationships (tags deleted)
    INSERT INTO @errors
    SELECT 
        'Orphaned Relationships',
        'Relationships with deleted tags',
        COUNT(*)
    FROM workspace_tag_relationships wtr
    WHERE wtr.deleted_at IS NULL
    AND (
        NOT EXISTS (SELECT 1 FROM tags WHERE id = wtr.from_tag_id AND deleted_at IS NULL)
        OR NOT EXISTS (SELECT 1 FROM tags WHERE id = wtr.to_tag_id AND deleted_at IS NULL)
    );
    
    -- Check 3: Incorrect usage_count
    INSERT INTO @errors
    SELECT 
        'Incorrect Usage Count',
        'Tags with wrong usage_count',
        COUNT(*)
    FROM tags t
    WHERE t.deleted_at IS NULL
    AND t.usage_count != (
        SELECT COUNT(DISTINCT workspace_id)
        FROM workspace_tag_relationships wtr
        WHERE (wtr.from_tag_id = t.id OR wtr.to_tag_id = t.id)
        AND wtr.deleted_at IS NULL
    );
    
    -- Check 4: Workspaces without owners
    INSERT INTO @errors
    SELECT 
        'Workspaces Without Owner',
        'Workspaces without owner member',
        COUNT(*)
    FROM workspaces w
    WHERE w.deleted_at IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM workspace_members wm
        WHERE wm.workspace_id = w.id
        AND wm.role = 'owner'
        AND wm.deleted_at IS NULL
    );
    
    -- Check 5: Incorrect workspace stats
    INSERT INTO @errors
    SELECT 
        'Incorrect Workspace Stats',
        'Workspaces with wrong tag/relationship counts',
        COUNT(*)
    FROM workspaces w
    WHERE w.deleted_at IS NULL
    AND (
        w.relationship_count != (
            SELECT COUNT(*) FROM workspace_tag_relationships 
            WHERE workspace_id = w.id AND deleted_at IS NULL
        )
        OR w.member_count != (
            SELECT COUNT(*) FROM workspace_members
            WHERE workspace_id = w.id AND deleted_at IS NULL AND invitation_status = 'active'
        )
    );
    
    -- Check 6: Multiple default workspaces per user
    INSERT INTO @errors
    SELECT 
        'Multiple Default Workspaces',
        'Users with multiple default workspaces',
        COUNT(*)
    FROM (
        SELECT user_id
        FROM workspaces
        WHERE deleted_at IS NULL AND is_default = 1
        GROUP BY user_id
        HAVING COUNT(*) > 1
    ) AS dups;
    
    -- Check 7: Invalid paths (deprecated table)
    INSERT INTO @errors
    SELECT 
        'Invalid Paths (deprecated)',
        'Relationships with depth not matching path',
        COUNT(*)
    FROM workspace_tag_relationships
    WHERE deleted_at IS NULL
    AND to_path IS NOT NULL
    AND depth != LEN(to_path) - LEN(REPLACE(to_path, '.', ''));
    
    -- Check 8: workspace_items with invalid child_type
    INSERT INTO @errors
    SELECT 
        'Invalid Child Type',
        'workspace_items with unknown child_type',
        COUNT(*)
    FROM workspace_items wi
    WHERE wi.deleted_at IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM entity_types et
        WHERE et.type_name = wi.child_type
        AND et.is_active = 1
    );
    
    -- Check 9: workspace_items pointing to deleted entities
    INSERT INTO @errors
    SELECT 
        'Orphaned workspace_items',
        'workspace_items pointing to deleted entities',
        COUNT(*)
    FROM workspace_items wi
    WHERE wi.deleted_at IS NULL
    AND (
        (wi.child_type = 'tag' AND NOT EXISTS (
            SELECT 1 FROM tags t WHERE t.id = wi.child_id AND t.deleted_at IS NULL
        ))
        OR (wi.child_type = 'note' AND NOT EXISTS (
            SELECT 1 FROM notes n WHERE n.id = wi.child_id AND n.deleted_at IS NULL
        ))
    );
    
    -- Check 10: Notes without owner
    INSERT INTO @errors
    SELECT 
        'Notes Without Owner',
        'Notes without owner member',
        COUNT(*)
    FROM notes n
    WHERE n.deleted_at IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM note_members nm
        WHERE nm.note_id = n.id
        AND nm.role = 'owner'
        AND nm.deleted_at IS NULL
    );
    
    -- Check 11: Notes with incorrect version_count
    INSERT INTO @errors
    SELECT 
        'Incorrect Note Version Count',
        'Notes with wrong version_count',
        COUNT(*)
    FROM notes n
    WHERE n.deleted_at IS NULL
    AND n.version_count != (
        SELECT COUNT(*) FROM note_versions nv
        WHERE nv.note_id = n.id
    );
    
    -- Check 12: Note versions with missing content
    INSERT INTO @errors
    SELECT 
        'Note Versions Missing Content',
        'Note versions without content',
        COUNT(*)
    FROM note_versions
    WHERE content IS NULL;
    
    -- Check 13: Note members without user
    INSERT INTO @errors
    SELECT 
        'Invalid Note Members',
        'Note members referencing deleted users',
        COUNT(*)
    FROM note_members nm
    WHERE nm.deleted_at IS NULL
    AND NOT EXISTS (
        SELECT 1 FROM users u
        WHERE u.id = nm.user_id
        AND u.deleted_at IS NULL
    );
    
    -- Display results
    SELECT * FROM @errors WHERE record_count > 0;
    
    IF NOT EXISTS (SELECT 1 FROM @errors WHERE record_count > 0)
    BEGIN
        PRINT '✅ All integrity checks passed!';
    END
    ELSE
    BEGIN
        PRINT '⚠️ Integrity issues found. Review results above.';
    END
END;
GO

PRINT '   ✅ usp_validate_data_integrity created';

-- Procedure: Fix common data issues
CREATE OR ALTER PROCEDURE usp_fix_data_issues
AS
BEGIN
    SET NOCOUNT ON;
    
    PRINT 'Fixing data issues...';
    PRINT '';
    
    BEGIN TRY
        BEGIN TRANSACTION;
        
        -- Fix 1: Update incorrect usage_count
        UPDATE t
        SET usage_count = (
            SELECT COUNT(DISTINCT workspace_id)
            FROM workspace_tag_relationships wtr
            WHERE (wtr.from_tag_id = t.id OR wtr.to_tag_id = t.id)
            AND wtr.deleted_at IS NULL
        )
        FROM tags t
        WHERE t.deleted_at IS NULL;
        
        PRINT '   ✅ Fixed tag usage_count';
        
        -- Fix 2: Update workspace statistics
        UPDATE w
        SET 
            relationship_count = (
                SELECT COUNT(*) FROM workspace_tag_relationships 
                WHERE workspace_id = w.id AND deleted_at IS NULL
            ),
            tag_count = (
                SELECT COUNT(DISTINCT tag_id)
                FROM (
                    SELECT from_tag_id AS tag_id FROM workspace_tag_relationships 
                    WHERE workspace_id = w.id AND deleted_at IS NULL
                    UNION
                    SELECT to_tag_id AS tag_id FROM workspace_tag_relationships 
                    WHERE workspace_id = w.id AND deleted_at IS NULL
                ) AS tags
            ),
            member_count = (
                SELECT COUNT(*) FROM workspace_members
                WHERE workspace_id = w.id AND deleted_at IS NULL AND invitation_status = 'active'
            )
        FROM workspaces w
        WHERE w.deleted_at IS NULL;
        
        PRINT '   ✅ Fixed workspace statistics';
        
        -- Fix 3: Remove orphaned relationships
        UPDATE workspace_tag_relationships
        SET deleted_at = GETUTCDATE()
        WHERE deleted_at IS NULL
        AND (
            NOT EXISTS (SELECT 1 FROM tags WHERE id = from_tag_id AND deleted_at IS NULL)
            OR NOT EXISTS (SELECT 1 FROM tags WHERE id = to_tag_id AND deleted_at IS NULL)
            OR NOT EXISTS (SELECT 1 FROM workspaces WHERE id = workspace_id AND deleted_at IS NULL)
        );
        
        PRINT '   ✅ Removed orphaned relationships';
        
        -- Fix 4: Fix multiple default workspaces
        ;WITH RankedDefaults AS (
            SELECT 
                id,
                user_id,
                ROW_NUMBER() OVER (PARTITION BY user_id ORDER BY created_at ASC) AS rn
            FROM workspaces
            WHERE deleted_at IS NULL AND is_default = 1
        )
        UPDATE w
        SET is_default = 0
        FROM workspaces w
        INNER JOIN RankedDefaults rd ON w.id = rd.id
        WHERE rd.rn > 1;
        
        PRINT '   ✅ Fixed multiple default workspaces';
        
        -- Fix 5: Remove orphaned workspace_items
        UPDATE workspace_items
        SET deleted_at = GETUTCDATE()
        WHERE deleted_at IS NULL
        AND (
            (child_type = 'tag' AND NOT EXISTS (
                SELECT 1 FROM tags WHERE id = child_id AND deleted_at IS NULL
            ))
            OR (child_type = 'note' AND NOT EXISTS (
                SELECT 1 FROM notes WHERE id = child_id AND n.deleted_at IS NULL
            ))
        );
        
        PRINT '   ✅ Removed orphaned workspace_items';
        
        -- Fix 6: Fix note version counts
        UPDATE n
        SET version_count = (
            SELECT COUNT(*) FROM note_versions nv
            WHERE nv.note_id = n.id
        )
        FROM notes n
        WHERE n.deleted_at IS NULL;
        
        PRINT '   ✅ Fixed note version counts';
        
        -- Fix 7: Remove invalid note members (deleted users)
        UPDATE note_members
        SET deleted_at = GETUTCDATE()
        WHERE deleted_at IS NULL
        AND NOT EXISTS (
            SELECT 1 FROM users u
            WHERE u.id = note_members.user_id
            AND u.deleted_at IS NULL
        );
        
        PRINT '   ✅ Removed invalid note members';
        
        COMMIT TRANSACTION;
        PRINT '';
        PRINT '✅ All data issues fixed successfully!';
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0
            ROLLBACK TRANSACTION;
        
        PRINT '❌ Error fixing data issues:';
        PRINT ERROR_MESSAGE();
        THROW;
    END CATCH;
END;
GO

PRINT '   ✅ usp_fix_data_issues created';

GO

PRINT '✅ Data validation procedures created successfully!';
PRINT '📊 Next step: Run constraints/06.15-summary.sql';
GO