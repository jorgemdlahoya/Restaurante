using System.Reflection;
using Microsoft.AspNetCore.Mvc;
using Restaurante.WebApi.Filtros;
using Restaurante.WebApi.Tests.Infraestructura;

namespace Restaurante.WebApi.Tests.Seguridad;

/// <summary>Constitución Art. 9: la autorización es explícita en cada endpoint.</summary>
public class GuardaEndpointsTests
{
    private static IReadOnlyList<string> EndpointsSinAutorizacionExplicita(Assembly ensamblado) =>
        ensamblado.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsSubclassOf(typeof(ControllerBase)))
            .SelectMany(t => t
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Where(m => !m.IsSpecialName && m.GetCustomAttribute<NonActionAttribute>() is null)
                .Where(m => !TieneDecision(t) && !TieneDecision(m))
                .Select(m => $"{t.FullName}.{m.Name}"))
            .ToList();

    private static bool TieneDecision(MemberInfo miembro) =>
        miembro.IsDefined(typeof(RequiereRolAttribute), inherit: true)
        || miembro.IsDefined(typeof(EndpointAnonimoAttribute), inherit: true);

    [Fact(DisplayName = "001/RF-05: todo endpoint de la API exige rol salvo los anónimos permitidos")]
    public void Todo_endpoint_exige_rol_salvo_los_anonimos_permitidos()
    {
        var infractores = EndpointsSinAutorizacionExplicita(typeof(Program).Assembly);

        Assert.True(
            infractores.Count == 0,
            "Endpoints sin [RequiereRol] ni [EndpointAnonimo]: " + string.Join(", ", infractores));
    }

    [Fact(DisplayName = "001/RF-05: la guarda detecta un endpoint sin rol ni marca anónima")]
    public void La_guarda_detecta_un_endpoint_sin_rol_ni_marca_anonima()
    {
        var infractores = EndpointsSinAutorizacionExplicita(typeof(FabricaApi).Assembly);

        Assert.Equal([$"{typeof(ControllerSinProteccion).FullName}.Abierto"], infractores);
    }
}
