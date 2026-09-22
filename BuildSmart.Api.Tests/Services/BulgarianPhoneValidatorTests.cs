using System.Threading.Tasks;
using BuildSmart.Core.Application.Services;
using FluentAssertions;
using Xunit;

namespace BuildSmart.Api.Tests.Services;

public class BulgarianPhoneValidatorTests
{
    [Theory]
    [InlineData("0885914263")] // A1 realistic
    [InlineData("0887391824")] // A1 realistic
    [InlineData("0894721985")] // Yettel realistic
    [InlineData("0895628193")] // Yettel realistic
    [InlineData("0878349120")] // Vivacom realistic
    [InlineData("0876251849")] // Vivacom realistic
    [InlineData("0988471923")] // Alternative operator realistic
    [InlineData("029871234")]  // Sofia landline (9 digits)
    public void Evaluate_ShouldReturnSuccess_ForValidBulgarianPhoneNumbers(string number)
    {
        var result = BulgarianPhoneValidator.Evaluate(number);

        result.IsValid.Should().BeTrue();
        result.ErrorCode.Should().Be(PhoneErrorCode.None);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Success);
        result.DigitsDelta.Should().Be(0);
    }

    [Theory]
    [InlineData("+359 885 914 263", "0885914263")]
    [InlineData("+359885914263", "0885914263")]
    [InlineData("00359894721985", "0894721985")]
    [InlineData("+3590885914263", "0885914263")] // Redundant zero after +359
    [InlineData("885914263", "0885914263")] // Omitted leading zero
    [InlineData("(088) 591-42-63", "0885914263")] // Formatting with brackets and dashes
    public void Evaluate_ShouldNormalizeAndPass_ForInternationalAndFormattedNumbers(string rawInput, string expectedDigits)
    {
        var result = BulgarianPhoneValidator.Evaluate(rawInput);

        result.IsValid.Should().BeTrue();
        result.RawDigits.Should().Be(expectedDigits);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Success);
    }

    [Theory]
    [InlineData("088812345", 9, -1)]
    [InlineData("0891234", 7, -3)]
    [InlineData("087", 3, -7)]
    public void Evaluate_ShouldReturnMissingDigits_WhenTooShort(string shortNumber, int expectedCount, int expectedDelta)
    {
        var result = BulgarianPhoneValidator.Evaluate(shortNumber);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.MissingDigits);
        result.DigitCount.Should().Be(expectedCount);
        result.DigitsDelta.Should().Be(expectedDelta);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Info);
        result.MessageBg.Should().MatchRegex("(Липсва|Остават)");
    }

    [Theory]
    [InlineData("08888123456", 11, 1)]
    [InlineData("089912345678", 12, 2)]
    public void Evaluate_ShouldReturnExtraDigits_WhenTooLong(string longNumber, int expectedCount, int expectedDelta)
    {
        var result = BulgarianPhoneValidator.Evaluate(longNumber);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.ExtraDigits);
        result.DigitCount.Should().Be(expectedCount);
        result.DigitsDelta.Should().Be(expectedDelta);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Warning);
        result.MessageBg.Should().MatchRegex("(излишна|излишни)");
    }

    [Theory]
    [InlineData("0899123456")] // Ascending sequence
    [InlineData("0888123456")] // Ascending sequence
    [InlineData("0878234567")] // Ascending sequence
    [InlineData("0898765432")] // Descending sequence
    [InlineData("0878654321")] // Descending sequence
    public void Evaluate_ShouldReject_SequentialDummyNumbers(string dummySeq)
    {
        var result = BulgarianPhoneValidator.Evaluate(dummySeq);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.DummySequence);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Danger);
        result.MessageBg.Should().Contain("поредни");
    }

    [Theory]
    [InlineData("0899000000")] // 6 repeated zeros
    [InlineData("0888888888")] // all eights
    [InlineData("0877777777")] // all sevens
    [InlineData("0899999999")] // all nines
    [InlineData("0888000000")] // 6 zeros
    public void Evaluate_ShouldReject_RepeatedDigitDummyNumbers(string repeatedDummy)
    {
        var result = BulgarianPhoneValidator.Evaluate(repeatedDummy);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.DummyRepetition);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Danger);
        result.MessageBg.Should().MatchRegex("(повтарящи|еднакви)");
    }

    [Theory]
    [InlineData("0888121212")] // repeating pairs
    [InlineData("0899010101")] // repeating pairs
    [InlineData("0888000100")] // 5 zeros in subscriber part
    public void Evaluate_ShouldReject_LowEntropyAndRepeatingCycles(string lowEntropyNumber)
    {
        var result = BulgarianPhoneValidator.Evaluate(lowEntropyNumber);

        result.IsValid.Should().BeFalse();
        (result.ErrorCode == PhoneErrorCode.LowEntropy || result.ErrorCode == PhoneErrorCode.DummyRepetition)
            .Should().BeTrue();
        result.Severity.Should().Be(PhoneFeedbackSeverity.Danger);
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("0668123456")]
    [InlineData("0448123456")]
    public void Evaluate_ShouldReject_InvalidBulgarianPrefixes(string invalidPrefixNumber)
    {
        var result = BulgarianPhoneValidator.Evaluate(invalidPrefixNumber);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.InvalidPrefix);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Danger);
        result.MessageBg.Should().Contain("Българските мобилни");
    }

    [Theory]
    [InlineData("0888abc456")]
    [InlineData("0888тест12")]
    public void Evaluate_ShouldReject_LettersAndForbiddenCharacters(string invalidCharsInput)
    {
        var result = BulgarianPhoneValidator.Evaluate(invalidCharsInput);

        result.IsValid.Should().BeFalse();
        result.ErrorCode.Should().Be(PhoneErrorCode.InvalidCharacters);
        result.Severity.Should().Be(PhoneFeedbackSeverity.Danger);
    }

    [Fact]
    public void Evaluate_ShouldHandleEmptyOrWhitespace()
    {
        var empty = BulgarianPhoneValidator.Evaluate("");
        empty.IsValid.Should().BeFalse();
        empty.ErrorCode.Should().Be(PhoneErrorCode.Empty);

        var whitespace = BulgarianPhoneValidator.Evaluate("   ");
        whitespace.IsValid.Should().BeFalse();
        whitespace.ErrorCode.Should().Be(PhoneErrorCode.Empty);
    }

    [Fact]
    public void FormatBulgarianNumber_ShouldFormatCorrectly()
    {
        var formatted = BulgarianPhoneValidator.FormatBulgarianNumber("0885914263");
        formatted.Should().Be("0885 914 263");

        var intl = BulgarianPhoneValidator.ToInternationalFormat("0885914263");
        intl.Should().Be("+359885914263");
    }
}
