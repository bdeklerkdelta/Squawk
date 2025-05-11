using Squawker.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Squawker.Infrastructure.Data.Configurations;

public class SquawkConfiguration : IEntityTypeConfiguration<Squawk>
{
    public void Configure(EntityTypeBuilder<Squawk> builder)
    {
        builder.Property(s => s.Content)
            .HasMaxLength(400)
            .IsRequired();
    }
}