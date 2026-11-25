using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Usuarios.ListarUsuarios;

/// <summary>
/// Usuarios que el actor puede ver (004/RF-03, RF-07), ordenados por nombre, con sus sucursales y si puede
/// gestionarlos. Cajero, cocinero o administrador de sucursal con su sucursal inactiva: 403.
/// </summary>
public sealed class ListarUsuariosHandler(IUsuarioRepository usuarios, ResolutorActor resolutor)
    : IRequestHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    public async Task<IReadOnlyList<UsuarioDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var actor = await resolutor.ObtenerAsync(cancellationToken);
        var lista = await usuarios.ListarAsync(cancellationToken);
        var visibles = lista
            .Where(u => AlcanceGestionUsuarios.PuedeVer(actor, u))
            .OrderBy(u => u.NombreUsuarioNormalizado, StringComparer.Ordinal);
        return await resolutor.ADtosAsync(actor, visibles, cancellationToken);
    }
}
