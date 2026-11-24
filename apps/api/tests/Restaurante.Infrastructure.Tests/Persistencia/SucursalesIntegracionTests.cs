using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Sucursales;
using Restaurante.Infrastructure.Persistencia;

namespace Restaurante.Infrastructure.Tests.Persistencia;

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class SucursalesIntegracionTests(PostgresFixture postgres)
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private static DatosSucursal Datos(
        string nombre, string prefijo, string whatsApp, string? phoneNumberId = null) =>
        new(nombre, "Calle 1", "+525511111111", whatsApp, phoneNumberId, prefijo, ZonasHorarias.PorDefecto);

    private async Task<string> BaseMigradaAsync()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        return cadena;
    }

    /// <summary>Guarda una sucursal con su semana (lunes de apertura a cierre si se indican) y un cierre.</summary>
    private static async Task<(Sucursal Sucursal, CierreTemporal Cierre)> SembrarSucursalAsync(
        string cadena, Guid restauranteId, string slug, DatosSucursal datos, TimeOnly apertura, TimeOnly cierre)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var sucursal = Sucursal.Crear(restauranteId, slug, datos, Ahora);
        var semana = HorarioDia.SemanaCerrada(restauranteId, sucursal.Id);
        semana[0].Cambiar(apertura, cierre);
        var repo = new SucursalRepository(ctx);
        repo.Agregar(sucursal, semana);
        await repo.GuardarCambiosAsync(CancellationToken.None);

        var cierreTemporal = CierreTemporal.Crear(
            restauranteId, sucursal.Id, Ahora.AddDays(1), Ahora.AddDays(2), "Inventario " + slug, Ahora);
        var cierres = new CierreTemporalRepository(ctx);
        cierres.Agregar(cierreTemporal);
        await cierres.GuardarCambiosAsync(CancellationToken.None);
        return (sucursal, cierreTemporal);
    }

    private static async Task<string?> IntentarGuardarAsync(string cadena, Guid restauranteId, Sucursal sucursal)
    {
        await using var ctx = PostgresFixture.CrearContexto(cadena, restauranteId);
        var repo = new SucursalRepository(ctx);
        repo.Agregar(sucursal, HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
        try
        {
            await repo.GuardarCambiosAsync(CancellationToken.None);
            return null;
        }
        catch (ConflictoException ex)
        {
            return ex.Message;
        }
    }

    [Fact(DisplayName = "003/RF-16: con contexto de la sucursal A sólo se ven su horario y sus cierres")]
    public async Task Contexto_de_sucursal_solo_ve_lo_suyo()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var (a, cierreA) = await SembrarSucursalAsync(
            cadena, r, "centro", Datos("Centro", "C", "+525500000001"), new TimeOnly(9, 0), new TimeOnly(17, 0));
        var (b, cierreB) = await SembrarSucursalAsync(
            cadena, r, "norte", Datos("Norte", "N", "+525500000002"), new TimeOnly(18, 0), new TimeOnly(2, 0));

        // Dos contextos vivos a la vez en el mismo proceso: el modelo cacheado se parametriza por contexto.
        await using var ctxA = PostgresFixture.CrearContexto(cadena, r, a.Id);
        await using var ctxB = PostgresFixture.CrearContexto(cadena, r, b.Id);

        var horarioA = await new SucursalRepository(ctxA).ObtenerHorarioAsync(CancellationToken.None);
        Assert.Equal(7, horarioA.Count);
        Assert.All(horarioA, h => Assert.Equal(a.Id, h.SucursalId));
        Assert.Equal(Enum.GetValues<DiaSemana>(), horarioA.Select(h => h.Dia));
        Assert.Equal(new TimeOnly(9, 0), horarioA[0].Apertura);
        Assert.Equal(new TimeOnly(17, 0), horarioA[0].Cierre);
        Assert.True(horarioA[1].Cerrado);

        var cierresRepoA = new CierreTemporalRepository(ctxA);
        var cierresA = await cierresRepoA.ListarDesdeAsync(Ahora, CancellationToken.None);
        Assert.Equal([cierreA.Id], cierresA.Select(c => c.Id));
        Assert.Null(await cierresRepoA.ObtenerPorIdAsync(cierreB.Id, CancellationToken.None));
        Assert.NotNull(await cierresRepoA.ObtenerPorIdAsync(cierreA.Id, CancellationToken.None));

        var horarioB = await new SucursalRepository(ctxB).ObtenerHorarioAsync(CancellationToken.None);
        Assert.All(horarioB, h => Assert.Equal(b.Id, h.SucursalId));
        Assert.True(horarioB[0].CruzaMedianoche);
        var cierresB = await new CierreTemporalRepository(ctxB).ListarDesdeAsync(Ahora, CancellationToken.None);
        Assert.Equal([cierreB.Id], cierresB.Select(c => c.Id));

        // ListarDesdeAsync sólo devuelve cierres con fin posterior al instante indicado.
        Assert.Empty(await cierresRepoA.ListarDesdeAsync(Ahora.AddDays(2), CancellationToken.None));
    }

    [Fact(DisplayName = "003/RF-16: sin sucursal en el contexto no se ve ninguna entidad de sucursal")]
    public async Task Sin_sucursal_no_ve_entidades_de_sucursal()
    {
        var cadena = await BaseMigradaAsync();
        var r = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var (a, cierreA) = await SembrarSucursalAsync(
            cadena, r, "centro", Datos("Centro", "C", "+525500000001"), new TimeOnly(9, 0), new TimeOnly(17, 0));

        await using var ctx = PostgresFixture.CrearContexto(cadena, r);
        var sucursales = new SucursalRepository(ctx);
        var cierres = new CierreTemporalRepository(ctx);

        Assert.Empty(await sucursales.ObtenerHorarioAsync(CancellationToken.None));
        Assert.Empty(await cierres.ListarDesdeAsync(Ahora, CancellationToken.None));
        Assert.Null(await cierres.ObtenerPorIdAsync(cierreA.Id, CancellationToken.None));
        // La sucursal en sí es entidad de restaurante: se sigue viendo.
        Assert.NotNull(await sucursales.ObtenerPorIdAsync(a.Id, CancellationToken.None));
    }

    [Fact(DisplayName = "003/RF-15: las sucursales de otro restaurante no se ven")]
    public async Task Sucursales_de_otro_restaurante_no_se_ven()
    {
        var cadena = await BaseMigradaAsync();
        var r1 = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var r2 = await PostgresFixture.SembrarRestauranteAsync(cadena, "beta");
        var (centro, _) = await SembrarSucursalAsync(
            cadena, r1, "centro", Datos("Centro", "C", "+525500000001", "123"), new TimeOnly(9, 0), new TimeOnly(17, 0));
        var (sur, _) = await SembrarSucursalAsync(
            cadena, r1, "sur", Datos("Abasto", "S", "+525500000003"), new TimeOnly(9, 0), new TimeOnly(17, 0));

        await using (var ctx2 = PostgresFixture.CrearContexto(cadena, r2))
        {
            var repo2 = new SucursalRepository(ctx2);
            Assert.Null(await repo2.ObtenerPorSlugAsync("centro", CancellationToken.None));
            Assert.Null(await repo2.ObtenerPorIdAsync(centro.Id, CancellationToken.None));
            Assert.Empty(await repo2.ListarAsync(CancellationToken.None));
            Assert.False(await repo2.ExisteNombreAsync("Centro", null, CancellationToken.None));
            Assert.False(await repo2.ExisteSlugAsync("centro", CancellationToken.None));
            Assert.False(await repo2.ExistePrefijoAsync("C", null, CancellationToken.None));
            // WhatsApp y phone_number_id son únicos en toda la plataforma.
            Assert.True(await repo2.ExisteWhatsAppAsync("+525500000001", null, CancellationToken.None));
            Assert.False(await repo2.ExisteWhatsAppAsync("+525500000001", centro.Id, CancellationToken.None));
            Assert.True(await repo2.ExistePhoneNumberIdAsync("123", null, CancellationToken.None));
            Assert.False(await repo2.ExistePhoneNumberIdAsync("123", centro.Id, CancellationToken.None));
        }

        await using var ctx1 = PostgresFixture.CrearContexto(cadena, r1);
        var repo1 = new SucursalRepository(ctx1);
        Assert.Equal(centro.Id, (await repo1.ObtenerPorSlugAsync("centro", CancellationToken.None))!.Id);
        Assert.Equal(["Abasto", "Centro"], (await repo1.ListarAsync(CancellationToken.None)).Select(s => s.Nombre));
        Assert.True(await repo1.ExisteNombreAsync("Centro", null, CancellationToken.None));
        Assert.False(await repo1.ExisteNombreAsync("Centro", centro.Id, CancellationToken.None));
        Assert.True(await repo1.ExisteSlugAsync("sur", CancellationToken.None));
        Assert.True(await repo1.ExistePrefijoAsync("S", null, CancellationToken.None));
        Assert.False(await repo1.ExistePrefijoAsync("S", sur.Id, CancellationToken.None));
    }

    [Fact(DisplayName = "003/RF-03: los índices únicos traducen a ConflictoException")]
    public async Task Indices_unicos_traducen_a_conflicto()
    {
        var cadena = await BaseMigradaAsync();
        var r1 = await PostgresFixture.SembrarRestauranteAsync(cadena, "alfa");
        var r2 = await PostgresFixture.SembrarRestauranteAsync(cadena, "beta");
        Assert.Null(await IntentarGuardarAsync(cadena, r1,
            Sucursal.Crear(r1, "centro", Datos("Centro", "C", "+525500000001", "123"), Ahora)));

        Assert.Equal("Ya existe una sucursal con ese prefijo", await IntentarGuardarAsync(cadena, r1,
            Sucursal.Crear(r1, "norte", Datos("Norte", "C", "+525500000002"), Ahora)));
        Assert.Null(await IntentarGuardarAsync(cadena, r2,
            Sucursal.Crear(r2, "norte", Datos("Norte", "C", "+525500000002"), Ahora)));
        Assert.Equal("Ya existe una sucursal con ese WhatsApp", await IntentarGuardarAsync(cadena, r2,
            Sucursal.Crear(r2, "sur", Datos("Sur", "S", "+525500000001"), Ahora)));
        Assert.Equal("Ya existe una sucursal con ese nombre", await IntentarGuardarAsync(cadena, r1,
            Sucursal.Crear(r1, "otra", Datos("Centro", "O", "+525500000009"), Ahora)));
        Assert.Equal("Ya existe una sucursal con ese slug", await IntentarGuardarAsync(cadena, r1,
            Sucursal.Crear(r1, "centro", Datos("Otra", "O", "+525500000009"), Ahora)));
        Assert.Equal("Ya existe una sucursal con ese phone_number_id", await IntentarGuardarAsync(cadena, r2,
            Sucursal.Crear(r2, "este", Datos("Este", "E", "+525500000008", "123"), Ahora)));
        // Varias sucursales sin phone_number_id no chocan entre sí (índice filtrado).
        Assert.Null(await IntentarGuardarAsync(cadena, r2,
            Sucursal.Crear(r2, "oeste", Datos("Oeste", "W", "+525500000007"), Ahora)));
    }
}
