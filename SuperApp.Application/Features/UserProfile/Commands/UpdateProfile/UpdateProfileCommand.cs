using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.UserProfile.Commands.UpdateProfile
{
    public record UpdateProfileCommand(UpdateUserProfileRequest Request, string UserEmail) : IRequest<UserProfileResponse>;
}