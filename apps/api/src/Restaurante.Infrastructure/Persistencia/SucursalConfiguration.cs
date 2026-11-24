using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Sucursales;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class SucursalConfiguration : IEntityTypeConfiguration<Sucursal>
{
    public void Configure(EntityTypeBuilder<Sucursal> builder)
    {
        builder.ToTable("sucursales");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(s => s.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        builder.HasOne<RestauranteEntidad>().WithMany().HasForeignKey(s => s.RestauranteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(s => s.Nombre).HasColumnName("nombre").HasMaxLength(Sucursal.LongitudMaximaNombre).IsRequired();
        builder.Property(s => s.Direccion).HasColumnName("direccion").HasMaxLength(Sucursal.LongitudMaximaDireccion)
            .IsRequired();
        builder.Property(s => s.Telefono).HasColumnName("telefono").HasMaxLength(13).IsRequired();
        builder.Property(s => s.Slug).HasColumnName("slug").HasMaxLength(40).IsRequired();
        builder.Property(s => s.WhatsApp).HasColumnName("whatsapp").HasMaxLength(13).IsRequired();
        builder.Property(s => s.PhoneNumberId).HasColumnName("phone_number_id").HasMaxLength(30).IsRequired(false);
        builder.Property(s => s.Prefijo).HasColumnName("prefijo").HasMaxLength(3).IsRequired();
        builder.Property(s => s.ZonaHoraria).HasColumnName("zona_horaria").HasMaxLength(40).IsRequired();
        builder.Property(s => s.Activa).HasColumnName("activa").IsRequired();
        builder.Property(s => s.CreadaEn).HasColumnName("creada_en").IsRequired();

        builder.HasIndex(s => new { s.RestauranteId, s.Nombre }).IsUnique()
            .HasDatabaseName("ix_sucursales_restaurante_nombre");
        builder.HasIndex(s => new { s.RestauranteId, s.Slug }).IsUnique()
            .HasDatabaseName("ix_sucursales_restaurante_slug");
        builder.HasIndex(s => new { s.RestauranteId, s.Prefijo }).IsUnique()
            .HasDatabaseName("ix_sucursales_restaurante_prefijo");
        // WhatsApp y phone_number_id identifican el número de Meta: únicos en toda la plataforma.
        builder.HasIndex(s => s.WhatsApp).IsUnique().HasDatabaseName("ix_sucursales_whatsapp");
        builder.HasIndex(s => s.PhoneNumberId).IsUnique().HasFilter("phone_number_id IS NOT NULL")
            .HasDatabaseName("ix_sucursales_phone_number_id");
    }
}
