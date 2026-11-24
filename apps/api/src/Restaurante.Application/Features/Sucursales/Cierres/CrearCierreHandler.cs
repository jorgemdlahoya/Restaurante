using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.Cierres;

/// <summary>Crea un cierre temporal (RF-10): interpreta las fechas en la zona de la sucursal y las guarda en UTC.</summary>
public sealed class CrearCierreHandler(
    ISucursalRepository sucursales, ICierreTemporalRepository cierres, IContextoSucursal contexto, IReloj reloj)
    : IRequestHandler<CrearCierreCommand, CierreTemporalDto>
{
    public async Task<CierreTemporalDto> Handle(CrearCierreCommand request, CancellationToken cancellationToken)
    {
        var (sucursal, zona) = await ZonaDeSucursal.ObtenerAsync(sucursales, contexto, cancellationToken);
        var ahora = reloj.Ahora;

        // El validador (pipeline) garantiza que las fechas presentes tienen el formato correcto.
        var inicio = ahora;
        if (request.Inicio is not null)
        {
            HoraLocal.TryLeer(request.Inicio, out var inicioLocal);
            inicio = HoraLocal.AUtc(inicioLocal, zona);
        }

        HoraLocal.TryLeer(request.Fin, out var finLocal);
        var fin = HoraLocal.AUtc(finLocal, zona);

        // Traduce a 400 las reglas de CierreTemporal.Crear (mismo orden que el dominio).
        if (fin <= inicio)
        {
            throw Invalido("El fin debe ser posterior al inicio");
        }

        if (fin <= ahora)
        {
            throw Invalido("El fin no puede estar en el pasado");
        }

        var cierre = CierreTemporal.Crear(sucursal.RestauranteId, sucursal.Id, inicio, fin, request.Motivo, ahora);
        cierres.Agregar(cierre);
        await cierres.GuardarCambiosAsync(cancellationToken);
        return CierreTemporalDto.Desde(cierre, zona);
    }

    private static ValidationException Invalido(string mensaje) =>
        new([new ValidationFailure("fin", mensaje)]);
}
