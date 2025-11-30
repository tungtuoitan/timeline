using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Workspaces.Commands.AddItemToWorkspace
{
    public class AddItemToWorkspaceCommandHandler : IRequestHandler<AddItemToWorkspaceCommand, WorkspaceItemResponse>
    {
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<AddItemToWorkspaceCommandHandler> _logger;

        public AddItemToWorkspaceCommandHandler(
            IWorkspaceRepository workspaceRepository,
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<AddItemToWorkspaceCommandHandler> logger)
        {
            _workspaceRepository = workspaceRepository;
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<WorkspaceItemResponse> Handle(AddItemToWorkspaceCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation(
                    "Adding item to workspace. WorkspaceId: {WorkspaceId}, ParentTagId: {ParentTagId}, ChildType: {ChildType}, ChildId: {ChildId}, UserId: {UserId}",
                    request.WorkspaceId, request.ParentTagId, request.ChildType, request.ChildId, request.UserId);

                // Validate user has permission (will throw UnauthorizedAccessException if not)
                await _workspaceRepository.ValidateUserAccessAsync(
                    request.WorkspaceId, 
                    request.UserId, 
                    new[] { "owner", "editor" });

                // Auto-create tag if it doesn't exist
                int actualChildId;
                if (request.ChildType.Equals("tag", StringComparison.OrdinalIgnoreCase))
                {
                    // If ChildId is provided, check if tag exists
                    if (request.ChildId.HasValue && request.ChildId.Value > 0)
                    {
                        var existingTag = await _tagRepository.GetTagById(request.ChildId.Value);
                        if (existingTag == null)
                        {
                            // Tag doesn't exist, create new one if TagName provided
                            if (string.IsNullOrWhiteSpace(request.TagName))
                            {
                                throw new ArgumentException($"Tag with ID {request.ChildId.Value} not found. Provide TagName to create a new tag.");
                            }

                            _logger.LogInformation("Tag {TagId} not found, creating new tag: {TagName}", 
                                request.ChildId.Value, request.TagName);

                            var newTag = new Tag
                            {
                                Name = request.TagName,
                                UserId = request.UserId,
                                Slug = GenerateSlug(request.TagName),
                                Color = request.Color,
                                Icon = request.Icon,
                                CreatedAt = DateTime.UtcNow,
                                UpdatedAt = DateTime.UtcNow
                            };

                            var createdTag = await _tagRepository.CreateTagAsync(newTag);
                            actualChildId = createdTag.TagId;

                            _logger.LogInformation("Created new tag with ID: {TagId}", createdTag.TagId);
                        }
                        else
                        {
                            actualChildId = existingTag.TagId;
                        }
                    }
                    else
                    {
                        // No ChildId provided, must create new tag
                        if (string.IsNullOrWhiteSpace(request.TagName))
                        {
                            throw new ArgumentException("TagName is required when creating a new tag.");
                        }

                        _logger.LogInformation("Creating new tag: {TagName}", request.TagName);

                        var newTag = new Tag
                        {
                            Name = request.TagName,
                            UserId = request.UserId,
                            Slug = GenerateSlug(request.TagName),
                            Color = request.Color,
                            Icon = request.Icon,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow
                        };

                        var createdTag = await _tagRepository.CreateTagAsync(newTag);
                        actualChildId = createdTag.TagId;

                        _logger.LogInformation("Created new tag with ID: {TagId}", createdTag.TagId);
                    }
                }
                else
                {
                    // Not a tag, use provided ChildId
                    if (!request.ChildId.HasValue || request.ChildId.Value <= 0)
                    {
                        throw new ArgumentException("ChildId is required for non-tag items.");
                    }
                    actualChildId = request.ChildId.Value;
                }

                // Create workspace item
                var workspaceItem = new WorkspaceItem
                {
                    WorkspaceId = request.WorkspaceId,
                    ParentTagId = request.ParentTagId,
                    ChildType = request.ChildType,
                    ChildId = actualChildId,  // Use actual ID (existing or newly created)
                    IsOriginal = request.IsOriginal,  // Set ownership flag
                    RelationshipType = request.RelationshipType,
                    Label = request.Label,
                    Notes = request.Notes,
                    SortOrder = request.SortOrder,
                    Color = request.Color,
                    Icon = request.Icon,
                    AddedBy = request.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                // Add item to workspace using EF Core
                var createdItem = await _workspaceRepository.AddItemToWorkspaceAsync(workspaceItem);

                var response = _mapper.Map<WorkspaceItemResponse>(createdItem);

                _logger.LogInformation(
                    "Successfully added item to workspace. ItemId: {ItemId}, WorkspaceId: {WorkspaceId}",
                    createdItem.ItemId, request.WorkspaceId);

                return response;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex,
                    "Unauthorized attempt to add item to workspace. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    request.WorkspaceId, request.UserId);
                throw;
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex,
                    "Invalid argument when adding item to workspace. WorkspaceId: {WorkspaceId}",
                    request.WorkspaceId);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Unexpected error adding item to workspace. WorkspaceId: {WorkspaceId}, UserId: {UserId}",
                    request.WorkspaceId, request.UserId);
                throw;
            }
        }

        /// <summary>
        /// Generates a unique URL-friendly slug from a tag name with timestamp suffix
        /// </summary>
        private static string GenerateSlug(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return $"tag-{DateTime.UtcNow.Ticks}";

            // Convert to lowercase
            var slug = name.ToLowerInvariant();

            // Remove special characters, keep only alphanumeric, spaces, and hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"[^a-z0-9\s-]", "");

            // Replace spaces with hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"\s+", "-");

            // Remove consecutive hyphens
            slug = System.Text.RegularExpressions.Regex.Replace(slug, @"-+", "-");

            // Trim hyphens from start and end
            slug = slug.Trim('-');

            // Add timestamp suffix to ensure uniqueness (user_id + slug must be unique)
            var timestamp = DateTime.UtcNow.Ticks;
            slug = $"{slug}-{timestamp}";

            // Limit length to 255 characters (database constraint)
            if (slug.Length > 255)
                slug = slug.Substring(0, 255).TrimEnd('-');

            return slug;
        }
    }
}
