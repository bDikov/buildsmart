using System;

namespace BuildSmart.Core.Domain.Entities;

public class LandingPageContent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    
    public string Slug { get; set; } = string.Empty;
    public string PageType { get; set; } = "custom"; // "apartment", "bathroom", "finishing", "mep", "custom"
    
    public string TitleBg { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    
    public string SubtitleBg { get; set; } = string.Empty;
    public string SubtitleEn { get; set; } = string.Empty;
    
    public string BadgeBg { get; set; } = string.Empty;
    public string BadgeEn { get; set; } = string.Empty;
    
    public string HeroImageUrl { get; set; } = string.Empty;
    public string HeroVideoUrl { get; set; } = string.Empty;
    
    public string MediaGalleryJson { get; set; } = "[]";
    public string FeaturesJson { get; set; } = "[]";
    
    public string CtaTextBg { get; set; } = string.Empty;
    public string CtaTextEn { get; set; } = string.Empty;
    public string CtaLink { get; set; } = "/renovation-estimator";
    
    public string? MetaTitleBg { get; set; }
    public string? MetaTitleEn { get; set; }
    public string? MetaDescriptionBg { get; set; }
    public string? MetaDescriptionEn { get; set; }
    
    public bool IsPublished { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
