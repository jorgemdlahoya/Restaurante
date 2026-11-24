namespace Restaurante.Application.Comun.Excepciones;

/// <summary>El usuario autenticado no tiene alcance para la operación (403 "Acceso denegado").</summary>
public sealed class ProhibidoException(string mensaje) : Exception(mensaje);
