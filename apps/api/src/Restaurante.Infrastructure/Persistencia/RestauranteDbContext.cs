using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteDbContext(DbContextOptions<RestauranteDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RestauranteEntidad> Restaurantes => Set<RestauranteEntidad>();
    public DbSet<LogoRestaurante> Logos => Set<LogoRestaurante>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestauranteDbContext).Assembly);
}
