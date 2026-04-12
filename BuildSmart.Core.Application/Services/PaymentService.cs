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
        var bid = await _context_Bids_GetQueryable()
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
        var agreedAmount = bid.Amount;
        var currency = agreedAmount.Currency;
        
        // 3% Homeowner Fee
        var homeownerFeeTotal = Math.Round(agreedAmount.Total * 0.03m, 2);
        var platformFeeHomeowner = Amount.Create(currency, homeownerFeeTotal);

        // 5% Tradesman Fee
        var tradesmanFeeTotal = Math.Round(agreedAmount.Total * 0.05m, 2);
        var platformFeeTradesman = Amount.Create(currency, tradesmanFeeTotal);

        var totalEscrow = Amount.Create(currency, agreedAmount.Total + homeownerFeeTotal);

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
                AmountAllocated = item.Price,
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
        var milestone = await _context_MilestonePayments_GetQueryable()
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

    private Microsoft.EntityFrameworkCore.DbSet<Bid> _context_Bids_GetQueryable()
    {
        // Ideally exposed on repository
        return (Microsoft.EntityFrameworkCore.DbSet<Bid>)_unitOfWork.Bids.GetQueryable();
    }

    private Microsoft.EntityFrameworkCore.DbSet<MilestonePayment> _context_MilestonePayments_GetQueryable()
    {
        // This is a hack for the sample, assuming we add IMilestonePaymentRepository
        throw new NotImplementedException();
    }
}
