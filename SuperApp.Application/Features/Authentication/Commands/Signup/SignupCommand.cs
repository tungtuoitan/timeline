using MediatR;
using SuperAppModels.DTOs.Requests;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.Signup
{
    public record SignupCommand(SignupRequest Request) : IRequest<AuthResponse>;
}