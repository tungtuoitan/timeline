using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SuperAppModels.Models;

namespace SuperAppDataRepositories.Data.Configurations
{
    public class WikiInfoConfiguration : IEntityTypeConfiguration<WikiInfo>
    {
        public void Configure(EntityTypeBuilder<WikiInfo> builder)
        {
            builder.ToTable("info", "wiki");

            builder.HasKey(i => i.Id);
            builder.Property(i => i.Id)
                .HasColumnName("id")
                .ValueGeneratedOnAdd();

            builder.Property(i => i.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(i => i.Title)
                .HasColumnName("title")
                .HasMaxLength(500)
                .IsRequired();

            builder.Property(i => i.Content)
                .HasColumnName("content")
                .IsRequired();

            builder.Property(i => i.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()");

            builder.Property(i => i.UpdatedAt)
                .HasColumnName("updated_at");

            builder.Property(i => i.DeletedAt)
                .HasColumnName("deleted_at");

            builder.HasIndex(i => i.UserId)
                .HasDatabaseName("IX_wiki_info_user")
                .HasFilter("[deleted_at] IS NULL");

            builder.HasOne(i => i.User)
                .WithMany()
                .HasForeignKey(i => i.UserId)
                .HasConstraintName("FK_wiki_info_users")
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
