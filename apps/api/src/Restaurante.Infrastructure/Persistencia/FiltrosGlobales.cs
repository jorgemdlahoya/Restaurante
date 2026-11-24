using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Comun;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>
/// Filtros globales por convención (003/RF-15, RF-16): toda entidad <see cref="IEntidadDeRestaurante"/>
/// se filtra por el restaurante del contexto y toda <see cref="IEntidadDeSucursal"/> además por su sucursal.
/// </summary>
/// <remarks>
/// La expresión referencia las propiedades de instancia del <see cref="RestauranteDbContext"/> mediante
/// <c>Expression.Constant(contexto)</c>, igual que lo haría una lambda de C# escrita en
/// <c>OnModelCreating</c>: EF Core reconoce esa constante y la sustituye en cada consulta por el contexto
/// que la ejecuta, así el modelo cacheado sirve para todos los restaurantes y sucursales.
/// Con contexto nulo la comparación <c>Guid? == Guid</c> es falsa: no se ve ninguna fila.
/// </remarks>
internal static class FiltrosGlobales
{
    private const BindingFlags Instancia = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

    private static readonly PropertyInfo RestauranteActual =
        typeof(RestauranteDbContext).GetProperty("RestauranteActualId", Instancia)
        ?? throw new InvalidOperationException("RestauranteDbContext debe exponer RestauranteActualId.");

    private static readonly PropertyInfo SucursalActual =
        typeof(RestauranteDbContext).GetProperty("SucursalActualId", Instancia)
        ?? throw new InvalidOperationException("RestauranteDbContext debe exponer SucursalActualId.");

    public static void Aplicar(ModelBuilder modelBuilder, RestauranteDbContext contexto)
    {
        var instancia = Expression.Constant(contexto);
        foreach (var tipo in modelBuilder.Model.GetEntityTypes().Where(t => !t.IsOwned()).ToList())
        {
            var clr = tipo.ClrType;
            if (!typeof(IEntidadDeRestaurante).IsAssignableFrom(clr))
            {
                continue;
            }

            var entidad = Expression.Parameter(clr, "e");
            Expression cuerpo = Igual(entidad, nameof(IEntidadDeRestaurante.RestauranteId), instancia, RestauranteActual);
            if (typeof(IEntidadDeSucursal).IsAssignableFrom(clr))
            {
                cuerpo = Expression.AndAlso(
                    cuerpo, Igual(entidad, nameof(IEntidadDeSucursal.SucursalId), instancia, SucursalActual));
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(cuerpo, entidad));
        }
    }

    // e.Propiedad (Guid, convertido a Guid?) == contexto.PropiedadActual (Guid?)
    private static BinaryExpression Igual(
        ParameterExpression entidad, string propiedad, ConstantExpression instancia, PropertyInfo actual) =>
        Expression.Equal(
            Expression.Convert(Expression.Property(entidad, propiedad), typeof(Guid?)),
            Expression.Property(instancia, actual));
}
