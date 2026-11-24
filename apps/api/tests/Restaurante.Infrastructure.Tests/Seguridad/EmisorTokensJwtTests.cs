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
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana", "hash", Rol.Cajero, [Guid.NewGuid()], ahora);

        var emitido = emisor.Emitir(usuario);

        var jwt = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(emitido.Token);
        Assert.Equal(usuario.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal("Ana", jwt.Claims.Single(c => c.Type == "unique_name").Value);
        Assert.Equal("cajero", jwt.Claims.Single(c => c.Type == "role").Value);
        Assert.Equal("restaurante-api", jwt.Issuer);
        Assert.Contains("restaurante-web", jwt.Audiences);
        Assert.Equal("HS256", jwt.Header.Alg);
        Assert.Equal(ahora.AddHours(8), emitido.ExpiraEn);
        Assert.Equal(ahora.AddHours(8).UtcDateTime, jwt.ValidTo);
    }

    [Fact(DisplayName = "002/RF-08: el emisor incluye restaurant_id sólo si el usuario tiene restaurante")]
    public void Incluye_restaurant_id_solo_si_el_usuario_tiene_restaurante()
    {
        var ahora = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var emisor = new EmisorTokensJwt(Clave, new RelojFijo(ahora));
        var restauranteId = Guid.NewGuid();
        var deRestaurante = Usuario.Crear(restauranteId, "Ana", "hash", Rol.Cajero, [Guid.NewGuid()], ahora);
        var dePlataforma = Usuario.Crear(null, "Raiz", "hash", Rol.AdministradorPlataforma, [], ahora);
        var lector = new JwtSecurityTokenHandler { MapInboundClaims = false };

        var conRestaurante = lector.ReadJwtToken(emisor.Emitir(deRestaurante).Token);
        var sinRestaurante = lector.ReadJwtToken(emisor.Emitir(dePlataforma).Token);

        Assert.Equal(restauranteId.ToString(), conRestaurante.Claims.Single(c => c.Type == "restaurant_id").Value);
        Assert.DoesNotContain(sinRestaurante.Claims, c => c.Type == "restaurant_id");
    }

    [Fact(DisplayName = "004/RF-13: el token incluye el sello del usuario")]
    public void Token_incluye_el_sello_del_usuario()
    {
        var ahora = new DateTimeOffset(2030, 1, 1, 10, 0, 0, TimeSpan.Zero);
        var emisor = new EmisorTokensJwt(Clave, new RelojFijo(ahora));
        var usuario = Usuario.Crear(Guid.NewGuid(), "Ana", "hash", Rol.Cajero, [Guid.NewGuid()], ahora);

        var jwt = new JwtSecurityTokenHandler { MapInboundClaims = false }.ReadJwtToken(emisor.Emitir(usuario).Token);

        Assert.Equal(usuario.SelloSesion.ToString("D"), jwt.Claims.Single(c => c.Type == "sello").Value);
    }
}
