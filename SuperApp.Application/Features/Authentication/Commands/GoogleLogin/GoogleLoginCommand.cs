using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.GoogleLogin
{
    public record GoogleLoginCommand(GoogleLoginRequest Request) : IRequest<AuthResponse>;
}