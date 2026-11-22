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
    [RequiereRol(Rol.Cajero)]
    public IActionResult SoloCaja() => Ok(new { ok = true });

    [HttpGet("anonimo")]
    [EndpointAnonimo]
    public IActionResult Anonimo() => Ok(new { ok = true });

    [HttpGet("explota")]
    [EndpointAnonimo]
    public IActionResult Explota() => throw new InvalidOperationException(MensajeInterno);
}

/// <summary>Incumple a propósito el Art. 9 (sin rol ni marca anónima): sirve para probar la prueba de guarda.</summary>
[ApiController]
[Route("prueba-sin-proteccion")]
public sealed class ControllerSinProteccion : ControllerBase
{
    [HttpGet]
    public IActionResult Abierto() => Ok();
}
