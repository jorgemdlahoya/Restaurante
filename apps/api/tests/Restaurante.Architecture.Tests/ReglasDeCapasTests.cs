using System.Reflection;
using NetArchTest.Rules;

namespace Restaurante.Architecture.Tests;

public class ReglasDeCapasTests
{
    private static readonly Assembly Domain = Assembly.Load("Restaurante.Domain");
    private static readonly Assembly Application = Assembly.Load("Restaurante.Application");

    private static void NoDepende(Assembly capa, params string[] prohibidos)
    {
        var resultado = Types.InAssembly(capa)
            .ShouldNot()
            .HaveDependencyOnAny(prohibidos)
            .GetResult();

        Assert.True(
            resultado.IsSuccessful,
            "Dependencias prohibidas en: " + string.Join(", ", resultado.FailingTypeNames ?? []));
    }

    [Theory(DisplayName = "001/RNF-02: Domain no depende de capas externas ni de frameworks")]
    [InlineData("Restaurante.Application")]
    [InlineData("Restaurante.Infrastructure")]
    [InlineData("Restaurante.WebApi")]
    [InlineData("MediatR")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    public void Domain_no_depende_de(string prohibido) => NoDepende(Domain, prohibido);

    [Theory(DisplayName = "001/RNF-02: Application no depende de Infrastructure ni WebApi")]
    [InlineData("Restaurante.Infrastructure")]
    [InlineData("Restaurante.WebApi")]
    public void Application_no_depende_de(string prohibido) => NoDepende(Application, prohibido);
}
