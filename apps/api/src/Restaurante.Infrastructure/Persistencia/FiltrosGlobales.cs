using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Restaurante.Domain.Comun;

namespace Restaurante.Infrastructure.Persistencia;

/// <summary>
/// Filtros globales por convención (003/RF-15, RF-16): toda entidad <see cref="IEntidadDeRestaurante"/>
/// se filtra por el restaurante del contexto y toda <see cref="IEntidadDeSucursal"/> además por su sucursal;
/// toda <see cref="IEliminable"/> oculta además las filas con borrado lógico (005/RF-07).
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
            var deRestaurante = typeof(IEntidadDeRestaurante).IsAssignableFrom(clr);
            var eliminable = typeof(IEliminable).IsAssignableFrom(clr);
            if (!deRestaurante && !eliminable)
            {
                continue;
            }

            // Cada tipo recibe un único HasQueryFilter: las condiciones se combinan con AND.
            var entidad = Expression.Parameter(clr, "e");
            Expression? cuerpo = null;
            if (deRestaurante)
            {
                cuerpo = Igual(entidad, nameof(IEntidadDeRestaurante.RestauranteId), instancia, RestauranteActual);
                if (typeof(IEntidadDeSucursal).IsAssignableFrom(clr))
                {
                    cuerpo = Expression.AndAlso(
                        cuerpo, Igual(entidad, nameof(IEntidadDeSucursal.SucursalId), instancia, SucursalActual));
                }
            }

            if (eliminable)
            {
                // Borrado lógico (005/RF-07): e.EliminadaEn == null.
                var noEliminada = Expression.Equal(
                    Expression.Property(entidad, nameof(IEliminable.EliminadaEn)),
                    Expression.Constant(null, typeof(DateTimeOffset?)));
                cuerpo = cuerpo is null ? noEliminada : Expression.AndAlso(cuerpo, noEliminada);
            }

            modelBuilder.Entity(clr).HasQueryFilter(Expression.Lambda(cuerpo!, entidad));
        }
    }

    // e.Propiedad (Guid, convertido a Guid?) == contexto.PropiedadActual (Guid?)
    private static BinaryExpression Igual(
        ParameterExpression entidad, string propiedad, ConstantExpression instancia, PropertyInfo actual) =>
        Expression.Equal(
            Expression.Convert(Expression.Property(entidad, propiedad), typeof(Guid?)),
            Expression.Property(instancia, actual));
}
