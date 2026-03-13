using Database.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Database.Context.Configuration;

public class UserDbConfiguration : IEntityTypeConfiguration<UserDb>
{
    public void Configure(EntityTypeBuilder<UserDb> builder)
    {
        builder.ToTable("users");

        builder.HasKey(keyExpression => keyExpression.Email);
        builder.Property(keyExpression => keyExpression.Email).HasColumnName("email").HasColumnType("text").IsRequired();
        builder.ToTable(t =>
            t.HasCheckConstraint("EmailCheck", "email ~ '^[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\\.[A-Za-z]{2,}$'"));

        builder.Property(keyExpression => keyExpression.Password).HasColumnName("password").HasColumnType("text").IsRequired();
        builder.HasIndex(keyExpression => keyExpression.Password).IsUnique();
        
        builder.Property(u => u.Salt).HasColumnName("salt").HasColumnType("text").IsRequired();
        
        builder.Property(u => u.Role).HasColumnName("role").HasColumnType("text").IsRequired();
        
        builder.Property(u=>u.Id).HasColumnName("id").IsRequired();

        builder.Property(u => u.FailedLoginAttempts)
            .HasColumnName("failed_login_attempts")
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(u => u.LockoutUntilUtc)
            .HasColumnName("lockout_until_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.LastPasswordChangedAtUtc)
            .HasColumnName("last_password_changed_at_utc")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()")
            .IsRequired();

        builder.Property(u => u.OtpChallengeId)
            .HasColumnName("otp_challenge_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(u => u.OtpCodeHash)
            .HasColumnName("otp_code_hash")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(u => u.OtpExpiresAtUtc)
            .HasColumnName("otp_expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.RecoveryTokenHash)
            .HasColumnName("recovery_token_hash")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(u => u.RecoveryTokenExpiresAtUtc)
            .HasColumnName("recovery_token_expires_at_utc")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        builder.Property(u => u.PasswordChangeRequired)
            .HasColumnName("password_change_required")
            .HasColumnType("boolean")
            .HasDefaultValue(false)
            .IsRequired();
    }
}