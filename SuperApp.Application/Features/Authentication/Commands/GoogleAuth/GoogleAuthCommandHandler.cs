using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Authentication.Commands.GoogleAuth;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.GoogleAuth
{
    public class GoogleAuthCommandHandler : IRequestHandler<GoogleAuthCommand, AuthResponse>
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<GoogleAuthCommandHandler> _logger;

        public GoogleAuthCommandHandler(
            IAuthRepository authRepository,
            ILogger<GoogleAuthCommandHandler> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing Google authentication with code: {CodeLength} characters", 
                    request.Request.Code?.Length ?? 0);

                var result = await _authRepository.GoogleAuthAsync(request.Request);

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _logger.LogInformation("Google authentication successful");
                    return result;
                }

                _logger.LogWarning("Google authentication failed");
                throw new InvalidOperationException("Google authentication failed");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during Google authentication");
                throw;
            }
        }
    }
}