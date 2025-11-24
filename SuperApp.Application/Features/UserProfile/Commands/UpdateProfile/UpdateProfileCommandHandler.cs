using AutoMapper;
using MediatR;
using Microsoft.Extensions.Logging;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.UserProfile.Commands.UpdateProfile
{
    public class UpdateProfileCommandHandler : IRequestHandler<UpdateProfileCommand, UserProfileResponse>
    {
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IMapper _mapper;
        private readonly ILogger<UpdateProfileCommandHandler> _logger;

        public UpdateProfileCommandHandler(
            IUserProfileRepository userProfileRepository,
            IMapper mapper,
            ILogger<UpdateProfileCommandHandler> logger)
        {
            _userProfileRepository = userProfileRepository;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<UserProfileResponse> Handle(UpdateProfileCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Updating user profile for user: {Email}", request.UserEmail);

                var userProfile = _mapper.Map<SuperAppModels.Models.UserProfile>(request.Request);
                userProfile.Email = request.UserEmail;

                var result = await _userProfileRepository.CreateOrUpdateAsync(userProfile);

                var response = _mapper.Map<UserProfileResponse>(result);
                _logger.LogInformation("Successfully updated user profile for user: {Email}", request.UserEmail);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user profile for user: {Email}", request.UserEmail);
                throw;
            }
        }
    }
}
