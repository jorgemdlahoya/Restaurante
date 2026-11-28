using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Menu;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class PlatilloConfiguration : IEntityTypeConfiguration<Platillo>
{
    public void Configure(EntityTypeBuilder<Platillo> builder)
    {
        builder.ToTable("platillos", t => t.HasCheckConstraint("ck_platillos_orden", "orden >= 1"));
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(p => p.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        builder.HasOne<RestauranteEntidad>().WithMany().HasForeignKey(p => p.RestauranteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.CategoriaId).HasColumnName("categoria_id").IsRequired();
        builder.HasOne<Categoria>().WithMany().HasForeignKey(p => p.CategoriaId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(p => p.Nombre).HasColumnName("nombre").HasMaxLength(Platillo.LongitudMaximaNombre).IsRequired();
        builder.Property(p => p.NombreNormalizado).HasColumnName("nombre_normalizado")
            .HasMaxLength(Platillo.LongitudMaximaNombre).IsRequired();
        builder.Property(p => p.NombreCorto).HasColumnName("nombre_corto")
            .HasMaxLength(Platillo.LongitudMaximaNombreCorto).IsRequired(false);
        builder.Property(p => p.NombreCortoNormalizado).HasColumnName("nombre_corto_normalizado")
            .HasMaxLength(Platillo.LongitudMaximaNombreCorto).IsRequired(false);
        builder.Property(p => p.Descripcion).HasColumnName("descripcion")
            .HasMaxLength(Platillo.LongitudMaximaDescripcion).IsRequired(false);
        builder.Property(p => p.Orden).HasColumnName("orden").IsRequired();
        builder.Property(p => p.Activo).HasColumnName("activo").IsRequired();
        builder.Property(p => p.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(p => p.EliminadaEn).HasColumnName("eliminada_en").IsRequired(false);

        // text[] sobre el campo de respaldo: conserva el orden; la lista se reemplaza entera al editar,
        // y EF compara su contenido para detectar el cambio.
        builder.Ignore(p => p.Ingredientes);
        builder.Property<List<string>>("_ingredientes")
            .HasColumnName("ingredientes").HasColumnType("text[]").HasDefaultValueSql("'{}'").IsRequired();

        // Únicos parciales: un platillo eliminado libera su nombre y su nombre corto.
        builder.HasIndex(p => new { p.RestauranteId, p.NombreNormalizado }).IsUnique()
            .HasFilter("eliminada_en IS NULL").HasDatabaseName("ix_platillos_restaurante_nombre");
        builder.HasIndex(p => new { p.RestauranteId, p.NombreCortoNormalizado }).IsUnique()
            .HasFilter("eliminada_en IS NULL AND nombre_corto IS NOT NULL")
            .HasDatabaseName("ix_platillos_restaurante_nombre_corto");
        builder.HasIndex(p => new { p.CategoriaId, p.Orden, p.Nombre })
            .HasDatabaseName("ix_platillos_categoria_orden_nombre");
    }
}
