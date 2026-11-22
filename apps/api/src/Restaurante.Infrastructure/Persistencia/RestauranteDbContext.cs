using Microsoft.EntityFrameworkCore;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteDbContext(
    DbContextOptions<RestauranteDbContext> options, IContextoRestaurante contexto) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RestauranteEntidad> Restaurantes => Set<RestauranteEntidad>();
    public DbSet<LogoRestaurante> Logos => Set<LogoRestaurante>();

    /// <summary>
    /// Propiedad de instancia: EF la parametriza en cada consulta con el valor de este contexto,
    /// así el modelo cacheado sirve para todos los restaurantes.
    /// </summary>
    private Guid? RestauranteActualId => contexto.RestauranteId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestauranteDbContext).Assembly);

        // Filtro global (RF-12): cada contexto sólo ve los usuarios de su restaurante; con contexto nulo,
        // sólo los de plataforma (la semántica nula de C# traduce null == null como verdadero).
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.RestauranteId == RestauranteActualId);
    }
}
