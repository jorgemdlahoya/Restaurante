using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class UsuarioConfiguration : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        var rolComoTexto = new ValueConverter<Rol, string>(
            rol => rol.ToString().ToLowerInvariant(),
            texto => Enum.Parse<Rol>(texto, true));

        builder.ToTable("usuarios");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnName("id");
        builder.Property(u => u.NombreUsuario).HasColumnName("nombre_usuario").HasMaxLength(50).IsRequired();
        builder.Property(u => u.NombreUsuarioNormalizado).HasColumnName("nombre_usuario_normalizado")
            .HasMaxLength(50).IsRequired();
        builder.Property(u => u.HashContrasena).HasColumnName("hash_contrasena").HasColumnType("text").IsRequired();
        builder.Property(u => u.Rol).HasColumnName("rol").HasMaxLength(20).HasConversion(rolComoTexto).IsRequired();
        builder.Property(u => u.Activo).HasColumnName("activo").IsRequired();
        builder.Property(u => u.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.HasIndex(u => u.NombreUsuarioNormalizado).IsUnique().HasDatabaseName("ix_usuarios_nombre_usuario_normalizado");
    }
}
