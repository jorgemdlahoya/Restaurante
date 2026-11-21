using System.IdentityModel.Tokens.Jwt;
using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;
using Restaurante.Infrastructure.Seguridad;

namespace Restaurante.Infrastructure.Tests.Seguridad;

public sealed class EmisorTokensJwtTests
{
    private const string Clave = "clave-de-pruebas-con-al-menos-32-caracteres!!";

    private sealed class RelojFijo(DateTimeOffset ahora) : IReloj
    {
        public DateTimeOffset Ahora { get; } = ahora;
    }

    [Fact(DisplayName = "001/RF-01: el token lleva sub, unique_name, role y expira a las 8 h")]
    public void Token_lleva_claims_y_expira_a_las_8_horas()
    {
        var ahora = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var emisor = new EmisorTokensJwt(Clave, new RelojFijo(ahora));
        var usuario = Usuario.Crear("Ana", "hash", Rol.Caja, ahora);

        var emitido = emisor.Emitir(usuario);

        var jwt = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(emitido.Token);
        Assert.Equal(usuario.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("Ana", jwt.Claims.Single(c => c.Type == "unique_name").Value);
        Assert.Equal("caja", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("restaurante-api", jwt.Issuer);
        Assert.Contains("restaurante-web", jwt.Audiences);
        Assert.Equal("HS256", jwt.Header.Alg);
        Assert.Equal(ahora.AddHours(8), emitido.ExpiraEn);
        Assert.Equal(ahora.AddHours(8).UtcDateTime, jwt.ValidTo);
    }
}
