using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Restaurante.Application.Abstracciones;
using Restaurante.Infrastructure.Persistencia;
using Restaurante.Infrastructure.Seguridad;
using Restaurante.WebApi.Filtros;
using Restaurante.WebApi.Seguridad;

namespace Restaurante.WebApi;

public static class DependencyInjection
{
    public const string PoliticaCors = "OrigenesPermitidos";
    public const string NombreSaludBase = "base-de-datos";

    public static IServiceCollection AddWebApiServices(
        this IServiceCollection services, string claveJwt, IReadOnlyList<string> origenesCors)
    {
        // Art. 1.4: sólo los orígenes de CORS_ALLOWED_ORIGINS; nunca AllowAnyOrigin.
        services.AddCors(o => o.AddPolicy(PoliticaCors, p => p
            .WithOrigins(origenesCors.ToArray())
            .AllowAnyHeader()
            .AllowAnyMethod()));

        services.AddHealthChecks().AddDbContextCheck<RestauranteDbContext>(NombreSaludBase);

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

    /// <summary>Swagger con el esquema Bearer. Sólo se invoca en el entorno Development (RF-25).</summary>
    public static IServiceCollection AddSwaggerConBearer(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(o =>
        {
            o.SwaggerDoc("v1", new OpenApiInfo { Title = "Restaurante API", Version = "v1" });
            o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Token JWT obtenido en POST /api/auth/login.",
            });
            o.AddSecurityRequirement(documento => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", documento)] = [],
            });
        });

        return services;
    }
}
