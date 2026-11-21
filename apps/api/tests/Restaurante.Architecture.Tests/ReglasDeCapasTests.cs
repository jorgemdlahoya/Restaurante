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

    /// <summary>Referencias del ensamblado que no son del runtime (<c>System.*</c>, netstandard, mscorlib).</summary>
    private static string[] ReferenciasNoDeRuntime(Assembly capa) =>
        capa.GetReferencedAssemblies()
            .Select(r => r.Name ?? string.Empty)
            .Where(n => n != "System" && !n.StartsWith("System.", StringComparison.Ordinal)
                        && n != "netstandard" && n != "mscorlib")
            .Order(StringComparer.Ordinal)
            .ToArray();

    [Fact(DisplayName = "001/RNF-02: Domain sólo referencia ensamblados del runtime (ningún paquete ni capa)")]
    public void Domain_solo_referencia_el_runtime()
    {
        var ajenas = ReferenciasNoDeRuntime(Domain);

        Assert.True(ajenas.Length == 0, "Domain referencia ensamblados ajenos al runtime: " + string.Join(", ", ajenas));
    }

    [Fact(DisplayName = "001/RNF-02: la comprobación de referencias detecta una referencia que no es del runtime")]
    public void La_comprobacion_de_referencias_detecta_paquetes_externos()
    {
        var ajenas = ReferenciasNoDeRuntime(Application);

        Assert.Contains("MediatR", ajenas);
        Assert.Contains("Restaurante.Domain", ajenas);
    }

    [Theory(DisplayName = "001/RNF-02: Application no depende de Infrastructure ni WebApi")]
    [InlineData("Restaurante.Infrastructure")]
    [InlineData("Restaurante.WebApi")]
    public void Application_no_depende_de(string prohibido) => NoDepende(Application, prohibido);
}
