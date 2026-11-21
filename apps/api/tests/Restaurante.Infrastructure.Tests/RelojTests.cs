using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Infrastructure.Tests;

public sealed class RelojTests
{
    private sealed class ProveedorFijo(DateTimeOffset ahora) : TimeProvider
    {
        public DateTimeOffset Ahora { get; set; } = ahora;

        public override DateTimeOffset GetUtcNow() => Ahora;
    }

    [Fact(DisplayName = "001/RNF-01: Reloj devuelve la hora del TimeProvider inyectado")]
    public void Reloj_devuelve_la_hora_del_TimeProvider()
    {
        var proveedor = new ProveedorFijo(new DateTimeOffset(2031, 5, 6, 7, 8, 9, TimeSpan.Zero));
        var reloj = new Reloj(proveedor);

        Assert.Equal(proveedor.Ahora, reloj.Ahora);

        proveedor.Ahora = proveedor.Ahora.AddHours(3);

        Assert.Equal(proveedor.Ahora, reloj.Ahora);
    }

    [Fact(DisplayName = "001/RNF-01: la infraestructura registra TimeProvider.System y resuelve IReloj")]
    public void Infraestructura_registra_TimeProvider_System()
    {
        var servicios = new ServiceCollection()
            .AddInfrastructureServices("Host=no-existe;Database=x;Username=u;Password=p", new string('k', 32))
            .BuildServiceProvider();

        Assert.Same(TimeProvider.System, servicios.GetRequiredService<TimeProvider>());
        Assert.IsType<Reloj>(servicios.GetRequiredService<IReloj>());
    }

    [Fact(DisplayName = "001/RNF-03: el DbContext usa la estrategia de reintentos de Npgsql")]
    public void DbContext_usa_estrategia_de_reintentos()
    {
        using var servicios = new ServiceCollection()
            .AddInfrastructureServices("Host=no-existe;Database=x;Username=u;Password=p", new string('k', 32))
            .BuildServiceProvider();
        using var ambito = servicios.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<Restaurante.Infrastructure.Persistencia.RestauranteDbContext>();

        var estrategia = contexto.Database.CreateExecutionStrategy();

        Assert.True(estrategia.RetriesOnFailure);
    }
}
