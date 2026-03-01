using BuildSmart.Core.Domain.Entities;
using FluentAssertions;

namespace BuildSmart.Core.Domain.Tests.Entities;

public class TradesmanProfileTests
{
    [Fact]
    public void TradesmanProfile_ShouldHaveVideoIntroductionUrl()
    {
        // Arrange
        var profile = new TradesmanProfile();
        var videoUrl = "https://youtube.com/my-intro";

        // Act
        profile.VideoIntroductionUrl = videoUrl;

        // Assert
        profile.VideoIntroductionUrl.Should().Be(videoUrl);
    }

    [Fact]
    public void TradesmanProfile_ShouldHaveCertifications()
    {
        // Arrange
        var profile = new TradesmanProfile();
        var certification = new Certification 
        { 
            Title = "Master Plumber", 
            IssuedAt = DateTime.UtcNow 
        };

        // Act
        profile.Certifications.Add(certification);

        // Assert
        profile.Certifications.Should().Contain(certification);
    }
}
