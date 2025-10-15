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
                SELECT 1 FROM notes WHERE id = child_id AND deleted_at IS NULL
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

-- ============================================
-- SUCCESS MESSAGE & SUMMARY
-- ============================================

PRINT '';
PRINT '✅ All constraints and validations created successfully!';
PRINT '';
PRINT '📊 CONSTRAINT SUMMARY (Unified System):';
PRINT '   Section 1: Users - 4 constraints';
PRINT '   Section 2: Tags - 6 constraints';
PRINT '   Section 3: Workspaces - 4 constraints';
PRINT '   Section 4: Workspace Members - 2 constraints';
PRINT '   Section 5: Relationship Types - 4 constraints';
PRINT '   Section 6: workspace_items (Unified) - 6 constraints';
PRINT '   Section 7: entity_types - 4 constraints';
PRINT '   Section 8: notes - 7 constraints';
PRINT '   Section 9: note_members - 5 constraints';
PRINT '   Section 10: note_versions - 5 constraints';
PRINT '   Section 11: Deprecated relationships (reference only)';
PRINT '   Section 12: Business Logic - 3 triggers';
PRINT '   Section 13: Audit - 1 table + 1 trigger';
PRINT '   Section 14: Validation - 2 procedures';
PRINT '';
PRINT '🎯 KEY VALIDATIONS:';
PRINT '   ✓ User email, username, password format validation';
PRINT '   ✓ Tag name, color, slug format validation';
PRINT '   ✓ Workspace name, statistics validation';
PRINT '   ✓ workspace_items path, depth, child_type validation';
PRINT '   ✓ entity_types name, icon, color format validation';
PRINT '   ✓ Notes name, slug, word count validation';
PRINT '   ✓ note_members role (owner/editor/viewer) validation';
PRINT '   ✓ note_versions content snapshot requirement';
PRINT '   ✓ Business logic triggers (single default workspace, owner membership)';
PRINT '';
PRINT '🛠️  VALIDATION PROCEDURES:';
PRINT '   - usp_validate_data_integrity (13 checks including unified system)';
PRINT '   - usp_fix_data_issues (7 fixes including notes & workspace_items)';
PRINT '';
PRINT '📋 VALIDATION CHECKS:';
PRINT '   1. Orphaned tags (no workspace references)';
PRINT '   2. Incorrect tag usage counts';
PRINT '   3. Duplicate tag names per user';
PRINT '   4. Workspaces without owners';
PRINT '   5. Incorrect workspace statistics';
PRINT '   6. Multiple default workspaces';
PRINT '   7. Invalid paths in deprecated table';
PRINT '   8. workspace_items with invalid child_type';
PRINT '   9. Orphaned workspace_items';
PRINT '   10. Notes without owner';
PRINT '   11. Incorrect note version counts';
PRINT '   12. Note versions missing content';
PRINT '   13. Invalid note members';
PRINT '';
PRINT '🔧 MAINTENANCE SCHEDULE:';
PRINT '   - Run usp_validate_data_integrity: Weekly';
PRINT '   - Run usp_fix_data_issues: When validation finds issues';
PRINT '   - Review audit_log: As needed';
PRINT '';
PRINT '📊 Next step: Run 08-procedures-workspace.sql (verify compatibility)';
GO
