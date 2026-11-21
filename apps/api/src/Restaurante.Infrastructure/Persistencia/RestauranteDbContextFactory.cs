using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>Sólo para herramientas de diseño (<c>dotnet ef</c>); la cadena es un marcador sin secretos.</summary>
public sealed class RestauranteDbContextFactory : IDesignTimeDbContextFactory<RestauranteDbContext>
{
    public RestauranteDbContext CreateDbContext(string[] args) =>
        new(new DbContextOptionsBuilder<RestauranteDbContext>()
            .UseNpgsql("Host=localhost;Database=restaurante_diseno;Username=marcador;Password=marcador")
            .Options);
}
