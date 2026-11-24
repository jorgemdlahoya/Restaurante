using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Comun;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.CrearSucursal;

public sealed class CrearSucursalHandler(
    ISucursalRepository sucursales,
    IReloj reloj,
    IContextoRestaurante contexto) : IRequestHandler<CrearSucursalCommand, SucursalDto>
{
    public async Task<SucursalDto> Handle(CrearSucursalCommand request, CancellationToken cancellationToken)
    {
        var restauranteId = contexto.RestauranteId
            ?? throw new InvalidOperationException("No se puede crear una sucursal fuera del ámbito de un restaurante.");

        var datos = request.Datos.ADominio();
        var nombre = datos.Nombre.Trim();
        var whatsApp = TelefonoMexico.Normalizar(datos.WhatsApp);

        if (await sucursales.ExisteNombreAsync(nombre, null, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese nombre");
        }

        if (await sucursales.ExisteSlugAsync(request.Slug, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese slug");
        }

        if (await sucursales.ExistePrefijoAsync(datos.Prefijo, null, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese prefijo");
        }

        if (whatsApp is not null && await sucursales.ExisteWhatsAppAsync(whatsApp, null, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese WhatsApp");
        }

        if (datos.PhoneNumberId is not null
            && await sucursales.ExistePhoneNumberIdAsync(datos.PhoneNumberId, null, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese phone_number_id");
        }

        var sucursal = Sucursal.Crear(restauranteId, request.Slug, datos, reloj.Ahora);
        sucursales.Agregar(sucursal, HorarioDia.SemanaCerrada(restauranteId, sucursal.Id));
        await sucursales.GuardarCambiosAsync(cancellationToken);

        return SucursalDto.Desde(sucursal);
    }
}
