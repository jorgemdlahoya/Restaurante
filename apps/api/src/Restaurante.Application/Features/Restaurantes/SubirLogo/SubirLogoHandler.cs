using System.Security.Cryptography;
using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Restaurantes;

namespace Restaurante.Application.Features.Restaurantes.SubirLogo;

public sealed class SubirLogoHandler(IRestauranteRepository restaurantes, IReloj reloj)
    : IRequestHandler<SubirLogoCommand, Unit>
{
    public async Task<Unit> Handle(SubirLogoCommand request, CancellationToken cancellationToken)
    {
        _ = await restaurantes.ObtenerPorIdAsync(request.RestauranteId, cancellationToken)
            ?? throw new NoEncontradoException("Restaurante no encontrado");

        var tipo = DetectorTipoImagen.Detectar(request.Logo)
            ?? throw new InvalidOperationException("El logo debe ser PNG, JPG o WebP");
        var etag = Convert.ToHexStringLower(SHA256.HashData(request.Logo));

        var existente = await restaurantes.ObtenerLogoAsync(request.RestauranteId, cancellationToken);
        if (existente is null)
        {
            restaurantes.AgregarLogo(LogoRestaurante.Crear(request.RestauranteId, request.Logo, tipo, etag, reloj.Ahora));
        }
        else
        {
            existente.Reemplazar(request.Logo, tipo, etag, reloj.Ahora);
        }

        await restaurantes.GuardarCambiosAsync(cancellationToken);
        return Unit.Value;
    }
}
