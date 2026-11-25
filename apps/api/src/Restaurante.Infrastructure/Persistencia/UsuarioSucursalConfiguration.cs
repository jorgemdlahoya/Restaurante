using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class UsuarioSucursalConfiguration : IEntityTypeConfiguration<UsuarioSucursal>
{
    public void Configure(EntityTypeBuilder<UsuarioSucursal> builder)
    {
        builder.ToTable("usuarios_sucursales");
        builder.HasKey(s => new { s.UsuarioId, s.SucursalId });
        builder.Property(s => s.UsuarioId).HasColumnName("usuario_id");
        builder.Property(s => s.SucursalId).HasColumnName("sucursal_id");
        builder.Property(s => s.RestauranteId).HasColumnName("restaurante_id").IsRequired();
        // Una sucursal con usuarios asignados no se borra.
        builder.HasOne<Sucursal>().WithMany().HasForeignKey(s => s.SucursalId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(s => s.SucursalId).HasDatabaseName("ix_usuarios_sucursales_sucursal");
    }
}
