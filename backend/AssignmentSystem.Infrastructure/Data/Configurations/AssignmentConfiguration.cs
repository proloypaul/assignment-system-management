using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Data.Configurations;

public class AssignmentConfiguration : IEntityTypeConfiguration<Assignment>
{
    public void Configure(EntityTypeBuilder<Assignment> builder)
    {
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(a => a.Description)
            .IsRequired();

        builder.Property(a => a.MaxMarks)
            .IsRequired();

        builder.Property(a => a.Status)
            .IsRequired()
            .HasConversion<string>();

        // Index on EndDate for efficient deadline queries
        builder.HasIndex(a => a.EndDate);

        builder.HasOne(a => a.Teacher)
            .WithMany(u => u.CreatedAssignments)
            .HasForeignKey(a => a.TeacherId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Subject)
            .WithMany(s => s.Assignments)
            .HasForeignKey(a => a.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(a => a.Submissions)
            .WithOne(s => s.Assignment)
            .HasForeignKey(s => s.AssignmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
