namespace Restaurante.Application.Comun.Excepciones;

public sealed class CredencialesInvalidasException(string mensaje) : Exception(mensaje);
