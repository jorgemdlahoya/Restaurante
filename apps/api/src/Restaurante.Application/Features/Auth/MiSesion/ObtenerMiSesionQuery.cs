using MediatR;

namespace Restaurante.Application.Features.Auth.MiSesion;

public sealed record ObtenerMiSesionQuery() : IRequest<MiSesionDto>;
