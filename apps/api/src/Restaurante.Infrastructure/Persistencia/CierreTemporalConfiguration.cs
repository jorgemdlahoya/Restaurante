using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class CierreTemporalConfiguration : IEntityTypeConfiguration<CierreTemporal>
{
    public void Configure(EntityTypeBuilder<CierreTemporal> builder)
    {
        builder.ToTable("cierres_temporales");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        builder.Property(c => c.SucursalId).HasColumnName("sucursal_id").IsRequired();
        builder.HasOne<Sucursal>().WithMany().HasForeignKey(c => c.SucursalId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.Inicio).HasColumnName("inicio").IsRequired();
        builder.Property(c => c.Fin).HasColumnName("fin").IsRequired();
        builder.Property(c => c.Motivo).HasColumnName("motivo").HasMaxLength(CierreTemporal.LongitudMaximaMotivo)
            .IsRequired(false);
        builder.Property(c => c.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(c => c.CanceladoEn).HasColumnName("cancelado_en").IsRequired(false);
        builder.HasIndex(c => new { c.SucursalId, c.Fin }).HasDatabaseName("ix_cierres_temporales_sucursal_fin");
    }
}
