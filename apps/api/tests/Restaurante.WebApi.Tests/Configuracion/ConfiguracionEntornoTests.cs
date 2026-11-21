using Restaurante.WebApi.Configuracion;

namespace Restaurante.WebApi.Tests.Configuracion;

public class ConfiguracionEntornoTests
{
    private const string ClaveValida = "0123456789abcdef0123456789abcdef";

    private static Dictionary<string, string?> EntornoValido() => new()
    {
        ["DB_CONNECTION_STRING"] = "Host=db;Database=restaurante;Password=secreto-db",
        ["JWT_SIGNING_KEY"] = ClaveValida,
        ["CORS_ALLOWED_ORIGINS"] = "http://localhost:5173",
    };

    [Theory(DisplayName = "001/RF-22: falta una variable obligatoria")]
    [InlineData("DB_CONNECTION_STRING")]
    [InlineData("JWT_SIGNING_KEY")]
    [InlineData("CORS_ALLOWED_ORIGINS")]
    public void Falta_una_variable_obligatoria(string variable)
    {
        var entorno = EntornoValido();
        entorno.Remove(variable);

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains(variable));
    }

    [Fact(DisplayName = "001/RF-22: clave JWT corta")]
    public void Clave_jwt_corta()
    {
        var entorno = EntornoValido();
        entorno["JWT_SIGNING_KEY"] = new string('k', 31);

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("JWT_SIGNING_KEY") && e.Contains("32"));
    }

    [Fact(DisplayName = "001/RF-22: los errores no muestran valores")]
    public void Los_errores_no_muestran_valores()
    {
        var entorno = EntornoValido();
        entorno["JWT_SIGNING_KEY"] = "clave-corta-secreta";
        entorno.Remove("CORS_ALLOWED_ORIGINS");

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.False(resultado.EsValida);
        Assert.DoesNotContain(resultado.Errores, e => e.Contains("clave-corta-secreta"));
        Assert.DoesNotContain(resultado.Errores, e => e.Contains("secreto-db"));
    }

    [Theory(DisplayName = "001/RF-22: orígenes CORS inválidos se rechazan sin mostrar el valor")]
    [InlineData("*")]
    [InlineData("http://a,*")]
    [InlineData(",")]
    [InlineData(" , ")]
    [InlineData("localhost:5173")]
    [InlineData("ftp://a.com")]
    [InlineData("not a url")]
    [InlineData("http://a.com/ruta")]
    [InlineData("http://a.com?x=1")]
    [InlineData("http://a.com/")]
    public void Origenes_cors_invalidos(string valor)
    {
        var entorno = EntornoValido();
        entorno["CORS_ALLOWED_ORIGINS"] = valor;

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.False(resultado.EsValida);
        Assert.Contains(resultado.Errores, e => e.Contains("CORS_ALLOWED_ORIGINS"));
        Assert.DoesNotContain(resultado.Errores, e => e.Contains(valor.Trim(' ', ',')) && valor.Trim(' ', ',').Length > 1);
    }

    [Fact(DisplayName = "001/RF-22: lista válida de orígenes https con puerto se acepta")]
    public void Origenes_cors_validos_con_puerto_y_https()
    {
        var entorno = EntornoValido();
        entorno["CORS_ALLOWED_ORIGINS"] = "https://app.example.com, http://localhost:5173";

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.True(resultado.EsValida);
        Assert.Equal(["https://app.example.com", "http://localhost:5173"], resultado.OrigenesCors);
    }

    [Fact(DisplayName = "001/RF-22: orígenes CORS separados por comas")]
    public void Origenes_cors_separados_por_comas()
    {
        var entorno = EntornoValido();
        entorno["CORS_ALLOWED_ORIGINS"] = "http://a, http://b";

        var resultado = ConfiguracionEntorno.Cargar(entorno);

        Assert.True(resultado.EsValida);
        Assert.Equal(["http://a", "http://b"], resultado.OrigenesCors);
    }
}
