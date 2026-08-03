using System.Threading.Tasks;

namespace BuildSmart.Core.Application.Interfaces;

public class EmailVerificationResult
{
    public bool IsValid { get; set; }
    public string Status { get; set; } = "Valid"; // "Valid", "InvalidSyntax", "DisposableDomain", "NoMxRecord"
    public string? ErrorMessage { get; set; }
}

public interface IEmailVerificationService
{
    Task<EmailVerificationResult> VerifyEmailAsync(string email);
}
