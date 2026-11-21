using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteDbContext(DbContextOptions<RestauranteDbContext> options) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestauranteDbContext).Assembly);
}
