using Restaurante.Application.Comun.Excepciones;
using Restaurante.Application.Features.Usuarios.CambiarEstadoUsuario;
using Restaurante.Application.Features.Usuarios.CrearUsuario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class UsuariosPorRestauranteTests
{
    private static readonly Guid A = Guid.NewGuid();
    private static readonly Guid B = Guid.NewGuid();
    private readonly RelojFijo _reloj = new();

    private CrearUsuarioHandler HandlerCrear(UsuarioRepositoryEnMemoria repositorio, Guid? restaurante) =>
        new(repositorio, new HasherFalso(), _reloj, new ContextoRestauranteFalso(restaurante));

    [Fact(DisplayName = "002/RF-13: el mismo nombre puede existir en otro restaurante")]
    public async Task MismoNombreEnOtroRestaurante()
    {
        var repositorio = new UsuarioRepositoryEnMemoria(new ContextoRestauranteFalso(A));
        repositorio.Agregar(Usuario.Crear(B, "maria", "hash:x", Rol.Cajero, _reloj.Ahora));

        var dto = await HandlerCrear(repositorio, A)
            .Handle(new CrearUsuarioCommand("maria", "secreta123", "cajero"), CancellationToken.None);

        var creado = Assert.Single(repositorio.Usuarios, u => u.Id == dto.Id);
        Assert.Equal(A, creado.RestauranteId);
        Assert.Equal(2, repositorio.Usuarios.Count);
    }

    [Fact(DisplayName = "002/RF-13: nombre repetido dentro del restaurante da conflicto")]
    public async Task NombreRepetidoEnElRestaurante()
    {
        var repositorio = new UsuarioRepositoryEnMemoria(new ContextoRestauranteFalso(A));
        repositorio.Agregar(Usuario.Crear(A, "maria", "hash:x", Rol.Cajero, _reloj.Ahora));

        await Assert.ThrowsAsync<ConflictoException>(() => HandlerCrear(repositorio, A)
            .Handle(new CrearUsuarioCommand("MARIA", "secreta123", "cocinero"), CancellationToken.None));

        Assert.Single(repositorio.Usuarios);
    }

    [Fact(DisplayName = "002/RF-13: crear usuario sin restaurante en el contexto falla")]
    public async Task CrearSinRestauranteFalla()
    {
        var repositorio = new UsuarioRepositoryEnMemoria();

        await Assert.ThrowsAsync<InvalidOperationException>(() => HandlerCrear(repositorio, null)
            .Handle(new CrearUsuarioCommand("maria", "secreta123", "cajero"), CancellationToken.None));

        Assert.Empty(repositorio.Usuarios);
    }

    [Fact(DisplayName = "002/RF-13: la regla del último administrador se cuenta por restaurante")]
    public async Task UltimoAdministradorPorRestaurante()
    {
        var repositorio = new UsuarioRepositoryEnMemoria(new ContextoRestauranteFalso(A));
        var adminA = Usuario.Crear(A, "jefe", "hash:x", Rol.AdministradorGeneral, _reloj.Ahora);
        repositorio.Agregar(adminA);
        repositorio.Agregar(Usuario.Crear(B, "jefe", "hash:x", Rol.AdministradorGeneral, _reloj.Ahora));

        await Assert.ThrowsAsync<ConflictoException>(() =>
            new CambiarEstadoUsuarioHandler(repositorio, new UsuarioActualFalso(Guid.NewGuid()))
                .Handle(new CambiarEstadoUsuarioCommand(adminA.Id, false), CancellationToken.None));

        Assert.True(adminA.Activo);
    }
}
