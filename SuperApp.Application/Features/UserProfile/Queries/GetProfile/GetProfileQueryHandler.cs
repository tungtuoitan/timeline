using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.UserProfile.Queries.GetProfile;
using SuperAppModels.DTOs.Responses;
using UserProfileDataRepositories.Ins;

namespace SuperApp.Application.Features.UserProfile.Queries.GetProfile
{
    public class GetProfileQueryHandler : IRequestHandler<GetProfileQuery, UserProfileResponse>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<GetProfileQueryHandler> _logger;

        public GetProfileQueryHandler(
            IUserProfileRepository userProfileRepository,
            IMapper mapper,
            ILogger<GetProfileQueryHandler> logger)
        {
            _userProfileRepository = userProfileRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserProfileResponse> Handle(GetProfileQuery request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Getting user profile for user: {Email}", request.UserEmail);

                var userProfile = await _userProfileRepository.GetUserProfileByEmailAsync(request.UserEmail);

                if (userProfile != null)
                {
                    var response = _mapper.Map<UserProfileResponse>(userProfile);
                    _logger.LogInformation("Successfully retrieved user profile for user: {Email}", request.UserEmail);
                    return response;
                }

                _logger.LogWarning("User profile not found for user: {Email}", request.UserEmail);
                throw new InvalidOperationException($"User profile not found for user: {request.UserEmail}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while getting user profile for user: {Email}", request.UserEmail);
                throw;
            }
        }
    }
}