namespace Restaurante.Domain.Sucursales;

/// <summary>Día de la semana con lunes = 0 (es el valor guardado en <c>dia_semana</c>).</summary>
public enum DiaSemana
{
    Lunes = 0,
    Martes,
    Miercoles,
    Jueves,
    Viernes,
    Sabado,
    Domingo,
}

public static class DiasSemana
{
    private static readonly string[] Textos =
        ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    public static string ComoTexto(this DiaSemana d) => Textos[(int)d];

    public static bool TryDesdeTexto(string? texto, out DiaSemana d)
    {
        var indice = texto is null ? -1 : Array.IndexOf(Textos, texto);
        d = indice >= 0 ? (DiaSemana)indice : default;
        return indice >= 0;
    }

    public static DiaSemana Desde(DayOfWeek d) =>
        d == DayOfWeek.Sunday ? DiaSemana.Domingo : (DiaSemana)((int)d - 1);
}
