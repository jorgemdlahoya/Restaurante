using MediatR;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun.Excepciones;
using Restaurante.Domain.Comun;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Features.Sucursales.EditarSucursal;

public sealed class EditarSucursalHandler(ISucursalRepository sucursales)
    : IRequestHandler<EditarSucursalCommand, SucursalDto>
{
    public async Task<SucursalDto> Handle(EditarSucursalCommand request, CancellationToken cancellationToken)
    {
        // El repositorio filtra por restaurante: una sucursal ajena es indistinguible de una inexistente.
        var sucursal = await sucursales.ObtenerPorIdAsync(request.Id, cancellationToken)
            ?? throw new NoEncontradoException("Sucursal no encontrada");

        var datos = request.Datos.ADominio();
        var nombre = datos.Nombre.Trim();
        var whatsApp = TelefonoMexico.Normalizar(datos.WhatsApp);

        if (await sucursales.ExisteNombreAsync(nombre, sucursal.Id, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese nombre");
        }

        if (await sucursales.ExistePrefijoAsync(datos.Prefijo, sucursal.Id, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese prefijo");
        }

        if (whatsApp is not null && await sucursales.ExisteWhatsAppAsync(whatsApp, sucursal.Id, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese WhatsApp");
        }

        if (datos.PhoneNumberId is not null
            && await sucursales.ExistePhoneNumberIdAsync(datos.PhoneNumberId, sucursal.Id, cancellationToken))
        {
            throw new ConflictoException("Ya existe una sucursal con ese phone_number_id");
        }

        sucursal.Editar(datos);
        await sucursales.GuardarCambiosAsync(cancellationToken);
        return SucursalDto.Desde(sucursal);
    }
}
