using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Restaurante.Application.Abstracciones;
using Restaurante.Application.Comun;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Infrastructure.Seguridad;

public sealed class EmisorTokensJwt(string claveJwt, IReloj reloj) : IEmisorTokens
{
    public const string Emisor = "restaurante-api";
    public const string Audiencia = "restaurante-web";
    public static readonly TimeSpan Duracion = TimeSpan.FromHours(8);

    private readonly SigningCredentials _credenciales =
        new(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)), SecurityAlgorithms.HmacSha256);

    public TokenEmitido Emitir(Usuario usuario)
    {
        var ahora = reloj.Ahora;
        var expira = ahora.Add(Duracion);
        var token = new JwtSecurityToken(
            issuer: Emisor,
            audience: Audiencia,
            claims:
            [
                new Claim("sub", usuario.Id.ToString()),
                new Claim("unique_name", usuario.NombreUsuario),
                new Claim("role", usuario.Rol.ComoTexto()),
            ],
            notBefore: ahora.UtcDateTime,
            expires: expira.UtcDateTime,
            signingCredentials: _credenciales);

        return new TokenEmitido(new JwtSecurityTokenHandler().WriteToken(token), expira);
    }
}
