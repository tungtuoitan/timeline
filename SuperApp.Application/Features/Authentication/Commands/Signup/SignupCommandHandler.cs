using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Features.Authentication.Commands.Signup;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;

namespace SuperApp.Application.Features.Authentication.Commands.Signup
{
    public class SignupCommandHandler : IRequestHandler<SignupCommand, AuthResponse>
    {
        private readonly IAuthRepository _authRepository;
        private readonly ILogger<SignupCommandHandler> _logger;

        public SignupCommandHandler(
            IAuthRepository authRepository,
            ILogger<SignupCommandHandler> logger)
        {
            _authRepository = authRepository;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(SignupCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing signup request for user: {Email}", request.Request.Email);

                var result = await _authRepository.SignupAsync(request.Request);

                if (result != null && !string.IsNullOrEmpty(result.Token))
                {
                    _logger.LogInformation("Signup successful for user: {Email}", request.Request.Email);
                    return result;
                }

                _logger.LogError("Signup failed for user: {Email}", request.Request.Email);
                throw new InvalidOperationException("Failed to create user account");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during signup for user: {Email}", request.Request.Email);
                throw;
            }
        }
    }
}