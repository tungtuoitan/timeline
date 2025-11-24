namespace SuperAppDataRepositories
{
    /// <summary>
    /// Centralized repository for all stored procedure names with consistent naming conventions
    /// </summary>
    public static class StoredProcedures
    {
        // ============================================================================
        // CORE ENTITY OPERATIONS (Legacy - for backward compatibility)
        // ============================================================================
        
        // Events (Legacy)
        public static string spSelectEvs => "[dbo].[usp_s_Evs]";
        public static string spInsertUpdateEv => "[dbo].[usp_iu_Ev]";
        
        // Standard Registries
        public static string spSelectStandardRegistries => "[dbo].[usp_s_SRs]";
        public static string spSelectStandardRegistryById => "[dbo].[usp_s_SRById]";
        public static string spSelectStandardRegistryByKey => "[dbo].[usp_s_SRByKey]";
        public static string spInsertUpdateStandardRegistry => "[dbo].[usp_iu_SR]";
        public static string spDeleteStandardRegistry => "[dbo].[usp_d_SR]";
        
        // ============================================================================
        // NOTES MANAGEMENT
        // ============================================================================
        // REFACTORED: Note operations now use EF Core
        // Removed: usp_s_Notes, usp_s_NoteById, usp_iu_Note, usp_d_Note
        
        // ============================================================================
        // USER PROFILE MANAGEMENT
        // ============================================================================
        
        public static string spSelectUserProfileJson => "[dbo].[usp_s_UserProfileJson]";
        public static string spInsertUpdateUserProfile => "[dbo].[usp_iu_UserProfile]";
        public static string spSelectPrParentIdList => "[dbo].[usp_s_PrParentIdList]";
        
        // ============================================================================
        // ADVANCED TAG SYSTEM
        // ============================================================================
        
        // Basic Tag Operations
        public static string spSelectUserTags => "[dbo].[usp_s_user_tags]";
        public static string spInsertTag => "[dbo].[usp_i_tag]";
        public static string spUpdateTag => "[dbo].[usp_u_tag]";
        public static string spDeleteTagAdvanced => "[dbo].[usp_d_tag]";
        
        // Tag Hierarchy & Navigation
        // KEPT: usp_s_tag_tree (used by GetTagTreeAsync, GetWorkspaceTagTreeAsync)
        public static string spSelectTagTree => "[dbo].[usp_s_tag_tree]";
        public static string spSelectTagTreeWithSharing => "[dbo].[usp_s_tag_tree]";
        public static string spSelectWorkspaceTagTree => "[dbo].[usp_s_tag_tree]";
        // REFACTORED to EF Core: usp_move_item (MoveWorkspaceItemAsync)
        // Removed: usp_s_tag_subtree, usp_s_tag_breadcrumb, usp_move_tag, usp_replace_tag
        
        // Tag Search & Discovery
        public static string spSearchTags => "[dbo].[usp_search_tags]";
        
        // Tag Templates & Bulk Operations
        public static string spInsertTagTemplate => "[dbo].[usp_i_tag_template]";
        public static string spApplyTagTemplate => "[dbo].[usp_apply_tag_template]";
        public static string spBulkTagItems => "[dbo].[usp_bulk_tag_items]";
        
        // Tag Sharing & Collaboration
        public static string spSelectSharedTags => "[dbo].[usp_s_shared_tags]";
        public static string spSelectTagShares => "[dbo].[usp_s_tag_shares]";
        public static string spShareTag => "[dbo].[usp_share_tag]";
        public static string spShareTagWithGroup => "[dbo].[usp_share_tag_with_group]";
        public static string spUpdateTagShare => "[dbo].[usp_u_tag_share]";
        public static string spRevokeTagShare => "[dbo].[usp_revoke_tag_share]";
        
        // Tag Recovery & Maintenance
        public static string spRestoreTag => "[dbo].[usp_restore_tag]";
        
        // ============================================================================
        // WORKSPACE MANAGEMENT
        // ============================================================================
        // REFACTORED: Workspace operations now use EF Core
        // KEPT: usp_update_workspace_item (UpdateWorkspaceItemAsync)
        // Removed: usp_s_workspace, usp_s_user_workspaces, usp_i_workspace, usp_u_workspace, usp_d_workspace, usp_move_item
        
        // ============================================================================
        // ITEM TAGGING SYSTEM
        // ============================================================================
        
        // Tagging Items (Universal tagging system)
        public static string spTagItem => "[dbo].[usp_tag_item]";
        public static string spUntagItem => "[dbo].[usp_untag_item]";
        public static string spSelectTaggedItems => "[dbo].[sp_s_tagged_items]";
        
        // ============================================================================
        // LEGACY TAG SYSTEM (for backward compatibility)
        // ============================================================================
        
        // Legacy Tag CRUD
        public static string spSelectTags => "[dbo].[usp_s_Tags]";
        public static string spSelectTagsWithHierarchy => "[dbo].[usp_s_tags]";
        public static string spSelectTagById => "[dbo].[usp_s_TagById]";
        public static string spInsertUpdateTag => "[dbo].[usp_iu_Tag]";
        public static string spDeleteTag => "[dbo].[usp_d_Tag]";
        
        // Legacy Taggables (Junction table system)
        public static string spInsertTaggable => "[dbo].[usp_i_Taggable]";
        public static string spDeleteTaggable => "[dbo].[usp_d_Taggable]";
        public static string spDeleteTaggablesByEntity => "[dbo].[usp_d_TaggablesByEntity]";
        public static string spSelectTaggablesByEntity => "[dbo].[usp_s_TaggablesByEntity]";
        
        // Legacy Note-Tag relationships
        // REFACTORED: Note-Tag operations now use EF Core
        // Removed: usp_s_NoteTagsByNoteId, usp_s_NoteTagsByTagId, usp_i_NoteTag, usp_d_NoteTag, usp_d_NoteTagsByNoteId
        
        // ============================================================================
        // STORED PROCEDURE NAMING CONVENTIONS
        // ============================================================================
        
        /* 
         * NAMING CONVENTION GUIDE:
         * 
         * Prefix: usp_ (user stored procedure)
         * Operations:
         *   - s_   = Select (read operations)
         *   - i_   = Insert (create operations)  
         *   - u_   = Update (update operations)
         *   - d_   = Delete (delete operations)
         *   - iu_  = Insert/Update (upsert operations)
         * 
         * Entity Names:
         *   - Use full entity names for clarity
         *   - Use singular form (Tag, Note, User)
         *   - Use camelCase for multi-word entities (UserProfile, StandardRegistry)
         * 
         * Specialized Operations:
         *   - Use descriptive verbs (move, replace, restore, share, revoke)
         *   - Include context when needed (ByEntity, ByGroup, WithGroup)
         * 
         * Examples:
         *   - usp_s_user_tags          (Select user's tags)
         *   - usp_i_tag                (Insert new tag)
         *   - usp_u_tag_share          (Update tag sharing settings)
         *   - usp_move_tag             (Move tag in hierarchy)
         *   - usp_share_tag_with_group (Share tag with user group)
         */
    }
}
