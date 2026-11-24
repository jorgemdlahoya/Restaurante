using Restaurante.Application.Features.Usuarios;
using Restaurante.Application.Features.Usuarios.ListarUsuarios;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Features.Usuarios;

public class ListarUsuariosTests
{
    [Fact(DisplayName = "001/RF-14: lista usuarios ordenados sin datos sensibles")]
    public async Task ListaOrdenadosSinDatosSensibles()
    {
        var restaurante = Guid.NewGuid();
        var contexto = new ContextoRestauranteFalso(restaurante);
        var repositorio = new UsuarioRepositoryEnMemoria(contexto);
        var ahora = new RelojFijo().Ahora;
        repositorio.Agregar(Usuario.Crear(restaurante, "Zoe", "hash:z", Rol.Cajero, [Guid.NewGuid()], ahora));
        var ana = Usuario.Crear(restaurante, "ana", "hash:a", Rol.AdministradorGeneral, [], ahora);
        repositorio.Agregar(ana);
        var desactivado = Usuario.Crear(restaurante, "Beto", "hash:b", Rol.Cocinero, [Guid.NewGuid()], ahora);
        desactivado.Desactivar();
        repositorio.Agregar(desactivado);
        // Desde 004/RF-03 consulta un administrador general del restaurante (ana).
        var resolutor = new ResolutorActor(
            new UsuarioActualFalso(ana.Id), new SucursalRepositoryEnMemoria(contexto));

        var lista = await new ListarUsuariosHandler(repositorio, resolutor)
            .Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(new[] { "ana", "Beto", "Zoe" }, lista.Select(u => u.NombreUsuario));
        Assert.Equal(new[] { "administrador_general", "cocinero", "cajero" }, lista.Select(u => u.Rol));
        Assert.Equal(new[] { true, false, true }, lista.Select(u => u.Activo));
        Assert.DoesNotContain(typeof(UsuarioDto).GetProperties(), p => p.Name.Contains("Contrasena") || p.Name.Contains("Hash"));
    }
}
