using MediatR;

namespace Restaurante.Application.Features.Restaurantes.ObtenerLogo;

/// <summary>Logo del restaurante fijado por el slug de la ruta.</summary>
public sealed record ObtenerLogoQuery : IRequest<LogoDto>;
