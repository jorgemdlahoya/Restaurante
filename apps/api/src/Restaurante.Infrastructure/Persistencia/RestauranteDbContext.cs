using Microsoft.EntityFrameworkCore;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Menu;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Sucursales;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Persistencia;

public sealed class RestauranteDbContext(
    DbContextOptions<RestauranteDbContext> options,
    IContextoRestaurante contexto,
    IContextoSucursal contextoSucursal) : DbContext(options)
{
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    public DbSet<RestauranteEntidad> Restaurantes => Set<RestauranteEntidad>();
    public DbSet<LogoRestaurante> Logos => Set<LogoRestaurante>();
    public DbSet<UsuarioSucursal> UsuariosSucursales => Set<UsuarioSucursal>();
    public DbSet<Sucursal> Sucursales => Set<Sucursal>();
    public DbSet<HorarioDia> Horarios => Set<HorarioDia>();
    public DbSet<CierreTemporal> CierresTemporales => Set<CierreTemporal>();
    public DbSet<Categoria> Categorias => Set<Categoria>();
    public DbSet<Platillo> Platillos => Set<Platillo>();

    /// <summary>
    /// Propiedad de instancia: EF la parametriza en cada consulta con el valor de este contexto,
    /// así el modelo cacheado sirve para todos los restaurantes.
    /// </summary>
    private Guid? RestauranteActualId => contexto.RestauranteId;

    /// <summary>Igual que <see cref="RestauranteActualId"/>, para la sucursal (filtro de <c>IEntidadDeSucursal</c>).</summary>
    private Guid? SucursalActualId => contextoSucursal.SucursalId;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(RestauranteDbContext).Assembly);

        // Filtro global (RF-12): cada contexto sólo ve los usuarios de su restaurante; con contexto nulo,
        // sólo los de plataforma (la semántica nula de C# traduce null == null como verdadero).
        modelBuilder.Entity<Usuario>().HasQueryFilter(u => u.RestauranteId == RestauranteActualId);

        // 003/RF-15, RF-16: entidades de restaurante y de sucursal, por convención.
        FiltrosGlobales.Aplicar(modelBuilder, this);
    }
}
