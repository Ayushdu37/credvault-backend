namespace CreditManagement.API.Configuration;

// JWT settings for token VALIDATION only — tokens are issued by Identity Service
public class JwtSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;
}
