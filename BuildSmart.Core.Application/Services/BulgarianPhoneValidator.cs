using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace BuildSmart.Core.Application.Services;

public enum PhoneErrorCode
{
    None,
    Empty,
    InvalidCharacters,
    MissingDigits,
    ExtraDigits,
    InvalidPrefix,
    DummySequence,
    DummyRepetition,
    LowEntropy
}

public enum PhoneFeedbackSeverity
{
    None,
    Info,
    Warning,
    Success,
    Danger
}

public record PhoneValidationResult(
    bool IsValid,
    string RawDigits,
    string NormalizedNumber,
    string FormattedDisplay,
    PhoneErrorCode ErrorCode,
    int DigitCount,
    int DigitsDelta,
    PhoneFeedbackSeverity Severity,
    string MessageBg,
    string MessageEn
);

public static class BulgarianPhoneValidator
{
    private static readonly string[] DummySequences = new[]
    {
        "123456", "234567", "345678", "456789", "567890",
        "654321", "765432", "876543", "987654", "098765"
    };

    private static readonly Regex InvalidCharsRegex = new(@"[a-zA-Zа-яА-Я]", RegexOptions.Compiled);
    private static readonly Regex RepeatedDigitsRegex = new(@"(\d)\1{4,}", RegexOptions.Compiled);
    private static readonly Regex RepeatedPairsRegex = new(@"(\d{2})\1{2,}", RegexOptions.Compiled);

