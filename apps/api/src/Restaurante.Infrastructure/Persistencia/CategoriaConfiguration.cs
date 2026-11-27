using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Menu;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class CategoriaConfiguration : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        builder.ToTable("categorias", t => t.HasCheckConstraint("ck_categorias_orden", "orden >= 1"));
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(c => c.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        builder.HasOne<RestauranteEntidad>().WithMany().HasForeignKey(c => c.RestauranteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(Categoria.LongitudMaximaNombre).IsRequired();
        builder.Property(c => c.NombreNormalizado).HasColumnName("nombre_normalizado")
            .HasMaxLength(Categoria.LongitudMaximaNombre).IsRequired();
        builder.Property(c => c.NombreCorto).HasColumnName("nombre_corto")
            .HasMaxLength(Categoria.LongitudMaximaNombreCorto).IsRequired(false);
        builder.Property(c => c.NombreCortoNormalizado).HasColumnName("nombre_corto_normalizado")
            .HasMaxLength(Categoria.LongitudMaximaNombreCorto).IsRequired(false);
        builder.Property(c => c.Orden).HasColumnName("orden").IsRequired();
        builder.Property(c => c.Activa).HasColumnName("activa").IsRequired();
        builder.Property(c => c.CreadaEn).HasColumnName("creada_en").IsRequired();
        builder.Property(c => c.EliminadaEn).HasColumnName("eliminada_en").IsRequired(false);

        // Únicos parciales: una categoría eliminada libera su nombre y su nombre corto.
        builder.HasIndex(c => new { c.RestauranteId, c.NombreNormalizado }).IsUnique()
            .HasFilter("eliminada_en IS NULL").HasDatabaseName("ix_categorias_restaurante_nombre");
        builder.HasIndex(c => new { c.RestauranteId, c.NombreCortoNormalizado }).IsUnique()
            .HasFilter("eliminada_en IS NULL AND nombre_corto IS NOT NULL")
            .HasDatabaseName("ix_categorias_restaurante_nombre_corto");
        builder.HasIndex(c => new { c.RestauranteId, c.Orden, c.Nombre })
            .HasDatabaseName("ix_categorias_restaurante_orden_nombre");
    }
}
