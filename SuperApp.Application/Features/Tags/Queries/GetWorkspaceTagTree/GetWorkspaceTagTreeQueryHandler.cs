using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetWorkspaceTagTree
{
    public class GetWorkspaceTagTreeQueryHandler : IRequestHandler<GetWorkspaceTagTreeQuery, WorkspaceWithTagTreeResponse>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IWorkspaceRepository _workspaceRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetWorkspaceTagTreeQueryHandler> _logger;

        public GetWorkspaceTagTreeQueryHandler(
            ITagRepository tagRepository,
            IWorkspaceRepository workspaceRepository,
            IMapper mapper,
            ILogger<GetWorkspaceTagTreeQueryHandler> logger)
        {
            _tagRepository = tagRepository;
            _workspaceRepository = workspaceRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<WorkspaceWithTagTreeResponse> Handle(GetWorkspaceTagTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting workspace with tag tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}", 
                    request.WorkspaceId, request.UserId);

                // Get workspace info
                var workspace = await _workspaceRepository.GetWorkspaceByIdAsync(request.WorkspaceId, request.UserId);
                if (workspace == null)
                {
                    _logger.LogWarning("Workspace {WorkspaceId} not found or not accessible for UserId: {UserId}", 
                        request.WorkspaceId, request.UserId);
                    throw new InvalidOperationException($"Workspace with ID {request.WorkspaceId} not found or not accessible");
                }

                // Get tag tree for workspace
                var tagTree = await _tagRepository.GetWorkspaceTagTreeAsync(request.WorkspaceId, request.UserId);
                var flatResponse = _mapper.Map<List<TagTreeResponse>>(tagTree);

                // Build hierarchical structure for tags
                var hierarchicalTags = BuildHierarchy(flatResponse);

                // Map workspace to response and include tag tree
                var response = new WorkspaceWithTagTreeResponse
                {
                    WorkspaceId = workspace.WorkspaceId,
                    UserId = workspace.UserId,
                    Name = workspace.Name,
                    Description = workspace.Description,
                    Color = workspace.Color,
                    Icon = workspace.Icon,
                    Type = workspace.Type,
                    MaxDepth = workspace.MaxDepth,
                    IsDefault = workspace.IsDefault,
                    IsPublic = workspace.IsPublic,
                    IsTemplate = workspace.IsTemplate,
                    IsArchived = workspace.IsArchived,
                    TagCount = workspace.TagCount,
                    MemberCount = workspace.MemberCount,
                    Settings = workspace.Settings,
                    CreatedAt = workspace.CreatedAt ?? DateTime.UtcNow,
                    UpdatedAt = workspace.UpdatedAt,
                    Tags = hierarchicalTags
                };

                _logger.LogInformation("Successfully retrieved workspace with {RootTagCount} root tags for WorkspaceId: {WorkspaceId}", 
                    hierarchicalTags.Count, request.WorkspaceId);
                
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting workspace tag tree for WorkspaceId: {WorkspaceId}", request.WorkspaceId);
                throw;
            }
        }

        /// <summary>
        /// Builds a hierarchical tree structure from flat list of tags
        /// </summary>
        private List<TagTreeResponse> BuildHierarchy(List<TagTreeResponse> flatTags)
        {
            var tagDict = flatTags.ToDictionary(t => t.TagId, t => t);
            var rootTags = new List<TagTreeResponse>();

            foreach (var tag in flatTags)
            {
                if (tag.ParentId == null)
                {
                    // Root tag
                    rootTags.Add(tag);
                }
                else if (tagDict.TryGetValue(tag.ParentId.Value, out var parent))
                {
                    // Child tag - add to parent's children
                    parent.Children.Add(tag);
                }
                // If parent not found, treat as root (shouldn't happen with proper data)
                else
                {
                    rootTags.Add(tag);
                }
            }

            // Sort children recursively
            SortTagsRecursively(rootTags);
            return rootTags;
        }

        /// <summary>
        /// Sorts tags and their children alphabetically
        /// </summary>
        private void SortTagsRecursively(List<TagTreeResponse> tags)
        {
            tags.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            
            foreach (var tag in tags)
            {
                if (tag.Children.Any())
                {
                    SortTagsRecursively(tag.Children);
                }
            }
        }
    }
}