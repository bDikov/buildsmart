using BuildSmart.Core.Application.Interfaces;
using BuildSmart.Core.Domain.Entities;
using BuildSmart.Core.Domain.Enums;
using BuildSmart.Core.Domain.ValueObjects;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace BuildSmart.Core.Application.Services;

public class PaymentService : IPaymentService
{
    private readonly IUnitOfWork _unitOfWork;

    public PaymentService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<Booking> AcceptBidAsync(Guid homeownerId, Guid bidId)
    {
        var bid = await _unitOfWork.Bids.GetQueryable()
            .Include(b => b.JobPost)
                .ThenInclude(jp => jp.Project)
            .Include(b => b.BidItems)
            .FirstOrDefaultAsync(b => b.Id == bidId);

        if (bid == null)
            throw new ArgumentException("Bid not found.", nameof(bidId));

        if (bid.JobPost.Project.HomeownerId != homeownerId)
            throw new UnauthorizedAccessException("You are not authorized to accept this bid.");

        if (bid.IsAccepted)
            throw new InvalidOperationException("Bid is already accepted.");

        // Mark bid as accepted
        bid.Accept();
        _unitOfWork.Bids.Update(bid);

        // Mark job post as contracted
        bid.JobPost.ContractJob(); 
        _unitOfWork.JobPosts.Update(bid.JobPost);

        // Fee logic
        var currency = bid.Amount.Currency;
        var agreedAmountTotal = bid.Amount.Total;
        var agreedAmount = Amount.Create(currency, agreedAmountTotal); // Clone to prevent EF Core tracking errors
        
        // 3% Homeowner Fee
        var homeownerFeeTotal = Math.Round(agreedAmountTotal * 0.03m, 2);
        var platformFeeHomeowner = Amount.Create(currency, homeownerFeeTotal);

        // 5% Tradesman Fee
        var tradesmanFeeTotal = Math.Round(agreedAmountTotal * 0.05m, 2);
        var platformFeeTradesman = Amount.Create(currency, tradesmanFeeTotal);

        var totalEscrow = Amount.Create(currency, agreedAmountTotal + homeownerFeeTotal);

        var booking = new Booking
        {
            HomeownerId = homeownerId,
            TradesmanProfileId = bid.TradesmanProfileId,
            JobPostId = bid.JobPostId,
            BidId = bid.Id,
            AgreedBidAmount = agreedAmount,
            PlatformFeeHomeowner = platformFeeHomeowner,
            PlatformFeeTradesman = platformFeeTradesman,
            TotalEscrowAmount = totalEscrow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        foreach (var item in bid.BidItems)
        {
            booking.MilestonePayments.Add(new MilestonePayment
            {
                JobTaskId = item.JobTaskId,
                AmountAllocated = Amount.Create(item.Price.Currency, item.Price.Total), // Clone instance
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await _unitOfWork.Bookings.AddAsync(booking);
        await _unitOfWork.SaveChangesAsync();

        return booking;
    }

    public async Task ApproveMilestoneAsync(Guid homeownerId, Guid milestonePaymentId)
    {
        var milestone = await _unitOfWork.Bookings.GetQueryable()
            .SelectMany(b => b.MilestonePayments)
            .Include(m => m.Booking)
            .FirstOrDefaultAsync(m => m.Id == milestonePaymentId);

        if (milestone == null)
            throw new ArgumentException("Milestone not found.", nameof(milestonePaymentId));

        if (milestone.Booking.HomeownerId != homeownerId)
            throw new UnauthorizedAccessException("Not authorized.");

        milestone.Approve();
        // In a real system, trigger Stripe transfer here
        
        await _unitOfWork.SaveChangesAsync();
    }
}
