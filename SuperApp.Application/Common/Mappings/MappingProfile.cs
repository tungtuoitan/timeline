using AutoMapper;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Common.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Note mappings
            CreateMap<Note, NoteResponse>();
                // Tags navigation property not available - will be handled separately
                // Type and CreatedBy properties not available in current Note model
            
            CreateMap<CreateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Description)) // Map Description to Content
                // .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)) // Property not available
                // .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => false));
                // .ForMember(dest => dest.Tags, opt => opt.Ignore()); // Navigation property not available
            
            CreateMap<UpdateNoteRequest, Note>()
                .ForMember(dest => dest.NoteId, opt => opt.MapFrom(src => src.NoteId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Content, opt => opt.MapFrom(src => src.Description)) // Map Description to Content
                // .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type)) // Property not available
                // .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsArchived, opt => opt.Ignore());
                // .ForMember(dest => dest.Tags, opt => opt.Ignore()); // Navigation property not available

            // User mappings
            CreateMap<User, UserResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId)) // Map UserId to Id
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                // .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.Phone)) // Property not available
                // .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName)) // Property not available
                // .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName)) // Property not available
                // .ForMember(dest => dest.Birthday, opt => opt.MapFrom(src => src.Birthday)) // Property not available
                // .ForMember(dest => dest.AuthenticationType, opt => opt.MapFrom(src => src.AuthenticationType)) // Property not available
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive));
                // .ForMember(dest => dest.IsEmailVerified, opt => opt.MapFrom(src => src.IsEmailVerified)); // Property not available

            // UserProfile mappings
            CreateMap<UserProfile, UserProfileResponse>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AppC, opt => opt.MapFrom(src => src.AppC))
                .ForMember(dest => dest.Parents, opt => opt.MapFrom(src => src.Parents))
                .ForMember(dest => dest.Priorities, opt => opt.MapFrom(src => src.Priorities))
                .ForMember(dest => dest.Statuses, opt => opt.MapFrom(src => src.Statuses))
                .ForMember(dest => dest.Types, opt => opt.MapFrom(src => src.Types))
                .ForMember(dest => dest.RepeatTypes, opt => opt.MapFrom(src => src.RepeatTypes))
                .ForMember(dest => dest.IsUpdatedTodays, opt => opt.MapFrom(src => src.IsUpdatedTodays))
                .ForMember(dest => dest.LastUpdated, opt => opt.MapFrom(src => src.UpdatedAt));

            CreateMap<UpdateUserProfileRequest, UserProfile>()
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
                .ForMember(dest => dest.AppC, opt => opt.MapFrom(src => src.AppC))
                .ForMember(dest => dest.Parents, opt => opt.MapFrom(src => src.Parents))
                .ForMember(dest => dest.Priorities, opt => opt.MapFrom(src => src.Priorities))
                .ForMember(dest => dest.Statuses, opt => opt.MapFrom(src => src.Statuses))
                .ForMember(dest => dest.Types, opt => opt.MapFrom(src => src.Types))
                .ForMember(dest => dest.RepeatTypes, opt => opt.MapFrom(src => src.RepeatTypes))
                .ForMember(dest => dest.IsUpdatedTodays, opt => opt.MapFrom(src => src.IsUpdatedTodays))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.Ignore());

            // Tag mappings
            CreateMap<Tag, TagResponse>()
                .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.TagId)) // Use TagId instead of Id
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt ?? DateTime.UtcNow))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.DeletedAt == null));
                // .ForMember(dest => dest.Depth, opt => opt.MapFrom(src => src.Depth)); // Property not available
                // CreatedBy and UpdatedAt excluded for security and simplicity

            CreateMap<CreateTagRequest, Tag>()
                .ForMember(dest => dest.TagId, opt => opt.Ignore()) // Use TagId instead of Id
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                // .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId)) // Property not available
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                // .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => src.IsPublic)) // Property not available
                // .ForMember(dest => dest.PublicSlug, opt => opt.MapFrom(src => src.PublicSlug)) // Property not available
                // .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                // .ForMember(dest => dest.Path, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());
                // .ForMember(dest => dest.Depth, opt => opt.Ignore()); // Property not available

            CreateMap<UpdateTagRequest, Tag>()
                .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.TagId)) // Use TagId instead of Id
                .ForMember(dest => dest.UserId, opt => opt.Ignore())
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                // .ForMember(dest => dest.CreatedBy, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
                // .ForMember(dest => dest.IsPublic, opt => opt.Ignore()) // Property not available
                // .ForMember(dest => dest.ParentId, opt => opt.Ignore()) // Property not available
                // .ForMember(dest => dest.Path, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.Slug, opt => opt.Ignore())
                .ForMember(dest => dest.Icon, opt => opt.Ignore())
                // .ForMember(dest => dest.PublicSlug, opt => opt.Ignore()) // Property not available
                .ForMember(dest => dest.DeletedAt, opt => opt.Ignore());

            // TagTree mappings
            CreateMap<TagTree, TagTreeResponse>()
                .ForMember(dest => dest.TagId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.Path, opt => opt.MapFrom(src => src.Path))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                .ForMember(dest => dest.AccessType, opt => opt.MapFrom(src => src.AccessType))
                .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.Level))
                .ForMember(dest => dest.UsageCount, opt => opt.MapFrom(src => src.UsageCount))
                .ForMember(dest => dest.ChildrenCount, opt => opt.MapFrom(src => src.ChildrenCount))
                .ForMember(dest => dest.Children, opt => opt.Ignore()) // Will be populated by business logic
                .ForMember(dest => dest.IsExpanded, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsSelected, opt => opt.MapFrom(src => false));

            // StandardRegistry mappings
            CreateMap<StandardRegistry, StandardRegistryResponse>()
                .ForMember(dest => dest.Key, opt => opt.MapFrom(src => src.Code))
                .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // ========================================
            // WORKSPACE TREE MAPPINGS (Phase 3.4)
            // Support for unified tree with tags, notes, AND files
            // ========================================

            // WorkspaceTreeItem (database model) → WorkspaceTreeItemResponse (DTO)
            // This is the PRIMARY mapping used by GetWorkspaceTreeAsync
            CreateMap<WorkspaceTreeItem, WorkspaceTreeItemResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.ItemId))         // workspace_items.item_id
                .ForMember(dest => dest.ItemId, opt => opt.MapFrom(src => src.ItemId))     // workspace_items.item_id
                .ForMember(dest => dest.ChildId, opt => opt.MapFrom(src => src.ChildId))   // actual tag_id/note_id
                .ForMember(dest => dest.ItemType, opt => opt.MapFrom(src => src.ItemType))
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ParentId, opt => opt.MapFrom(src => src.ParentId))
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                .ForMember(dest => dest.AccessType, opt => opt.MapFrom(src => src.AccessType))
                .ForMember(dest => dest.IsOriginal, opt => opt.MapFrom(src => src.IsOriginal))
                .ForMember(dest => dest.Level, opt => opt.MapFrom(src => src.Level))
                .ForMember(dest => dest.Position, opt => opt.MapFrom(src => src.Position))
                .ForMember(dest => dest.SortOrder, opt => opt.MapFrom(src => src.Position)) // Alias
                .ForMember(dest => dest.Depth, opt => opt.MapFrom(src => src.Level)) // Alias
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom((src, dest, destMember, context) => 
                {
                    // Deserialize MetadataJson if present
                    if (string.IsNullOrEmpty(src.MetadataJson))
                        return null;
                    
                    try
                    {
                        return System.Text.Json.JsonSerializer.Deserialize<object>(src.MetadataJson, 
                            new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    }
                    catch
                    {
                        return null;
                    }
                }))
                .ForMember(dest => dest.Children, opt => opt.Ignore()) // Populated during tree building
                .ForMember(dest => dest.IsExpanded, opt => opt.MapFrom(src => false))
                .ForMember(dest => dest.IsSelected, opt => opt.MapFrom(src => false));

            // Tag → WorkspaceTreeItemResponse
            CreateMap<Tag, WorkspaceTreeItemResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.TagId))
                .ForMember(dest => dest.ItemType, opt => opt.MapFrom(src => "tag"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ParentId, opt => opt.Ignore()) // ⚠️ Tags don't have ParentId in MVP 1.1, relationship via WorkspaceItem
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.SortOrder, opt => opt.Ignore()) // Set from WorkspaceItem
                .ForMember(dest => dest.Depth, opt => opt.Ignore()) // Computed during tree building
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => new TagMetadata
                {
                    Color = src.Color,
                    Icon = src.Icon,
                    Description = src.Description,
                    UsageCount = 0, // Populated separately if needed
                    IsPublic = false, // ⚠️ Default to false, is_public not in MVP 1.1
                    CreatedAt = src.CreatedAt ?? DateTime.UtcNow
                }))
                .ForMember(dest => dest.Children, opt => opt.Ignore()); // Populated during tree building

            // Note → WorkspaceTreeItemResponse
            CreateMap<Note, WorkspaceTreeItemResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.NoteId))
                .ForMember(dest => dest.ItemType, opt => opt.MapFrom(src => "note"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ParentId, opt => opt.Ignore()) // Notes don't have ParentId, relationship via WorkspaceItem
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.SortOrder, opt => opt.Ignore()) // Set from WorkspaceItem
                .ForMember(dest => dest.Depth, opt => opt.Ignore()) // Computed during tree building
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => new NoteMetadata
                {
                    ContentPreview = !string.IsNullOrEmpty(src.Content) && src.Content.Length > 200
                        ? src.Content.Substring(0, 200) + "..."
                        : src.Content,
                    IsArchived = src.IsArchived,
                    IsPinned = src.IsPinned,
                    IsFavorite = src.IsFavorite,
                    CreatedAt = src.CreatedAt,
                    UpdatedAt = src.UpdatedAt
                }))
                .ForMember(dest => dest.Children, opt => opt.Ignore()); // Notes are leaf nodes (no children)

            // FileInfo → WorkspaceTreeItemResponse
            CreateMap<SuperAppModels.Models.FileInfo, WorkspaceTreeItemResponse>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.FileId))
                .ForMember(dest => dest.ItemType, opt => opt.MapFrom(src => "file"))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name))
                .ForMember(dest => dest.ParentId, opt => opt.Ignore()) // Files don't have ParentId, relationship via WorkspaceItem
                .ForMember(dest => dest.Slug, opt => opt.MapFrom(src => src.Slug))
                .ForMember(dest => dest.SortOrder, opt => opt.Ignore()) // Set from WorkspaceItem
                .ForMember(dest => dest.Depth, opt => opt.Ignore()) // Computed during tree building
                .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => new FileMetadata
                {
                    OriginalFilename = src.OriginalFilename,
                    Size = src.FileSize,
                    MimeType = src.MimeType,
                    StoragePath = src.FilePath,
                    BlobUrl = null,
                    BlobContainerName = null,
                    Description = src.Description,
                    CreatedAt = src.CreatedAt,
                    UpdatedAt = src.UpdatedAt
                }))
                .ForMember(dest => dest.Children, opt => opt.Ignore()); // Files are leaf nodes (no children)

            // Tag → TagMetadata (for detailed metadata extraction)
            CreateMap<Tag, TagMetadata>()
                .ForMember(dest => dest.Color, opt => opt.MapFrom(src => src.Color))
                .ForMember(dest => dest.Icon, opt => opt.MapFrom(src => src.Icon))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.UsageCount, opt => opt.Ignore()) // Populated separately
                .ForMember(dest => dest.IsPublic, opt => opt.MapFrom(src => false)) // ⚠️ Default to false, is_public not in MVP 1.1
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt ?? DateTime.UtcNow));

            // Note → NoteMetadata (for detailed metadata extraction)
            CreateMap<Note, NoteMetadata>()
                .ForMember(dest => dest.ContentPreview, opt => opt.MapFrom(src =>
                    !string.IsNullOrEmpty(src.Content) && src.Content.Length > 200
                        ? src.Content.Substring(0, 200) + "..."
                        : src.Content))
                .ForMember(dest => dest.IsArchived, opt => opt.MapFrom(src => src.IsArchived))
                .ForMember(dest => dest.IsPinned, opt => opt.MapFrom(src => src.IsPinned))
                .ForMember(dest => dest.IsFavorite, opt => opt.MapFrom(src => src.IsFavorite))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));

            // FileInfo → FileMetadata (for detailed metadata extraction)
            CreateMap<SuperAppModels.Models.FileInfo, FileMetadata>()
                .ForMember(dest => dest.OriginalFilename, opt => opt.MapFrom(src => src.OriginalFilename))
                .ForMember(dest => dest.Size, opt => opt.MapFrom(src => src.FileSize))
                .ForMember(dest => dest.MimeType, opt => opt.MapFrom(src => src.MimeType))
                .ForMember(dest => dest.StoragePath, opt => opt.MapFrom(src => src.FilePath))
                .ForMember(dest => dest.BlobUrl, opt => opt.MapFrom(src => (string?)null))
                .ForMember(dest => dest.BlobContainerName, opt => opt.MapFrom(src => (string?)null))
                .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
                .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
                .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => src.UpdatedAt));
        }
    }
}