using AssignmentSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AssignmentSystem.Infrastructure.Data.Configurations;

public class TeacherSubjectAssignmentConfiguration : IEntityTypeConfiguration<TeacherSubjectAssignment>
{
    public void Configure(EntityTypeBuilder<TeacherSubjectAssignment> builder)
    {
        // Composite primary key
        builder.HasKey(t => new { t.TeacherId, t.SubjectId });

        builder.Property(t => t.AssignedDate)
            .IsRequired();

        builder.HasOne(t => t.Teacher)
            .WithMany(u => u.SubjectAssignments)
            .HasForeignKey(t => t.TeacherId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Subject)
            .WithMany(s => s.Teachers)
            .HasForeignKey(t => t.SubjectId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
