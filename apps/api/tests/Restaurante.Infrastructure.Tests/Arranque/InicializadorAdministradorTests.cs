using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Arranque;
using Restaurante.Infrastructure.Persistencia;
using Restaurante.Infrastructure.Seguridad;
using Restaurante.Infrastructure.Tests.Persistencia;

namespace Restaurante.Infrastructure.Tests.Arranque;

public sealed class InicializadorAdministradorTests
{
    private static readonly DateTimeOffset Ahora = new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private sealed class RepoDoble(bool existe) : IUsuarioRepository
    {
        public List<Usuario> Agregados { get; } = [];
        public int Guardados { get; private set; }
        public Task<bool> ExisteAlgunoAsync(CancellationToken ct) => Task.FromResult(existe);
        public void Agregar(Usuario usuario) => Agregados.Add(usuario);
        public Task GuardarCambiosAsync(CancellationToken ct) { Guardados++; return Task.CompletedTask; }
        public Task<Usuario?> ObtenerPorIdAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();

        public Task<Usuario?> ObtenerParaSesionAsync(Guid id, CancellationToken ct) => throw new NotSupportedException();
        public Task<Usuario?> ObtenerPorNombreNormalizadoAsync(string n, CancellationToken ct) => throw new NotSupportedException();
        public Task<IReadOnlyList<Usuario>> ListarAsync(CancellationToken ct) => throw new NotSupportedException();
        public Task<int> ContarAdministradoresActivosAsync(CancellationToken ct) => throw new NotSupportedException();
    }

    private sealed class HasherDoble : IHasherContrasenas
    {
        public string Hashear(string contrasena) => "hash:" + contrasena;
        public bool Verificar(string hash, string contrasena) => hash == "hash:" + contrasena;
    }

    private sealed class RelojDoble : IReloj
    {
        public DateTimeOffset Ahora => InicializadorAdministradorTests.Ahora;
    }

    private static InicializadorAdministrador Crear(RepoDoble repo) => new(repo, new HasherDoble(), new RelojDoble());

    private static Dictionary<string, string?> Entorno(string? usuario, string? contrasena) =>
        new() { ["ADMIN_USERNAME"] = usuario, ["ADMIN_PASSWORD"] = contrasena };

