using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Restaurantes;
using Restaurante.Domain.Usuarios;
using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;

namespace Restaurante.Application.Features.Restaurantes.CrearRestaurante;

public sealed class CrearRestauranteHandler(
    IRestauranteRepository restaurantes,
    IUsuarioRepository usuarios,
    IHasherContrasenas hasher,
    IReloj reloj) : IRequestHandler<CrearRestauranteCommand, RestauranteDto>
{
    public async Task<RestauranteDto> Handle(CrearRestauranteCommand request, CancellationToken cancellationToken)
    {
        var nombre = request.Nombre.Trim();
        if (await restaurantes.ExisteNombreAsync(nombre, null, cancellationToken))
        {
            throw new ConflictoException("Ya existe un restaurante con ese nombre");
        }

        if (await restaurantes.ExisteSlugAsync(request.Slug, cancellationToken))
        {
            throw new ConflictoException("Ya existe un restaurante con ese slug");
        }

        var ahora = reloj.Ahora;
        var paleta = request.Paleta?.ADominio() ?? Paleta.Vacia;
        var restaurante = RestauranteEntidad.Crear(nombre, request.Slug, paleta, ahora);
        var administrador = Usuario.Crear(
            restaurante.Id,
            request.Administrador.NombreUsuario.Trim(),
            hasher.Hashear(request.Administrador.Contrasena),
            Rol.AdministradorGeneral,
            [],
            ahora);

        restaurantes.Agregar(restaurante);
        usuarios.Agregar(administrador);
        await restaurantes.GuardarCambiosAsync(cancellationToken);

        return RestauranteDto.Desde(restaurante);
    }
}
