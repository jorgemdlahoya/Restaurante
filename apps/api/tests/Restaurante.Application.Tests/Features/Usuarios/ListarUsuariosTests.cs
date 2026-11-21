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
        var repositorio = new UsuarioRepositoryEnMemoria();
        var ahora = new RelojFijo().Ahora;
        repositorio.Agregar(Usuario.Crear("Zoe", "hash:z", Rol.Caja, ahora));
        repositorio.Agregar(Usuario.Crear("ana", "hash:a", Rol.Administrador, ahora));
        var desactivado = Usuario.Crear("Beto", "hash:b", Rol.Cocina, ahora);
        desactivado.Desactivar();
        repositorio.Agregar(desactivado);

        var lista = await new ListarUsuariosHandler(repositorio).Handle(new ListarUsuariosQuery(), CancellationToken.None);

        Assert.Equal(new[] { "ana", "Beto", "Zoe" }, lista.Select(u => u.NombreUsuario));
        Assert.Equal(new[] { "administrador", "cocina", "caja" }, lista.Select(u => u.Rol));
        Assert.Equal(new[] { true, false, true }, lista.Select(u => u.Activo));
        Assert.DoesNotContain(typeof(UsuarioDto).GetProperties(), p => p.Name.Contains("Contrasena") || p.Name.Contains("Hash"));
    }
}
