using System.Threading.Tasks;
using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IEstimatorEmailService
{
    Task SendOfferEmailAsync(CalculatorLead lead);
}
