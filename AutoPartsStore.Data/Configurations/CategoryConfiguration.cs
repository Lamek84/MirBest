using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.Property(c => c.Name).IsRequired().HasMaxLength(150);
        builder.Property(c => c.ImageUrl).HasMaxLength(300);

        // Name muss nur unter demselben Elternteil eindeutig sein (z. B. "Filter"
        // kann unter verschiedenen Oberkategorien vorkommen) — vorher war das
        // ein globaler eindeutiger Index, das würde Unterkategorien blockieren.
        builder.HasIndex(c => new { c.ParentCategoryId, c.Name }).IsUnique();

        // Selbstreferenz für die Hierarchie. Restrict statt Cascade — eine
        // Kategorie mit Unterkategorien kann nicht versehentlich samt Baum
        // gelöscht werden (siehe CategoriesController.DeleteConfirmed).
        builder.HasOne(c => c.ParentCategory)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(c => c.ParentCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
