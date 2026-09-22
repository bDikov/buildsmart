using System.Threading.Tasks;
using BuildSmart.Core.Application.DTOs;

namespace BuildSmart.Core.Application.Interfaces;

public interface IMarkdownOfferParserService
{
    Task<OfferPreviewDto> PreviewMarkdownOfferAsync(string markdownContent);
    Task<ParsedOfferMarkdownDto> ParseMarkdownOfferAsync(string markdownContent);
}
