using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteConfiguration : IEntityTypeConfiguration<RestauranteEntidad>
{
    public void Configure(EntityTypeBuilder<RestauranteEntidad> builder)
    {
        builder.ToTable("restaurantes");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id");
        builder.Property(r => r.Nombre).HasColumnName("nombre").HasMaxLength(80).IsRequired();
        builder.Property(r => r.Slug).HasColumnName("slug").HasMaxLength(40).IsRequired();
        builder.Property(r => r.Activo).HasColumnName("activo").IsRequired();
        builder.Property(r => r.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.ComplexProperty(r => r.Paleta, paleta =>
        {
            paleta.Property(p => p.Principal).HasColumnName("color_principal").HasColumnType("char(7)").IsRequired(false);
            paleta.Property(p => p.Acento).HasColumnName("color_acento").HasColumnType("char(7)").IsRequired(false);
            paleta.Property(p => p.Oscuro).HasColumnName("color_oscuro").HasColumnType("char(7)").IsRequired(false);
            paleta.Property(p => p.Fondo).HasColumnName("color_fondo").HasColumnType("char(7)").IsRequired(false);
            paleta.Property(p => p.Texto).HasColumnName("color_texto").HasColumnType("char(7)").IsRequired(false);
        });
        builder.HasIndex(r => r.Nombre).IsUnique().HasDatabaseName("ix_restaurantes_nombre");
        builder.HasIndex(r => r.Slug).IsUnique().HasDatabaseName("ix_restaurantes_slug");
    }
}
