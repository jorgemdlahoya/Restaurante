namespace Restaurante.Application.Comun.Excepciones;

public sealed class NoEncontradoException(string mensaje) : Exception(mensaje);
