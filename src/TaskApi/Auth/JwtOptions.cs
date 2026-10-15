using System.ComponentModel.DataAnnotations;
using System.Text;

namespace TaskApi.Auth;

public class JwtOptions : IValidatableObject
{
    public const string Section = "Jwt";

    [Required] public string Issuer { get; set; } = "TaskApi";
    [Required] public string Audience { get; set; } = "TaskApi";
    [Required] public string SigningKey { get; set; } = "";
    [Range(1, 1440)] public int LifetimeMinutes { get; set; } = 60;

    // Spec S1: refuse to start with a missing or weak signing key.
    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (Encoding.UTF8.GetByteCount(SigningKey) < 32)
            yield return new ValidationResult(
                "Jwt:SigningKey must be at least 32 bytes. Set it with the Jwt__SigningKey environment variable.",
                [nameof(SigningKey)]);
    }
}
