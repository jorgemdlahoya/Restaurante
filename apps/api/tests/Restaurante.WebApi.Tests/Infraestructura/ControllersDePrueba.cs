using Microsoft.AspNetCore.Mvc;
using Restaurante.Domain.Usuarios;
using Restaurante.WebApi.Filtros;

namespace Restaurante.WebApi.Tests.Infraestructura;

/// <summary>Controller que sólo existe en el ensamblado de pruebas; se añade vía ApplicationParts.</summary>
[ApiController]
[Route("prueba")]
public sealed class ControllerDePrueba : ControllerBase
{
    public const string MensajeInterno = "detalle-interno-secreto-xyz";

    [HttpGet("caja")]
    [RequiereRol(Rol.Caja)]
    public IActionResult SoloCaja() => Ok(new { ok = true });

    [HttpGet("anonimo")]
    public IActionResult Anonimo() => Ok(new { ok = true });

    [HttpGet("explota")]
    public IActionResult Explota() => throw new InvalidOperationException(MensajeInterno);
}
