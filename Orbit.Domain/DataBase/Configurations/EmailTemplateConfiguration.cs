using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Orbit.Domain.Entities;

namespace Orbit.Domain.DataBase.Configurations;

public class EmailTemplateConfiguration : IEntityTypeConfiguration<EmailTemplate>
{
    public void Configure(EntityTypeBuilder<EmailTemplate> builder)
    {
        builder.ToTable("email_templates");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasColumnName("name")
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Subject)
            .HasColumnName("subject")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.HtmlBody)
            .HasColumnName("html_body")
            .HasColumnType("NVARCHAR(MAX)")
            .IsRequired();

        builder.Property(t => t.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("SYSUTCDATETIME()");

        builder.HasIndex(t => t.Name)
            .IsUnique()
            .HasDatabaseName("ux_email_templates_name");

        builder.HasQueryFilter(t => t.IsActive);
    }
}
