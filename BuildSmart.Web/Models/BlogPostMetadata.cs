namespace BuildSmart.Web.Models;

public class BlogPostMetadata
{
    public string Slug { get; set; } = "";
    public string? TitleBg { get; set; }
    public string? TitleEn { get; set; }
    public string? DescriptionBg { get; set; }
    public string? DescriptionEn { get; set; }
    public string Date { get; set; } = "";
    public string? CategoryBg { get; set; }
    public string? CategoryEn { get; set; }
    public string Image { get; set; } = "";
    public string? ReadTimeBg { get; set; }
    public string? ReadTimeEn { get; set; }
    public string? SeoKeywordsBg { get; set; }
    public string? SeoKeywordsEn { get; set; }
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? Category { get; set; }
    public string? ReadTime { get; set; }
    public string? SeoKeywords { get; set; }
}
