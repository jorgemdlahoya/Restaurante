namespace Restaurante.WebApi.Filtros;

/// <summary>
/// Marca explícitamente un endpoint como anónimo (p. ej. login, health). Constitución Art. 9: todo
/// endpoint exige rol salvo los que optan por esta marca; una prueba de guarda lo verifica.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class EndpointAnonimoAttribute : Attribute;
