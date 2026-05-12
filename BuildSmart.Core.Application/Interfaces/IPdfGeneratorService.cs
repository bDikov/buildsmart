namespace BuildSmart.Core.Application.Interfaces;

public interface IPdfGeneratorService
{
	Task<byte[]> GenerateOfferPdfAsync(object offerData);
}