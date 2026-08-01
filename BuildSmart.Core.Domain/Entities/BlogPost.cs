using System;

namespace BuildSmart.Core.Domain.Entities;

public class BlogPost
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Slug { get; set; } = string.Empty;
    
    public string TitleBg { get; set; } = string.Empty;
    public string TitleEn { get; set; } = string.Empty;
    
    public string DescriptionBg { get; set; } = string.Empty;
    public string DescriptionEn { get; set; } = string.Empty;
    
    public string ContentBg { get; set; } = string.Empty;
    public string ContentEn { get; set; } = string.Empty;
    
    public string CoverImageUrl { get; set; } = string.Empty;
    
    public string CategoryBg { get; set; } = string.Empty;
    public string CategoryEn { get; set; } = string.Empty;
    
    public string ReadTimeBg { get; set; } = string.Empty;
    public string ReadTimeEn { get; set; } = string.Empty;
    
    public string? SeoKeywordsBg { get; set; }
    public string? SeoKeywordsEn { get; set; }
    
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    
    public bool IsPublished { get; set; } = true;
}
