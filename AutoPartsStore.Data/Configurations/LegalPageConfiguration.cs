using AutoPartsStore.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AutoPartsStore.Data.Configurations;

public class LegalPageConfiguration : IEntityTypeConfiguration<LegalPage>
{
    public void Configure(EntityTypeBuilder<LegalPage> builder)
    {
        builder.Property(p => p.Key).IsRequired().HasMaxLength(50);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        // Kein explizites HasColumnType hier: EF Core mappt ein string ohne
        // HasMaxLength auf SQL Server ohnehin standardmäßig auf nvarchar(max).
        // "nvarchar(max)" explizit anzugeben ist nicht nur redundant, sondern
        // bricht die SQLite-Tests (SQLite kennt "(max)" als Typ-Parameter nicht
        // und wirft einen Syntaxfehler beim Erstellen der Tabelle).
        builder.Property(p => p.Content).IsRequired();

        builder.HasIndex(p => p.Key).IsUnique();
    }
}
