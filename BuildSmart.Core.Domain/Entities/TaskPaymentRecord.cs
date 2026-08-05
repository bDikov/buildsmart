using BuildSmart.Core.Domain.Common;
using BuildSmart.Core.Domain.Enums;

namespace BuildSmart.Core.Domain.Entities;

public class TaskPaymentRecord : BaseEntity
{
    public Guid JobTaskId { get; set; }
    public JobTask JobTask { get; set; } = null!;

    public PaymentStatus Status { get; set; } = PaymentStatus.AwaitingPayment;

    public decimal CalculatedAmount { get; set; }
    public decimal FinalAmount { get; set; }

    public DateTime? PaidAt { get; set; }

    public Guid? PaidByAdminId { get; set; }
    public User? PaidByAdmin { get; set; }

    public string? PaymentNotes { get; set; }

    public void MarkAsPaid(Guid adminUserId, decimal finalAmount, string? notes)
    {
        Status = PaymentStatus.Paid;
        FinalAmount = finalAmount;
        PaidAt = DateTime.UtcNow;
        PaidByAdminId = adminUserId;
        PaymentNotes = notes;
        UpdatedAt = DateTime.UtcNow;
    }
}
