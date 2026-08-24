using System;

namespace BuildSmart.Api.DTOs;

public class LandingPageInput
{
    public Guid? Id { get; set; }
    public string Slug { get; set; } = string.Empty;
    public string PageType { get; set; } = "custom";
    
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
}

public class MediaGalleryItemDto
{
    public string Url { get; set; } = string.Empty;
    public string Type { get; set; } = "image";
    public string CaptionBg { get; set; } = string.Empty;
    public string CaptionEn { get; set; } = string.Empty;
    public string Section { get; set; } = "gallery";
    public int Order { get; set; } = 1;
}
