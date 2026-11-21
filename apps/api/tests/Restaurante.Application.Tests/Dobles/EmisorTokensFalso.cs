using Restaurante.Application.Abstracciones;
using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Tests.Dobles;

public sealed class EmisorTokensFalso : IEmisorTokens
{
    public static readonly DateTimeOffset Expiracion = new(2030, 1, 1, 8, 0, 0, TimeSpan.Zero);

    public TokenEmitido Emitir(Usuario usuario) => new("token-de-" + usuario.NombreUsuario, Expiracion);
}
