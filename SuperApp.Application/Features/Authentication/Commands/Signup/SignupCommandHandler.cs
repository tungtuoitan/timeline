using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Interfaces;
using SuperApp.Application.Common.Security;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Authentication.Commands.Signup
{
    public class SignupCommandHandler : IRequestHandler<SignupCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IJwtService _jwtService;
        private readonly ILogger<SignupCommandHandler> _logger;

        public SignupCommandHandler(
            IUserRepository userRepository,
            IJwtService jwtService,
            ILogger<SignupCommandHandler> logger)
        {
            _userRepository = userRepository;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(SignupCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing signup request for user: {Email}", request.Request.Email);

                if (string.IsNullOrEmpty(request.Request.Email))
                {
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email is required"
                    };
                }

                var existingUser = await _userRepository.GetByEmailAsync(request.Request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning("Email already registered: {Email}", request.Request.Email);
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Email is already registered"
                    };
                }

                var displayName = string.IsNullOrEmpty(request.Request.FirstName) && string.IsNullOrEmpty(request.Request.LastName)
                    ? null
                    : $"{request.Request.FirstName} {request.Request.LastName}".Trim();

                var user = new User
                {
                    Email = request.Request.Email,
                    Username = request.Request.Email,
                    PasswordHash = PasswordHasher.HashPassword(request.Request.Password),
                    DisplayName = displayName,
                    IsActive = true,
                    EmailVerified = false,
                    CreatedAt = DateTime.UtcNow
                };

                var createdUser = await _userRepository.CreateAsync(user);

                var token = _jwtService.GenerateToken(createdUser);

                _logger.LogInformation("Signup successful for user: {Email}", request.Request.Email);

                return new AuthResponse
                {
                    Success = true,
                    Message = "Sign up successfully",
                    User = new UserData
                    {
                        Id = createdUser.UserId,
                        Email = createdUser.Email,
                        FirstName = createdUser.DisplayName,
                        Token = token
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during signup for user: {Email}", request.Request.Email);
                throw;
            }
        }
    }
}
