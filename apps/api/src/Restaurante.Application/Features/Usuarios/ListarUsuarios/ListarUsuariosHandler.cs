using MediatR;
using Restaurante.Application.Abstracciones;

namespace Restaurante.Application.Features.Usuarios.ListarUsuarios;

public sealed class ListarUsuariosHandler(IUsuarioRepository usuarios)
    : IRequestHandler<ListarUsuariosQuery, IReadOnlyList<UsuarioDto>>
{
    public async Task<IReadOnlyList<UsuarioDto>> Handle(ListarUsuariosQuery request, CancellationToken cancellationToken)
    {
        var lista = await usuarios.ListarAsync(cancellationToken);
        return lista
            .OrderBy(u => u.NombreUsuarioNormalizado, StringComparer.Ordinal)
            .Select(UsuarioDto.Desde)
            .ToList();
    }
}
