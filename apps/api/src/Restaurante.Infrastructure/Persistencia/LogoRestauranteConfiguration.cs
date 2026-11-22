using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;
using Restaurante.Domain.Restaurantes;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class LogoRestauranteConfiguration : IEntityTypeConfiguration<LogoRestaurante>
{
    public void Configure(EntityTypeBuilder<LogoRestaurante> builder)
    {
        builder.ToTable("restaurantes_logo");
        builder.HasKey(l => l.RestauranteId);
        builder.Property(l => l.RestauranteId).HasColumnName("restaurante_id").ValueGeneratedNever();
        builder.Property(l => l.Contenido).HasColumnName("contenido").HasColumnType("bytea").IsRequired();
        builder.Property(l => l.Tipo).HasColumnName("tipo").HasMaxLength(20).IsRequired();
        builder.Property(l => l.ETag).HasColumnName("etag").HasMaxLength(64).IsRequired();
        builder.Property(l => l.ActualizadoEn).HasColumnName("actualizado_en").IsRequired();
        builder.HasOne<RestauranteEntidad>().WithOne().HasForeignKey<LogoRestaurante>(l => l.RestauranteId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
