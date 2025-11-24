using MediatR;
using Microsoft.Extensions.Logging;
using SuperApp.Application.Common.Interfaces;
using SuperAppDataRepositories.Ins;
using SuperAppModels.DTOs.Responses;
using SuperAppModels.Models;

namespace SuperApp.Application.Features.Authentication.Commands.GoogleAuth
{
    public class GoogleAuthCommandHandler : IRequestHandler<GoogleAuthCommand, AuthResponse>
    {
        private readonly IUserRepository _userRepository;
        private readonly IGoogleAuthService _googleAuthService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<GoogleAuthCommandHandler> _logger;

        public GoogleAuthCommandHandler(
            IUserRepository userRepository,
            IGoogleAuthService googleAuthService,
            IJwtService jwtService,
            ILogger<GoogleAuthCommandHandler> logger)
        {
            _userRepository = userRepository;
            _googleAuthService = googleAuthService;
            _jwtService = jwtService;
            _logger = logger;
        }

        public async Task<AuthResponse> Handle(GoogleAuthCommand request, CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Processing Google authentication");

                var googleUser = await _googleAuthService.VerifyGoogleTokenAsync(request.Request.Code);

                if (googleUser == null || string.IsNullOrEmpty(googleUser.Email))
                {
                    _logger.LogWarning("Failed to verify Google token");
                    return new AuthResponse
                    {
                        Success = false,
                        Message = "Failed to verify Google authentication"
                    };
                }

                var user = await _userRepository.GetByEmailAsync(googleUser.Email);

                if (user == null)
                {
                    user = new User
                    {
                        Email = googleUser.Email,
                        Username = googleUser.Email,
                        PasswordHash = string.Empty,
                        DisplayName = googleUser.Name ?? $"{googleUser.GivenName} {googleUser.FamilyName}".Trim(),
                        AvatarUrl = googleUser.Picture,
                        IsActive = true,
                        EmailVerified = googleUser.EmailVerified,
                        CreatedAt = DateTime.UtcNow
                    };

                    user = await _userRepository.CreateAsync(user);
                    _logger.LogInformation("Created new user from Google auth: {Email}", googleUser.Email);
                }
                else
                {
                    user.RecordLogin();
                    await _userRepository.UpdateAsync(user);
                }

                var token = _jwtService.GenerateToken(user);

                _logger.LogInformation("Google authentication successful for: {Email}", googleUser.Email);

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
                _logger.LogError(ex, "Error occurred during Google authentication");
                throw;
            }
        }
    }
}
