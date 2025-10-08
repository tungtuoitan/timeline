namespace SuperAppModels.DTOs.Responses
{
    public class AuthResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserData? User { get; set; }
    }

    public class UserData
    {
        public int Id { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string Token { get; set; } = string.Empty;
    }
}
