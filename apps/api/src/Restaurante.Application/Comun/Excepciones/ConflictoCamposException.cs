namespace Restaurante.Application.Comun.Excepciones;

/// <summary>Conflicto de unicidad con los campos afectados: clave del campo → mensajes.</summary>
public sealed class ConflictoCamposException(IReadOnlyDictionary<string, string[]> errores)
    : Exception(string.Join(". ", errores.Values.SelectMany(m => m)))
{
    public IReadOnlyDictionary<string, string[]> Errores { get; } = errores;
}
