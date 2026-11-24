namespace Restaurante.Application.Comun.Excepciones;

/// <summary>
/// Credenciales válidas de un usuario activo cuyas sucursales asignadas están todas desactivadas (403 "Sin
/// sucursales activas"; 004/RF-17). No se emite token.
/// </summary>
public sealed class SinSucursalesActivasException(string mensaje) : Exception(mensaje);
