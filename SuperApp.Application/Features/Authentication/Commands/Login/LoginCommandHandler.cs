using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Interfaces;
using SuperApp.Application.Common.Security;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using System.Security.Authentication;

namespace SuperApp.Application.Features.Authentication.Commands.Login
{
    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<LoginCommandHandler> _logger;

        public LoginCommandHandler(
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<LoginCommandHandler> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing login request for user: {Email}", request.Request.Email);

                var user = await _userRepository.GetByEmailAsync(request.Request.Email);

                if (user == null)
                {
                    _logger.LogWarning("User not found: {Email}", request.Request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "The email you entered isn't connected to an account."
                    };
                }

                if (!user.IsActive)
                {
                    _logger.LogWarning("User account is deactivated: {Email}", request.Request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "This account has been deactivated."
                    };
                }

                if (!PasswordHasher.VerifyPassword(request.Request.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Invalid password for user: {Email}", request.Request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Password isn't correct!"
                    };
                }

                user.RecordLogin();
                await _userRepository.UpdateAsync(user);

                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Login successful for user: {Email}", request.Request.Email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Login successfully",
                    User = new UserData
                    {
                        Id = user.UserId,
                        Email = user.Email,
                        FirstName = user.DisplayName,
                        Token = token
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during login for user: {Email}", request.Request.Email);
                throw;
            }
        }
    }
}
