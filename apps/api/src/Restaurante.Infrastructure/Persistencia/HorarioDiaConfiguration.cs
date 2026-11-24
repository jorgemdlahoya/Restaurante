using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class HorarioDiaConfiguration : IEntityTypeConfiguration<HorarioDia>
{
    public void Configure(EntityTypeBuilder<HorarioDia> builder)
    {
        builder.ToTable("horarios_sucursal", t =>
            t.HasCheckConstraint("ck_horarios_sucursal_dia_semana", "dia_semana BETWEEN 0 AND 6"));
        builder.HasKey(h => new { h.SucursalId, h.Dia });
        builder.Property(h => h.SucursalId).HasColumnName("sucursal_id");
        builder.Property(h => h.Dia).HasColumnName("dia_semana").HasConversion<short>();
        builder.Property(h => h.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        builder.Property(h => h.Apertura).HasColumnName("apertura").HasColumnType("time").IsRequired(false);
        builder.Property(h => h.Cierre).HasColumnName("cierre").HasColumnType("time").IsRequired(false);
        // "cerrado" del diseño: columna calculada por PostgreSQL a partir de apertura (el dominio lo deriva igual).
        builder.Ignore(h => h.Cerrado);
        builder.Ignore(h => h.CruzaMedianoche);
        builder.Property<bool>("CerradoEnBase").HasColumnName("cerrado")
            .HasComputedColumnSql("apertura IS NULL", stored: true);
        builder.HasOne<Sucursal>().WithMany().HasForeignKey(h => h.SucursalId).OnDelete(DeleteBehavior.Restrict);
    }
}
