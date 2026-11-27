namespace Restaurante.Domain.Comun;

/// <summary>Entidad con borrado lógico: conserva los datos y marca la fecha de eliminación.</summary>
public interface IEliminable
{
    DateTimeOffset? EliminadaEn { get; }
}
