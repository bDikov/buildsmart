using BuildSmart.Core.Domain.Entities;

namespace BuildSmart.Core.Application.Interfaces;

public interface IPaymentService
{
    Task<Booking> AcceptBidAsync(Guid homeownerId, Guid bidId);
    Task ApproveMilestoneAsync(Guid homeownerId, Guid milestonePaymentId);
}