    [Fact(DisplayName = "002/RF-17: el administrador inicial se crea como administrador_plataforma")]
    public async Task Sin_usuarios_crea_administrador()
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno("root", "Secreta123"), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Null(r.Error);
        var admin = Assert.Single(repo.Agregados);
        Assert.Equal("root", admin.NombreUsuario);
        Assert.Equal(Rol.AdministradorPlataforma, admin.Rol);
        Assert.True(admin.Activo);
        Assert.Equal(Ahora, admin.CreadoEn);
        Assert.Equal("hash:Secreta123", admin.HashContrasena);
        Assert.NotEqual("Secreta123", admin.HashContrasena);
        Assert.Equal(1, repo.Guardados);
    }

    [Fact(DisplayName = "001/RF-09: con usuarios no crea ni modifica nada")]
    public async Task Con_usuarios_no_hace_nada()
    {
        var repo = new RepoDoble(true);

        var r = await Crear(repo).InicializarAsync(Entorno("root", "Secreta123"), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Empty(repo.Agregados);
        Assert.Equal(0, repo.Guardados);
    }

    [Theory(DisplayName = "001/RF-10: sin usuarios y sin ADMIN_PASSWORD devuelve error con el nombre de la variable")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Sin_password_devuelve_error(string? valor)
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno("root", valor), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("ADMIN_PASSWORD", r.Error);
        Assert.DoesNotContain("root", r.Error);
        Assert.Empty(repo.Agregados);
        Assert.Equal(0, repo.Guardados);
    }

    [Theory(DisplayName = "001/RF-10: sin usuarios y sin ADMIN_USERNAME devuelve error con el nombre de la variable")]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Sin_usuario_devuelve_error(string? valor)
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno(valor, "Secreta123"), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("ADMIN_USERNAME", r.Error);
        Assert.DoesNotContain("Secreta123", r.Error);
        Assert.Empty(repo.Agregados);
    }

    [Theory(DisplayName = "001/RF-10: ADMIN_USERNAME con formato inválido devuelve error sin revelar el valor")]
    [InlineData("ab")]
    [InlineData("nombre con espacios")]
    [InlineData("ñandú")]
    [InlineData("a@b.com")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa")]
    public async Task Usuario_con_formato_invalido_devuelve_error(string nombre)
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno(nombre, "Secreta123"), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("ADMIN_USERNAME", r.Error);
        Assert.Contains("3 y 50", r.Error);
        Assert.DoesNotContain(nombre, r.Error);
        Assert.DoesNotContain("Secreta123", r.Error);
        Assert.Empty(repo.Agregados);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact(DisplayName = "001/RF-10: ADMIN_USERNAME se valida tras recortar espacios")]
    public async Task Usuario_valido_con_espacios_alrededor_se_acepta_recortado()
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno("  root  ", "Secreta123"), CancellationToken.None);

        Assert.True(r.Ok);
        Assert.Equal("root", Assert.Single(repo.Agregados).NombreUsuario);
    }

    [Theory(DisplayName = "001/RF-10: ADMIN_PASSWORD de menos de 8 caracteres devuelve error sin revelar el valor")]
    [InlineData("1234567")]
    [InlineData("corta")]
    public async Task Contrasena_corta_devuelve_error(string contrasena)
    {
        var repo = new RepoDoble(false);

        var r = await Crear(repo).InicializarAsync(Entorno("root", contrasena), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("ADMIN_PASSWORD", r.Error);
        Assert.Contains("8", r.Error);
        Assert.DoesNotContain(contrasena, r.Error);
        Assert.DoesNotContain("root", r.Error);
        Assert.Empty(repo.Agregados);
        Assert.Equal(0, repo.Guardados);
    }

    [Fact(DisplayName = "001/RF-10: variable ausente del diccionario cuenta como faltante")]
    public async Task Variable_ausente_devuelve_error()
    {
        var r = await Crear(new RepoDoble(false)).InicializarAsync(new Dictionary<string, string?>(), CancellationToken.None);

        Assert.False(r.Ok);
        Assert.Contains("ADMIN_USERNAME", r.Error);
    }
}

[Trait("Category", "Integracion")]
[Collection(PostgresCollection.Nombre)]
public sealed class InicializadorAdministradorIntegracionTests(PostgresFixture postgres)
{
    private sealed class RelojFijo : IReloj
    {
        public DateTimeOffset Ahora => new(2030, 1, 1, 0, 0, 0, TimeSpan.Zero);
    }

    [Fact(DisplayName = "001/RF-08: crea el administrador en PostgreSQL")]
    public async Task Crea_administrador_en_postgres()
    {
        var cadena = await postgres.CrearBaseAsync();
        await using var ctx = PostgresFixture.CrearContexto(cadena);
        await new AplicadorMigraciones(ctx).AplicarAsync(true, CancellationToken.None);
        var hasher = new HasherContrasenas();
        var inicializador = new InicializadorAdministrador(new UsuarioRepository(ctx), hasher, new RelojFijo());
        var entorno = new Dictionary<string, string?> { ["ADMIN_USERNAME"] = "Root", ["ADMIN_PASSWORD"] = "Secreta123" };

        var r = await inicializador.InicializarAsync(entorno, CancellationToken.None);

        Assert.True(r.Ok);
        await using var ctx2 = PostgresFixture.CrearContexto(cadena);
        var admin = await new UsuarioRepository(ctx2).ObtenerPorNombreNormalizadoAsync("root", CancellationToken.None);
        Assert.NotNull(admin);
        Assert.Equal(Rol.AdministradorPlataforma, admin.Rol);
        Assert.True(admin.Activo);
        Assert.True(hasher.Verificar(admin.HashContrasena, "Secreta123"));
    }
}
