# Mapa de arquitectura

Solución .NET en `apps/api/` (`Restaurante.sln`), proyectos `net10.0`.

## Capas

| Capa | Proyecto | Responsabilidad |
|------|----------|-----------------|
| Domain | `src/Restaurante.Domain` | Entidades y reglas de negocio. Sin dependencias. |
| Application | `src/Restaurante.Application` | Casos de uso (commands, handlers, validators) y puertos. |
| Infrastructure | `src/Restaurante.Infrastructure` | Persistencia y servicios externos. |
| WebApi | `src/Restaurante.WebApi` | Controllers, configuración por entorno y `Program.cs`. |

## Dependencias permitidas

```
WebApi → Application, Infrastructure
Infrastructure → Application
Application → Domain
Domain → (nada)
```

Reglas verificadas por `tests/Restaurante.Architecture.Tests` (001/RNF-02):
`Domain` no depende de `Application`, `Infrastructure`, `WebApi`, `MediatR` ni
`Microsoft.EntityFrameworkCore`; `Application` no depende de `Infrastructure` ni `WebApi`.

## Pruebas

Un proyecto de pruebas por capa (`Domain`, `Application`, `Infrastructure`, `WebApi`) más
`Architecture`. Las pruebas con Testcontainers llevan `[Trait("Category", "Integracion")]`.
