using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Api.GraphQL.Resolvers;

public class BookingResolvers
{
    public async Task<IEnumerable<MilestonePayment>> GetMilestonePaymentsAsync(
        [Parent] Booking booking,
        [Service] IUnitOfWork unitOfWork,
        CancellationToken cancellationToken)
    {
        // Ideally we'd fetch this efficiently, but this is a placeholder 
        // to show how it hooks into the domain.
        // Assuming we add a method to get milestone payments by booking ID.
        var bookingWithMilestones = await unitOfWork.Bookings.GetQueryable()
            .Include(b => b.MilestonePayments)
            .FirstOrDefaultAsync(b => b.Id == booking.Id, cancellationToken);

        return bookingWithMilestones?.MilestonePayments ?? Enumerable.Empty<MilestonePayment>();
    }
}
