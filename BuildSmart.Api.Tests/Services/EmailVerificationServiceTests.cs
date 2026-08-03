using System.Threading.Tasks;
using BuildSmart.Infrastructure.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class EmailVerificationServiceTests
{
    private readonly EmailVerificationService _service;

    public EmailVerificationServiceTests()
    {
        _service = new EmailVerificationService(NullLogger<EmailVerificationService>.Instance);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not-an-email")]
    [InlineData("@domain.com")]
    [InlineData("user@")]
    public async Task VerifyEmailAsync_ShouldReturnInvalidSyntax_ForInvalidFormats(string input)
    {
        var result = await _service.VerifyEmailAsync(input);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("InvalidSyntax");
    }

    [Theory]
    [InlineData("user@tempmail.com")]
    [InlineData("test@mailinator.com")]
    [InlineData("john@10minutemail.com")]
    [InlineData("dummy@trashmail.com")]
    [InlineData("fake@guerrillamail.com")]
    public async Task VerifyEmailAsync_ShouldReject_DisposableDomains(string disposableEmail)
    {
        var result = await _service.VerifyEmailAsync(disposableEmail);

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("DisposableDomain");
        result.ErrorMessage.Should().Contain("disposable");
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldReject_UnresolvableDnsDomain()
    {
        var result = await _service.VerifyEmailAsync("test@invalid-nonexistent-domain-xyz-999.bg");

        result.IsValid.Should().BeFalse();
        result.Status.Should().Be("NoMxRecord");
    }

    [Fact]
    public async Task VerifyEmailAsync_ShouldPass_ForValidEmailSyntaxAndRealDomain()
    {
        var result = await _service.VerifyEmailAsync("test@gmail.com");

        result.IsValid.Should().BeTrue();
        result.Status.Should().Be("Valid");
    }
}
