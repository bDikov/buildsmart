using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuildSmart.Core.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace BuildSmart.Infrastructure.Services;

public class EmailVerificationService : IEmailVerificationService
{
    private readonly ILogger<EmailVerificationService> _logger;

    private static readonly HashSet<string> DisposableDomains = new(StringComparer.OrdinalIgnoreCase)
    {
        "tempmail.com", "temp-mail.org", "mailinator.com", "10minutemail.com",
        "trashmail.com", "dispostable.com", "guerrillamail.com", "yopmail.com",
        "getnada.com", "sharklasers.com", "throwawaymail.com", "maildrop.cc",
        "fakeinbox.com", "disposablemail.com", "0clickemail.com", "bmail.com",
        "mailnesia.com", "crazymailing.com", "tmailor.com", "mytemp.email",
        "temp-mail.io", "burnermail.io", "nada.ltd", "dropmail.me"
    };

    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public EmailVerificationService(ILogger<EmailVerificationService> logger)
    {
        _logger = logger;
    }

    public async Task<EmailVerificationResult> VerifyEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return new EmailVerificationResult
            {
                IsValid = false,
                Status = "InvalidSyntax",
                ErrorMessage = "Email is empty."
            };
        }

        var trimmed = email.Trim();

        // 1. Syntax Verification
        if (!EmailRegex.IsMatch(trimmed))
        {
            _logger.LogWarning("Email '{Email}' failed regex syntax validation.", trimmed);
            return new EmailVerificationResult
            {
                IsValid = false,
                Status = "InvalidSyntax",
                ErrorMessage = "Невалиден формат на имейл адреса."
            };
        }

        var parts = trimmed.Split('@');
        if (parts.Length != 2)
        {
            return new EmailVerificationResult
            {
                IsValid = false,
                Status = "InvalidSyntax",
                ErrorMessage = "Невалиден имейл адрес."
            };
        }

        var domain = parts[1].ToLowerInvariant();

        // 2. Disposable Domain Filter
        if (DisposableDomains.Contains(domain))
        {
            _logger.LogWarning("Email '{Email}' uses a disposable domain '{Domain}'.", trimmed, domain);
            return new EmailVerificationResult
            {
                IsValid = false,
                Status = "DisposableDomain",
                ErrorMessage = "Временни имейл домейни (disposable email) не се приемат."
            };
        }

        // 3. DNS Host Resolution Check (Verify domain exists and has active DNS records)
        try
        {
            var addresses = await Dns.GetHostAddressesAsync(domain);
            if (addresses == null || addresses.Length == 0)
            {
                _logger.LogWarning("Domain '{Domain}' has no active DNS IP addresses.", domain);
                return new EmailVerificationResult
                {
                    IsValid = false,
                    Status = "NoMxRecord",
                    ErrorMessage = "Домейнът на имейла не съществува или няма активни пощенски сървъри."
                };
            }
        }
        catch (SocketException ex)
        {
            _logger.LogWarning(ex, "DNS resolution failed for domain '{Domain}'.", domain);
            return new EmailVerificationResult
            {
                IsValid = false,
                Status = "NoMxRecord",
                ErrorMessage = "Домейнът на имейла не бе намерен (невалиден DNS)."
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error verifying DNS records for '{Domain}'. Proceeding with caution.", domain);
        }

        return new EmailVerificationResult
        {
            IsValid = true,
            Status = "Valid",
            ErrorMessage = null
        };
    }
}
