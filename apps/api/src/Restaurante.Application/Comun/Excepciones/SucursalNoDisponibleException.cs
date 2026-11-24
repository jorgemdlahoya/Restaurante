namespace Restaurante.Application.Comun.Excepciones;

/// <summary>La sucursal de la ruta no existe o está inactiva para el estado público (RF-14); se traduce a 404.</summary>
public sealed class SucursalNoDisponibleException() : Exception("Sucursal no disponible");
