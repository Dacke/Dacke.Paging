using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Paging.Sample.Web.Data.Models;

namespace Paging.Sample.Web.Data.Configuration;

public class PersonConfiguration : IEntityTypeConfiguration<Person> {
    public void Configure(EntityTypeBuilder<Person> builder) {
        builder.HasKey(m => m.PersonId);

        builder.Property(m => m.PersonId)
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(m => m.FirstName)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(m => m.LastName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.Email)
            .HasMaxLength(255);

        builder.HasIndex(m => new { 
            m.LastName, 
            m.FirstName, 
            m.PersonId 
        });

        builder.HasIndex(m => new { 
            m.Age, 
            m.PersonId 
        });
    }
}