using Restaurante.Domain.Usuarios;

namespace Restaurante.Application.Abstracciones;

public sealed record TokenEmitido(string Token, DateTimeOffset ExpiraEn);

public interface IEmisorTokens
{
    TokenEmitido Emitir(Usuario usuario);
}
