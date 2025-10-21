using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Tags.Queries.GetTagTree
{
    public class GetTagTreeQueryHandler : IRequestHandler<GetTagTreeQuery, List<TagTreeResponse>>
    {
        private readonly ITagRepository _tagRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetTagTreeQueryHandler> _logger;

        public GetTagTreeQueryHandler(
            ITagRepository tagRepository,
            IMapper mapper,
            ILogger<GetTagTreeQueryHandler> logger)
        {
            _tagRepository = tagRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<List<TagTreeResponse>> Handle(GetTagTreeQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting tag tree for WorkspaceId: {WorkspaceId}, UserId: {UserId}", 
                    request.WorkspaceId, request.UserId);

                var tagTree = await _tagRepository.GetTagTreeAsync(request.WorkspaceId, request.UserId);
                var flatResponse = _mapper.Map<List<TagTreeResponse>>(tagTree);

                // Build hierarchical structure
                var hierarchicalResponse = BuildHierarchy(flatResponse);

                _logger.LogInformation("Successfully retrieved {Count} root tags with hierarchy", hierarchicalResponse.Count);
                return hierarchicalResponse;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting tag tree");
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