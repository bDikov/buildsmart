using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class TaskPaymentRecordConfiguration : IEntityTypeConfiguration<TaskPaymentRecord>
{
    public void Configure(EntityTypeBuilder<TaskPaymentRecord> builder)
    {
        builder.ToTable("TaskPaymentRecords");

        builder.HasKey(pr => pr.Id);

        builder.HasOne(pr => pr.JobTask)
            .WithOne(t => t.PaymentRecord)
            .HasForeignKey<TaskPaymentRecord>(pr => pr.JobTaskId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pr => pr.PaidByAdmin)
            .WithMany()
            .HasForeignKey(pr => pr.PaidByAdminId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
