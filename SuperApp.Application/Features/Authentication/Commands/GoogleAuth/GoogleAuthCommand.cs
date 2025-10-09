using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.GoogleAuth
{
    public record GoogleAuthCommand(GoogleCodeRequest Request) : IRequest<AuthResponse>;
}