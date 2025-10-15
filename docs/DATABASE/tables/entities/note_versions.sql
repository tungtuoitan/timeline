-- ============================================
-- FILE: tables/entities/note_versions.sql
-- PURPOSE: Version history for notes with full content snapshots
-- DEPENDENCIES: notes.sql, users.sql
-- ============================================

PRINT '';
PRINT '📦 Creating table: note_versions';
PRINT '   Purpose: Version history for notes';
PRINT '   Scale: ~10 versions per note on average';
PRINT '   Storage: Full content snapshots';
PRINT '';

-- ============================================
-- TABLE: note_versions
-- PURPOSE: Version history for notes
-- SCALE: ~10 versions per note on average (~20M rows if 2M notes)
-- ============================================

CREATE TABLE note_versions (
    id BIGINT IDENTITY(1,1) PRIMARY KEY,
    
    -- Note reference
    note_id INT NOT NULL,
    
    -- Version info
    version_number INT NOT NULL, -- 1, 2, 3, ...
    
    -- Content snapshot
    name NVARCHAR(200) NOT NULL,
    description NVARCHAR(MAX),
    content NVARCHAR(MAX), -- Full content snapshot
    
    -- Metadata snapshot
    word_count INT DEFAULT 0,
    
    -- Change tracking
    change_summary NVARCHAR(500), -- Brief description of changes
    created_by INT NOT NULL, -- Who created this version
    
    -- Timestamps
    created_at DATETIME2 DEFAULT GETUTCDATE(),
    
    -- Constraints
    CONSTRAINT fk_noteversions_note FOREIGN KEY (note_id) 
        REFERENCES notes(id) ON DELETE CASCADE,
    
    CONSTRAINT fk_noteversions_user FOREIGN KEY (created_by) 
        REFERENCES users(id),
    
    CONSTRAINT ck_noteversions_version_positive CHECK (
        version_number > 0
    ),
    
    -- Unique: Each note can only have one version with same number
    CONSTRAINT uq_noteversions_number UNIQUE (note_id, version_number)
);

-- Indexes for note_versions

-- Primary lookup: Version history for a note
CREATE INDEX ix_noteversions_note ON note_versions(
    note_id, version_number DESC
)
INCLUDE (name, created_at, created_by);

-- User activity: Versions created by user
CREATE INDEX ix_noteversions_user ON note_versions(
    created_by, created_at DESC
);

-- Comments
EXEC sys.sp_addextendedproperty 
    @name = N'MS_Description',
    @value = N'Version history for notes. Stores full content snapshots.',
    @level0type = N'SCHEMA', @level0name = N'dbo',
    @level1type = N'TABLE', @level1name = N'note_versions';

PRINT '   ✅ note_versions table created';
PRINT '   ✅ 2 indexes created (note, user)';
GO

-- ============================================
-- VALIDATION
-- ============================================

PRINT '';
PRINT '📊 Verifying note_versions...';

SELECT 
    name AS index_name,
    type_desc,
    is_unique
FROM sys.indexes
WHERE object_id = OBJECT_ID('note_versions')
AND name IS NOT NULL
ORDER BY name;

GO

-- ============================================
-- SUCCESS MESSAGE
-- ============================================

PRINT '';
PRINT '✅ note_versions table created successfully!';
PRINT '';
PRINT '📚 KEY FEATURES:';
PRINT '   - Full content snapshots for each version';
PRINT '   - Sequential version numbering (1, 2, 3...)';
PRINT '   - Tracks who created each version';
PRINT '   - Optional change summary field';
PRINT '   - CASCADE delete with parent note';
PRINT '';
PRINT '🔢 VERSION NUMBERING:';
PRINT '   - Enforced by trigger on notes table (tr_notes_create_version)';
PRINT '   - Race condition protection with TABLOCKX';
PRINT '   - Automatic increment on note update';
PRINT '';
PRINT '📈 SCALE ESTIMATE:';
PRINT '   - ~10 versions per note average';
PRINT '   - ~20M rows if 2M notes';
PRINT '   - Index on (note_id, version_number DESC) for fast retrieval';
PRINT '';
PRINT '💡 USAGE:';
PRINT '   - Versions auto-created by tr_notes_create_version trigger';
PRINT '   - Retrieve history: SELECT * FROM note_versions WHERE note_id = ? ORDER BY version_number DESC';
PRINT '   - Compare versions: Join versions with different version_numbers';
PRINT '';
GO
