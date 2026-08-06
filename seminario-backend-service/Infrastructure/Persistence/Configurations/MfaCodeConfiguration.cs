using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Seminario.Domain.Entities;

namespace Seminario.Infrastructure.Persistence.Configurations;

public class MfaCodeConfiguration : IEntityTypeConfiguration<MfaCode>
{
    public void Configure(EntityTypeBuilder<MfaCode> builder)
    {
        builder.ToTable("MfaCodes");
        builder.HasKey(m => m.Id);
        builder.Property(m => m.MfaToken).HasMaxLength(100).IsRequired();
        builder.Property(m => m.CodeHash).HasMaxLength(200).IsRequired();
        builder.HasIndex(m => m.MfaToken).IsUnique();

        builder.HasOne(m => m.User)
            .WithMany(u => u.MfaCodes)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
