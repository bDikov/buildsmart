using BuildSmart.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace BuildSmart.Infrastructure.Persistence.Configurations;

public class QuestionConfiguration : IEntityTypeConfiguration<Question>
{
    public void Configure(EntityTypeBuilder<Question> builder)
    {
        builder.ToTable("Questions");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.QuestionCode)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(q => q.QuestionCode)
            .IsUnique();

        builder.Property(q => q.Text)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(q => q.Type)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(q => q.OptionsJson)
            .HasColumnType("jsonb");

        builder.Property(q => q.EnglishOptionsJson)
            .HasColumnType("jsonb");

        builder.Property(q => q.HintText)
            .HasMaxLength(1000);

        builder.Property(q => q.EnglishHint)
            .HasMaxLength(1000);

        builder.Property(q => q.EnglishText)
            .HasMaxLength(1000);

        builder.Property(q => q.VisibilityCondition)
            .HasMaxLength(2000);

        // Serialize SkuIds list as JSONB array
        builder.Property(q => q.SkuIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            )
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()))
            ;
        builder.Property(q => q.SkuIds).HasColumnType("jsonb");

        // Serialize FormulaIds list as JSONB array
        builder.Property(q => q.FormulaIds)
            .HasConversion(
                v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                v => JsonSerializer.Deserialize<List<Guid>>(v, (JsonSerializerOptions?)null) ?? new List<Guid>()
            )
            .Metadata.SetValueComparer(new Microsoft.EntityFrameworkCore.ChangeTracking.ValueComparer<List<Guid>>(
                (c1, c2) => c1 != null && c2 != null && c1.SequenceEqual(c2),
                c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                c => c.ToList()))
            ;
        builder.Property(q => q.FormulaIds).HasColumnType("jsonb");

        // Relationships
        builder.HasOne(q => q.ServiceCategory)
            .WithMany()
            .HasForeignKey(q => q.ServiceCategoryId)
            .OnDelete(DeleteBehavior.SetNull);

        // Self-referencing relationship for next questions in spider-net
        builder.HasOne(q => q.ParentQuestion)
            .WithMany(q => q.NextQuestions)
            .HasForeignKey(q => q.ParentQuestionId)
            .OnDelete(DeleteBehavior.Restrict);

        // Many-to-many relationship with ServiceSku
        builder.HasMany(q => q.Skus)
            .WithMany(s => s.Questions)
            .UsingEntity<Dictionary<string, object>>(
                "QuestionSku",
                j => j.HasOne<ServiceSku>().WithMany().HasForeignKey("SkuId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Question>().WithMany().HasForeignKey("QuestionId").OnDelete(DeleteBehavior.Cascade),
                je => je.ToTable("QuestionSkus")
            );

        // Many-to-many relationship with Formula
        builder.HasMany(q => q.Formulas)
            .WithMany(f => f.Questions)
            .UsingEntity<Dictionary<string, object>>(
                "QuestionFormula",
                j => j.HasOne<Formula>().WithMany().HasForeignKey("FormulaId").OnDelete(DeleteBehavior.Cascade),
                j => j.HasOne<Question>().WithMany().HasForeignKey("QuestionId").OnDelete(DeleteBehavior.Cascade),
                je => je.ToTable("QuestionFormulas")
            );
    }
}
