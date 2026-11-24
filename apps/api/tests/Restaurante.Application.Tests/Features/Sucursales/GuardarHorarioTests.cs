using FluentValidation;
using Restaurante.Application.Features.Sucursales.Horario;
using Restaurante.Application.Tests.Dobles;
using Restaurante.Domain.Sucursales;

namespace Restaurante.Application.Tests.Features.Sucursales;

public class GuardarHorarioTests
{
    private static readonly Guid RestauranteId = Guid.NewGuid();
    private static readonly Guid SucursalId = Guid.NewGuid();
    private readonly SucursalRepositoryEnMemoria _repositorio = new(new ContextoRestauranteFalso(RestauranteId));
    private readonly GuardarHorarioValidator _validador = new();

    public GuardarHorarioTests() =>
        _repositorio.Horarios.AddRange(HorarioDia.SemanaCerrada(RestauranteId, SucursalId));

    private static string[] Dias => ["lunes", "martes", "miércoles", "jueves", "viernes", "sábado", "domingo"];

    private static List<HorarioDiaDto> Semana(Func<string, HorarioDiaDto> porDia) => Dias.Select(porDia).ToList();

    private static List<HorarioDiaDto> SemanaAbierta() =>
        Semana(d => new HorarioDiaDto(d, false, "09:00", "22:00"));

    [Fact(DisplayName = "003/RF-08: guardar reemplaza los siete días y consultar los devuelve en orden")]
    public async Task GuardaYConsultaEnOrden()
    {
        var entrada = Semana(d => d == "domingo"
            ? new HorarioDiaDto(d, true, null, null)
            : new HorarioDiaDto(d, false, "09:00", "22:00"));
        entrada.Reverse(); // el orden de entrada no importa

        var guardado = await new GuardarHorarioHandler(_repositorio)
            .Handle(new GuardarHorarioCommand(entrada), CancellationToken.None);
        var consultado = await new ObtenerHorarioHandler(_repositorio)
            .Handle(new ObtenerHorarioQuery(), CancellationToken.None);

        Assert.Equal(Dias, consultado.Select(d => d.Dia));
        Assert.Equal(guardado, consultado);
        Assert.All(consultado.Take(6), d => Assert.Equal(new HorarioDiaDto(d.Dia, false, "09:00", "22:00"), d));
        Assert.Equal(new HorarioDiaDto("domingo", true, null, null), consultado[6]);
        Assert.Equal(1, _repositorio.GuardadosLlamados);
    }

    [Fact(DisplayName = "003/RF-08: un día cerrado ignora las horas enviadas")]
    public async Task CerradoIgnoraHoras()
    {
        var entrada = Semana(d => d == "lunes"
            ? new HorarioDiaDto(d, true, "99:99", "10:00")
            : new HorarioDiaDto(d, false, "09:00", "22:00"));

        Assert.True((await _validador.ValidateAsync(new GuardarHorarioCommand(entrada))).IsValid);
        var guardado = await new GuardarHorarioHandler(_repositorio)
            .Handle(new GuardarHorarioCommand(entrada), CancellationToken.None);

        Assert.Equal(new HorarioDiaDto("lunes", true, null, null), guardado[0]);
    }

    [Fact(DisplayName = "003/RF-09: un cierre anterior a la apertura se acepta como turno nocturno")]
    public async Task TurnoNocturno()
    {
        var entrada = Semana(d => new HorarioDiaDto(d, false, "18:00", "02:00"));

        Assert.True((await _validador.ValidateAsync(new GuardarHorarioCommand(entrada))).IsValid);
        var guardado = await new GuardarHorarioHandler(_repositorio)
            .Handle(new GuardarHorarioCommand(entrada), CancellationToken.None);

        Assert.All(guardado, d => Assert.Equal(new HorarioDiaDto(d.Dia, false, "18:00", "02:00"), d));
    }

    public static TheoryData<string, List<HorarioDiaDto>?, string, string> Invalidos()
    {
        var datos = new TheoryData<string, List<HorarioDiaDto>?, string, string>();
        const string siete = "El horario debe incluir los siete días";
        const string formato = "La hora debe tener el formato HH:mm";
        const string falta = "Un día abierto necesita hora de apertura y de cierre";
        const string iguales = "La hora de apertura y la de cierre no pueden ser iguales";

        datos.Add("nulo", null, "Dias", siete);
        datos.Add("seis días", SemanaAbierta().Take(6).ToList(), "Dias", siete);
        datos.Add("ocho días", [.. SemanaAbierta(), new HorarioDiaDto("lunes", true, null, null)], "Dias", siete);
        var repetido = SemanaAbierta();
        repetido[1] = repetido[0];
        datos.Add("día repetido", repetido, "Dias", siete);
        var desconocido = SemanaAbierta();
        desconocido[2] = new HorarioDiaDto("funday", false, "09:00", "22:00");
        datos.Add("día desconocido", desconocido, "Dias", siete);
        datos.Add("hora mal escrita", Con(3, new HorarioDiaDto("jueves", false, "9:00", "22:00")), "Dias[3].Apertura", formato);
        datos.Add("hora fuera de rango", Con(4, new HorarioDiaDto("viernes", false, "09:00", "24:00")), "Dias[4].Cierre", formato);
        datos.Add("sin cierre", Con(5, new HorarioDiaDto("sábado", false, "09:00", null)), "Dias[5].Cierre", falta);
        datos.Add("sin apertura", Con(6, new HorarioDiaDto("domingo", false, null, "22:00")), "Dias[6].Apertura", falta);
        datos.Add("horas iguales", Con(0, new HorarioDiaDto("lunes", false, "10:00", "10:00")), "Dias[0].Cierre", iguales);
        return datos;
    }

    private static List<HorarioDiaDto> Con(int indice, HorarioDiaDto dia)
    {
        var semana = SemanaAbierta();
        semana[indice] = dia;
        return semana;
    }

    [Theory(DisplayName = "003/RF-09: un horario con días faltantes, horas iguales o mal escritas es inválido")]
    [MemberData(nameof(Invalidos))]
    public async Task Invalido(string caso, List<HorarioDiaDto>? dias, string clave, string mensaje)
    {
        _ = caso;
        var resultado = await _validador.ValidateAsync(new GuardarHorarioCommand(dias!));

        Assert.Contains(resultado.Errors, e => e.PropertyName == clave && e.ErrorMessage == mensaje);
    }
}
