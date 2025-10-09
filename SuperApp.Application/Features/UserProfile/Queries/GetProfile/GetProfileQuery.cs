using MediatR;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.UserProfile.Queries.GetProfile
{
    public record GetProfileQuery(string UserEmail) : IRequest<UserProfileResponse>;
}