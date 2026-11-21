namespace Restaurante.Application.Comun.Excepciones;

public sealed class ConflictoException(string mensaje) : Exception(mensaje);
