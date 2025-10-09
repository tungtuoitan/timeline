using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Authentication.Commands.Login;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using System.Security.Authentication;

namespace SuperApp.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IAuthRepository authRepository,
            ILogger<LoginCommandHandler> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing login request for user: {Email}", request.Request.Email);

                var result = await _authRepository.LoginAsync(request.Request);

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _logger.LogInformation("Login successful for user: {Email}", request.Request.Email);
                    return result;
                }

                _logger.LogWarning("Login failed for user: {Email}", request.Request.Email);
                throw new AuthenticationException("Invalid credentials");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for user: {Email}", request.Request.Email);
                throw;
            }
        }
    }
}