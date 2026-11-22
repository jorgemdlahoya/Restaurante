using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        var rolComoTexto = new ValueConverter<Rol, string>(
            rol => rol.ComoTexto(),
            texto => RolExtensiones.DesdeTexto(texto));

        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.RestauranteId).HasColumnName("restaurante_id");
        builder.HasOne<RestauranteEntidad>().WithMany().HasForeignKey(u => u.RestauranteId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Property(u => u.NombreUsuario).HasColumnName("nombre_usuario").HasMaxLength(50).IsRequired();
        builder.Property(u => u.NombreUsuarioNormalizado).HasColumnName("nombre_usuario_normalizado")
            .HasMaxLength(50).IsRequired();
        builder.Property(u => u.HashContrasena).HasColumnName("hash_contrasena").HasColumnType("text").IsRequired();
        builder.Property(u => u.Rol).HasColumnName("rol").HasMaxLength(30).HasConversion(rolComoTexto).IsRequired();
        builder.Property(u => u.Activo).HasColumnName("activo").IsRequired();
        builder.Property(u => u.CreadoEn).HasColumnName("creado_en").IsRequired();
        // Único por restaurante; NULLS NOT DISTINCT lo hace único también entre administradores de plataforma.
        builder.HasIndex(u => new { u.RestauranteId, u.NombreUsuarioNormalizado }).IsUnique()
            .AreNullsDistinct(false).HasDatabaseName("ix_usuarios_restaurante_nombre");
    }
}
