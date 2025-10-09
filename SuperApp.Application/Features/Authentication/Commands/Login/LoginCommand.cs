using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.Login
{
    public record LoginCommand(LoginRequest Request) : IRequest<AuthResponse>;
}