using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Context.Configuration;

public class PostDbConfiguration : IEntityTypeConfiguration<PostDb>
{
    public void Configure(EntityTypeBuilder<PostDb> builder)
    {
        builder.ToTable("post");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(p => p.Title)
            .HasColumnName("title")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(p => p.Salary)
            .HasColumnName("salary")
            .HasColumnType("numeric(18,2)")
            .IsRequired();

        builder.Property(p => p.CompanyId)
            .HasColumnName("company_id")
            .HasColumnType("uuid").ValueGeneratedNever()
            .IsRequired();

        builder.HasCheckConstraint("salary_check", "salary > 0");
        
        builder.Property(x=>x.IsDeleted)
            .HasColumnName("_is_deleted")
            .HasColumnType("bool")
            .IsRequired();

        builder.HasOne<CompanyDb>()
            .WithMany(c => c.Posts)
            .HasForeignKey(p => p.CompanyId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}