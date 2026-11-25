using Microsoft.AspNetCore.Mvc;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Application.Comun.Excepciones;
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

    public const string MensajeProhibido = "No puedes gestionar a este usuario.";

    [HttpGet("prohibido")]
    [EndpointAnonimo]
    public IActionResult Prohibido() => throw new ProhibidoException(MensajeProhibido);
}

/// <summary>Devuelve el contexto de sucursal que fijó el middleware (003/RF-14, RF-15).</summary>
[ApiController]
public sealed class ControllerEcoContextoSucursal : ControllerBase
{
    [HttpGet("api/r/{slug}/s/{sucursal}/eco-contexto")]
    [EndpointAnonimo]
    public IActionResult EnSucursal([FromServices] IContextoSucursal contexto) =>
        Ok(new { sucursalId = contexto.SucursalId });

    [HttpGet("api/r/{slug}/eco-contexto")]
    [EndpointAnonimo]
    public IActionResult FueraDeSucursal([FromServices] IContextoSucursal contexto) =>
        Ok(new { sucursalId = contexto.SucursalId });
}

/// <summary>Devuelve el usuario que fijó <c>MiddlewareSesionUsuario</c> en <see cref="IUsuarioActual"/> (004/RF-13).</summary>
[ApiController]
[Route("prueba/sesion")]
public sealed class ControllerEcoSesion : ControllerBase
{
    [HttpGet]
    [RequiereRol(Rol.AdministradorPlataforma, Rol.AdministradorGeneral, Rol.AdministradorSucursal, Rol.Cajero,
        Rol.Cocinero)]
    public IActionResult Eco([FromServices] IUsuarioActual usuario) =>
        Ok(new { id = usuario.Id, rol = usuario.Rol.ComoTexto(), sucursales = usuario.Sucursales });
}

/// <summary>Endpoint de sucursal abierto a todos los roles, para observar el acceso por sucursal (004/RF-15, RF-25).</summary>
[ApiController]
public sealed class ControllerAccesoSucursal : ControllerBase
{
    [HttpGet("api/r/{slug}/s/{sucursal}/eco-acceso")]
    [RequiereRol(Rol.AdministradorGeneral, Rol.AdministradorSucursal, Rol.Cajero, Rol.Cocinero)]
    public IActionResult Acceso([FromServices] IContextoSucursal contexto) =>
        Ok(new { sucursalId = contexto.SucursalId });
}

/// <summary>Incumple a propósito el Art. 9 (sin rol ni marca anónima): sirve para probar la prueba de guarda.</summary>
[ApiController]
[Route("prueba-sin-proteccion")]
public sealed class ControllerSinProteccion : ControllerBase
{
    [HttpGet]
    public IActionResult Abierto() => Ok();
}
