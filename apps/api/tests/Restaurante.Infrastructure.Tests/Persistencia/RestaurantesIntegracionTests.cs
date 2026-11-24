using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Restaurantes;
using Restaurante.Infrastructure.Persistencia;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class RestaurantesIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        return cadena;
    }

    [Fact(DisplayName = "002/RF-03: guarda un restaurante con paleta parcial y lo lista ordenado por nombre")]
    public async Task Guarda_con_paleta_parcial_y_lista_ordenado()
    {
        var cadena = await BaseMigradaAsync();
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            var repo = new RestauranteRepository(ctx);
            repo.Agregar(RestauranteEntidad.Crear("Beta", "beta", Paleta.Vacia, Ahora));
            repo.Agregar(RestauranteEntidad.Crear("Alfa", "alfa", new Paleta("#F97316", null, "#111111", null, null), Ahora));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using var ctx2 = PostgresFixture.CrearContexto(cadena);
        var repo2 = new RestauranteRepository(ctx2);
        var lista = await repo2.ListarAsync(CancellationToken.None);

        Assert.Equal(["Alfa", "Beta"], lista.Select(r => r.Nombre));
        Assert.Equal("#F97316", lista[0].Paleta.Principal);
        Assert.Null(lista[0].Paleta.Acento);
        Assert.Equal("#111111", lista[0].Paleta.Oscuro);
        Assert.Equal(Paleta.Vacia, lista[1].Paleta);
        Assert.True(await repo2.ExisteNombreAsync("Alfa", null, CancellationToken.None));
        Assert.False(await repo2.ExisteNombreAsync("Alfa", lista[0].Id, CancellationToken.None));
        Assert.True(await repo2.ExisteSlugAsync("beta", CancellationToken.None));
        Assert.NotNull(await repo2.ObtenerPorSlugAsync("beta", CancellationToken.None));
        Assert.NotNull(await repo2.ObtenerPorIdAsync(lista[0].Id, CancellationToken.None));
    }

    [Fact(DisplayName = "002/RF-03: un slug repetido al guardar lanza ConflictoException")]
    public async Task Slug_repetido_lanza_conflicto()
    {
        var cadena = await BaseMigradaAsync();
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            var repo = new RestauranteRepository(ctx);
            repo.Agregar(RestauranteEntidad.Crear("Alfa", "alfa", Paleta.Vacia, Ahora));
            await repo.GuardarCambiosAsync(CancellationToken.None);
        }

        await using var ctx2 = PostgresFixture.CrearContexto(cadena);
        var repo2 = new RestauranteRepository(ctx2);
        repo2.Agregar(RestauranteEntidad.Crear("Otro", "alfa", Paleta.Vacia, Ahora));
        var ex = await Assert.ThrowsAsync<ConflictoException>(() => repo2.GuardarCambiosAsync(CancellationToken.None));
        Assert.Equal("Ya existe un restaurante con ese slug", ex.Message);
    }

    [Fact(DisplayName = "002/RF-05: el logo se guarda en bytea y se lee igual")]
    public async Task Logo_se_guarda_y_se_lee_igual()
    {
        var cadena = await BaseMigradaAsync();
        var bytes = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0, 1, 2, 255 };
        Guid conLogo, sinLogo;
        await using (var ctx = PostgresFixture.CrearContexto(cadena))
        {
            var repo = new RestauranteRepository(ctx);
            var a = RestauranteEntidad.Crear("Alfa", "alfa", Paleta.Vacia, Ahora);
            var b = RestauranteEntidad.Crear("Beta", "beta", Paleta.Vacia, Ahora);
            repo.Agregar(a);
            repo.Agregar(b);
            repo.AgregarLogo(LogoRestaurante.Crear(a.Id, bytes, "image/png", "etag-123", Ahora));
            await repo.GuardarCambiosAsync(CancellationToken.None);
            conLogo = a.Id;
            sinLogo = b.Id;
        }

        await using var ctx2 = PostgresFixture.CrearContexto(cadena);
        var repo2 = new RestauranteRepository(ctx2);
        var logo = await repo2.ObtenerLogoAsync(conLogo, CancellationToken.None);

        Assert.NotNull(logo);
        Assert.Equal(bytes, logo!.Contenido);
        Assert.Equal("image/png", logo.Tipo);
        Assert.Equal("etag-123", logo.ETag);
        Assert.Equal("etag-123", await repo2.ObtenerEtagLogoAsync(conLogo, CancellationToken.None));
        Assert.Null(await repo2.ObtenerEtagLogoAsync(sinLogo, CancellationToken.None));
        Assert.Null(await repo2.ObtenerLogoAsync(sinLogo, CancellationToken.None));
    }
}
