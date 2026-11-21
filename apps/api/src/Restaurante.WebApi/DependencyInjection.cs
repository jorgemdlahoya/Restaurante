using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Seguridad;
using Restaurante.WebApi.Filtros;
using Restaurante.WebApi.Seguridad;

namespace Restaurante.WebApi;

public static class DependencyInjection
{
    public static IServiceCollection AddWebApiServices(this IServiceCollection services, string claveJwt)
    {
        services.AddControllers(o => o.Filters.Add<FiltroExcepciones>())
            .ConfigureApiBehaviorOptions(o => o.InvalidModelStateResponseFactory = contexto =>
                RespuestasProblema.Validacion(contexto.ModelState));
        services.AddHttpContextAccessor();
        services.AddScoped<IUsuarioActual, UsuarioActual>();

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o =>
            {
                o.MapInboundClaims = false;
                o.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(claveJwt)),
                    ValidateIssuer = true,
                    ValidIssuer = EmisorTokensJwt.Emisor,
                    ValidateAudience = true,
                    ValidAudience = EmisorTokensJwt.Audiencia,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero,
                    RoleClaimType = "role",
                    NameClaimType = "unique_name",
                };
            });

        return services;
    }
}