    public static PhoneValidationResult Evaluate(string? rawInput, bool checkDummyPatterns = true)
    {
        if (string.IsNullOrWhiteSpace(rawInput))
        {
            return new PhoneValidationResult(
                IsValid: false,
                RawDigits: string.Empty,
                NormalizedNumber: string.Empty,
                FormattedDisplay: string.Empty,
                ErrorCode: PhoneErrorCode.Empty,
                DigitCount: 0,
                DigitsDelta: -10,
                Severity: PhoneFeedbackSeverity.None,
                MessageBg: "Моля, въведете телефонен номер.",
                MessageEn: "Please enter a phone number."
            );
        }

        var trimmed = rawInput.Trim();

        // Check for forbidden alphabetical characters
        if (InvalidCharsRegex.IsMatch(trimmed))
        {
            return new PhoneValidationResult(
                IsValid: false,
                RawDigits: string.Empty,
                NormalizedNumber: trimmed,
                FormattedDisplay: trimmed,
                ErrorCode: PhoneErrorCode.InvalidCharacters,
                DigitCount: 0,
                DigitsDelta: 0,
                Severity: PhoneFeedbackSeverity.Danger,
                MessageBg: "Телефонният номер може да съдържа само цифри.",
                MessageEn: "Phone number can only contain digits."
            );
        }

        // Clean spaces, dashes, brackets, slashes
        var cleaned = trimmed
            .Replace(" ", "")
            .Replace("-", "")
            .Replace("(", "")
            .Replace(")", "")
            .Replace("/", "")
            .Replace(".", "");

        // Normalize international prefixes (+359, 00359, 359)
        if (cleaned.StartsWith("+359", StringComparison.Ordinal))
        {
            cleaned = "0" + cleaned.Substring(4).TrimStart('0');
        }
        else if (cleaned.StartsWith("00359", StringComparison.Ordinal))
        {
            cleaned = "0" + cleaned.Substring(5).TrimStart('0');
        }
        else if (cleaned.StartsWith("359", StringComparison.Ordinal) && cleaned.Length > 9)
        {
            cleaned = "0" + cleaned.Substring(3).TrimStart('0');
        }
        else if (cleaned.Length == 9 && (cleaned.StartsWith('8') || cleaned.StartsWith('9')))
        {
            // User omitted the leading zero for a mobile number (e.g. 888123456)
            cleaned = "0" + cleaned;
        }

        var digits = new string(cleaned.Where(char.IsDigit).ToArray());
        int digitCount = digits.Length;

        // Valid landline exception: Sofia landlines (02 + 7 digits = 9 digits)
        bool isSofiaLandline = digits.StartsWith("02", StringComparison.Ordinal) && digitCount == 9;

        // Missing digits (short input)
        if (digitCount < 10 && !isSofiaLandline)
        {
            int missing = 10 - digitCount;
            string msgBg = missing == 1
                ? "Липсва още 1 цифра (9 от 10)."
                : $"Остават още {missing} цифри ({digitCount} от 10).";
            string msgEn = missing == 1
                ? "Missing 1 digit (9 of 10)."
                : $"{missing} digits remaining ({digitCount} of 10).";

            return new PhoneValidationResult(
                IsValid: false,
                RawDigits: digits,
                NormalizedNumber: digits,
                FormattedDisplay: FormatPartialNumber(digits),
                ErrorCode: PhoneErrorCode.MissingDigits,
                DigitCount: digitCount,
                DigitsDelta: -missing,
                Severity: PhoneFeedbackSeverity.Info,
                MessageBg: msgBg,
                MessageEn: msgEn
            );
        }

        // Extra digits (too long)
        if (digitCount > 10)
        {
            int extra = digitCount - 10;
            string msgBg = extra == 1
                ? "Въведена е 1 излишна цифра (11 от 10). Проверете за повторена цифра."
                : $"Въведени са {extra} излишни цифри ({digitCount} от 10).";
            string msgEn = extra == 1
                ? "1 extra digit entered (11 of 10). Check for duplicate digits."
                : $"{extra} extra digits entered ({digitCount} of 10).";

            return new PhoneValidationResult(
                IsValid: false,
                RawDigits: digits,
                NormalizedNumber: digits,
                FormattedDisplay: digits,
                ErrorCode: PhoneErrorCode.ExtraDigits,
                DigitCount: digitCount,
                DigitsDelta: extra,
                Severity: PhoneFeedbackSeverity.Warning,
                MessageBg: msgBg,
                MessageEn: msgEn
            );
        }

        // Check prefixes
        bool isMobilePrefix = digits.StartsWith("087", StringComparison.Ordinal) ||
                              digits.StartsWith("088", StringComparison.Ordinal) ||
                              digits.StartsWith("089", StringComparison.Ordinal) ||
                              digits.StartsWith("098", StringComparison.Ordinal) ||
                              digits.StartsWith("099", StringComparison.Ordinal);

        bool isLandlinePrefix = isSofiaLandline ||
                                digits.StartsWith("02", StringComparison.Ordinal) ||
                                digits.StartsWith("03", StringComparison.Ordinal) ||
                                digits.StartsWith("05", StringComparison.Ordinal);

        if (!isMobilePrefix && !isLandlinePrefix)
        {
            return new PhoneValidationResult(
                IsValid: false,
                RawDigits: digits,
                NormalizedNumber: digits,
                FormattedDisplay: digits,
                ErrorCode: PhoneErrorCode.InvalidPrefix,
                DigitCount: digitCount,
                DigitsDelta: 0,
                Severity: PhoneFeedbackSeverity.Danger,
                MessageBg: "Българските мобилни номера започват с 087, 088, 089 или 098/099.",
                MessageEn: "Bulgarian mobile numbers start with 087, 088, 089 or 098/099."
            );
        }

        var subscriberPart = digits.Length >= 3 ? digits.Substring(3) : digits;

        if (checkDummyPatterns)
        {
            // Anti-Dummy Check 1: Sequential sequences (123456, 654321, etc.)
            foreach (var seq in DummySequences)
            {
                if (digits.Contains(seq, StringComparison.Ordinal))
                {
                    return new PhoneValidationResult(
                        IsValid: false,
                        RawDigits: digits,
                        NormalizedNumber: digits,
                        FormattedDisplay: FormatBulgarianNumber(digits),
                        ErrorCode: PhoneErrorCode.DummySequence,
                        DigitCount: digitCount,
                        DigitsDelta: 0,
                        Severity: PhoneFeedbackSeverity.Danger,
                        MessageBg: "Номерът съдържа поредни тестови цифри. Моля, въведете реален телефон.",
                        MessageEn: "Phone number contains sequential test digits. Please enter a real phone."
                    );
                }
            }

            // Anti-Dummy Check 2: 5 or more identical digits in a row (0899000000, 0888888888)
            if (RepeatedDigitsRegex.IsMatch(digits))
            {
                return new PhoneValidationResult(
                    IsValid: false,
                    RawDigits: digits,
                    NormalizedNumber: digits,
                    FormattedDisplay: FormatBulgarianNumber(digits),
                    ErrorCode: PhoneErrorCode.DummyRepetition,
                    DigitCount: digitCount,
                    DigitsDelta: 0,
                    Severity: PhoneFeedbackSeverity.Danger,
                    MessageBg: "Номерът съдържа повтарящи се тестови цифри. Моля, въведете реален телефон.",
                    MessageEn: "Phone number contains repeated test digits. Please enter a real phone."
                );
            }

            // Anti-Dummy Check 3: Subscriber part has >= 5 identical digits overall
            if (subscriberPart.GroupBy(c => c).Any(g => g.Count() >= 5))
            {
                return new PhoneValidationResult(
                    IsValid: false,
                    RawDigits: digits,
                    NormalizedNumber: digits,
                    FormattedDisplay: FormatBulgarianNumber(digits),
                    ErrorCode: PhoneErrorCode.DummyRepetition,
                    DigitCount: digitCount,
                    DigitsDelta: 0,
                    Severity: PhoneFeedbackSeverity.Danger,
                    MessageBg: "Номерът съдържа твърде много еднакви цифри. Моля, въведете реален телефон.",
                    MessageEn: "Phone number contains too many identical digits. Please enter a real phone."
                );
            }

            // Anti-Dummy Check 4: Repeating pairs in subscriber part (0888121212, 0899010101)
            if (RepeatedPairsRegex.IsMatch(subscriberPart))
            {
                return new PhoneValidationResult(
                    IsValid: false,
                    RawDigits: digits,
                    NormalizedNumber: digits,
                    FormattedDisplay: FormatBulgarianNumber(digits),
                    ErrorCode: PhoneErrorCode.LowEntropy,
                    DigitCount: digitCount,
                    DigitsDelta: 0,
                    Severity: PhoneFeedbackSeverity.Danger,
                    MessageBg: "Номерът съдържа шаблон от повтарящи се двойки. Моля, въведете реален номер.",
                    MessageEn: "Phone number contains repeating cycles. Please enter a real contact number."
                );
            }

            // Anti-Dummy Check 5: Low entropy (only 1 or 2 unique digits in 7-digit subscriber part)
            if (subscriberPart.Length >= 6 && subscriberPart.Distinct().Count() <= 2)
            {
                return new PhoneValidationResult(
                    IsValid: false,
                    RawDigits: digits,
                    NormalizedNumber: digits,
                    FormattedDisplay: FormatBulgarianNumber(digits),
                    ErrorCode: PhoneErrorCode.LowEntropy,
                    DigitCount: digitCount,
                    DigitsDelta: 0,
                    Severity: PhoneFeedbackSeverity.Danger,
                    MessageBg: "Номерът изглежда фиктивен или генериран. Моля, въведете реален номер.",
                    MessageEn: "Phone number appears fictitious or generated. Please enter a real contact number."
                );
            }
        }

        // All checks passed!
        string formatted = FormatBulgarianNumber(digits);
        return new PhoneValidationResult(
            IsValid: true,
            RawDigits: digits,
            NormalizedNumber: digits,
            FormattedDisplay: formatted,
            ErrorCode: PhoneErrorCode.None,
            DigitCount: digitCount,
            DigitsDelta: 0,
            Severity: PhoneFeedbackSeverity.Success,
            MessageBg: "Валиден български телефонен номер.",
            MessageEn: "Valid Bulgarian phone number."
        );
    }

    public static string FormatBulgarianNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        if (digits.Length == 10)
        {
            // Format as 0888 123 456
            return $"{digits.Substring(0, 4)} {digits.Substring(4, 3)} {digits.Substring(7, 3)}";
        }

        if (digits.Length == 9 && digits.StartsWith("02", StringComparison.Ordinal))
        {
            // Format as 02 987 6543
            return $"{digits.Substring(0, 2)} {digits.Substring(2, 3)} {digits.Substring(5, 4)}";
        }

        return digits;
    }

    public static string FormatPartialNumber(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        if (digits.Length <= 4)
            return digits;

        if (digits.Length <= 7)
            return $"{digits.Substring(0, 4)} {digits.Substring(4)}";

        return $"{digits.Substring(0, 4)} {digits.Substring(4, 3)} {digits.Substring(7)}";
    }

    public static string ToInternationalFormat(string digits)
    {
        if (string.IsNullOrEmpty(digits))
            return string.Empty;

        if (digits.StartsWith("0", StringComparison.Ordinal) && digits.Length >= 9)
        {
            return "+359" + digits.Substring(1);
        }

        return digits;
    }
}
