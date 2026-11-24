---
feature: 003-sucursales-alta-horarios-y-aislamiento-p
estado: borrador
---
# Tareas: sucursales, horarios y aislamiento por sucursal

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** que cada restaurante gestione sus sucursales, su horario semanal y sus cierres
temporales, con un estado público "abierta/cerrada" y aislamiento por sucursal en la base de datos.

**Arquitectura:** las operaciones de una sucursal viven bajo `/api/r/{slug}/s/{sucursal}/…`; un
middleware nuevo, encadenado tras el de restaurante de 002, fija `IContextoSucursal`. Un filtro
global de EF Core por convención aísla toda entidad marcada con `IEntidadDeRestaurante` o
`IEntidadDeSucursal`. El estado de atención lo calcula una función pura a partir de `IReloj`.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, Vitest + Testing Library, Playwright.

**Spec:** `specs/003-sucursales-alta-horarios-y-aislamiento-p/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`. El código de 002 (contexto de restaurante, middleware,
`FabricaApi`, pantallas del restaurante) es la base: seguir sus patrones.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "003/RF-NN: …")]`, `it("003/RF-NN: …")`, `test("003/RF-NN: …")`.
- Integración con PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`; se ejecuta a mano en la tarea que la crea (Docker encendido).
- `dotnet`: anteponer `PATH=/root/.dotnet:$PATH`. EF: `dotnet tool run dotnet-ef -- migrations add <Nombre> --project src/Restaurante.Infrastructure --startup-project src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones` desde `apps/api`.
- `Restaurante` (clase) choca con el namespace raíz: usar `using RestauranteEntidad = Restaurante.Domain.Restaurantes.Restaurante;` donde haga falta.
- Formatos (RF-02): nombre 1–80 y dirección 1–200 tras `Trim()`; teléfono y WhatsApp: se quitan espacios y guiones y deben cumplir `^\+52\d{10}\z` (se guardan así, RNF-02); `slug` `^[a-z0-9-]{3,40}\z`; `phone_number_id` `^\d{1,30}\z` o ausente; prefijo `^[A-Z]{1,3}\z`; zona horaria en la lista de RNF-01 (`America/Mexico_City` por defecto). Todas las regex anclan con `\z`, no con `$` (lección de 002).
- Días: `lunes`, `martes`, `miércoles`, `jueves`, `viernes`, `sábado`, `domingo` (texto en API y mensajes); en BD `dia_semana` 0–6 con lunes = 0. Horas `HH:mm` en 24 h.
- Fechas de cierres en la API: entrada y salida como hora local de la sucursal sin zona (`yyyy-MM-ddTHH:mm`); se guardan en UTC.
- Mensajes de cerrada (RF-13): `Estamos cerrados, abrimos {día} a las {hora}` con `{día}` ∈ `hoy` | `mañana` | nombre del día; sin apertura en 14 días: `Estamos cerrados por el momento`.
- Errores: datos de otra sucursal u otro restaurante → 404, nunca 403. Sucursal inexistente en la ruta o inactiva en el estado público → 404 con título `Sucursal no disponible`. Rol distinto de `administrador_general` → 403.
- Art. 5: todo endpoint nuevo actualiza su `.http` en `docs/context/http/` y los glosarios (`docs/GLOSSARY.md`, `docs/context/glossary.md`) en la misma tarea; los cambios de capas o despliegue, `docs/context/architecture-map.md` y `docs/ONBOARDING.md`.
- Frontend: sólo variables de tema (RNF-02 de 002, la prueba ya lo vigila); escritorio 1440 px y móvil 390 px (proyectos de Playwright ya configurados); el token se pasa siempre explícito a `pedirApi`, y nunca en el estado público.

## Foco de revisión

1. **Turno que cruza la medianoche consultado de madrugada** (abre domingo 18:00, cierra 02:00; consulta lunes 01:00): debe estar abierta (T-03).
2. **Cierre temporal programado para el futuro:** no debe cerrar la sucursal antes de su inicio (T-03).
3. **Teléfono escrito con espacios** (`+52 55 1234 5678`): se acepta y se guarda como `+525512345678` (T-01).
4. **Cierre creado con la sucursal en otra zona** (Tijuana) que la del navegador: `2026-12-24T09:00` se interpreta como 09:00 de Tijuana, no del navegador ni de UTC (T-09).
5. **Desactivar la sucursal elegida en el selector:** desaparece del selector y deja de ser la sucursal activa de la sesión (T-13).

---

### T-01 [RF-01, RF-05, RF-06]: Entidad Sucursal y reglas de formato

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Comun/IEntidadDeRestaurante.cs`, `IEntidadDeSucursal.cs`, `TelefonoMexico.cs`
- Crear: `apps/api/src/Restaurante.Domain/Sucursales/Sucursal.cs`, `ZonasHorarias.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Sucursales/SucursalTests.cs`, `TelefonoMexicoTests.cs`
- Docs: glosarios (Sucursal, prefijo, zona horaria, `phone_number_id`)

**Interfaces:**
- Produce: `interface IEntidadDeRestaurante { Guid RestauranteId { get; } }`; `interface IEntidadDeSucursal : IEntidadDeRestaurante { Guid SucursalId { get; } }`.
- Produce: `static class TelefonoMexico { static string? Normalizar(string? texto); }` (quita espacios y guiones; devuelve `+52XXXXXXXXXX` o `null` si no cumple).
- Produce: `static class ZonasHorarias { const string PorDefecto = "America/Mexico_City"; static IReadOnlyList<string> Admitidas; static bool EsAdmitida(string? zona); }` con las 12 zonas de RNF-01.
- Produce: `sealed record DatosSucursal(string Nombre, string Direccion, string Telefono, string WhatsApp, string? PhoneNumberId, string Prefijo, string ZonaHoraria)` (todo menos el slug).
- Produce: `Sucursal : IEntidadDeRestaurante` con `const string PatronSlug = @"^[a-z0-9-]{3,40}\z"`, `static Sucursal Crear(Guid restauranteId, string slug, DatosSucursal datos, DateTimeOffset creadaEn)`, `void Editar(DatosSucursal datos)`, `void Activar()`, `void Desactivar()`. Propiedades `Id`, `RestauranteId`, `Nombre`, `Direccion`, `Telefono`, `Slug`, `WhatsApp`, `PhoneNumberId`, `Prefijo`, `ZonaHoraria`, `Activa` (empieza en `true`), `CreadaEn`. `Crear` y `Editar` recortan y normalizan (teléfonos con `TelefonoMexico`) y lanzan `ArgumentException` si algún formato de las restricciones globales no se cumple. Constructor privado sin parámetros para EF.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-01: una sucursal nueva está activa y con datos recortados y normalizados"` → `Crear(rid, "centro", new DatosSucursal("  Centro ", "Av. Juárez 1", "+52 55 1234 5678", "55-1234-5679", null, "A", "America/Mexico_City"), t)` da `Nombre == "Centro"`, `Telefono == "+525512345678"`, `WhatsApp == "+525512345679"`, `Activa` (Foco 3; el WhatsApp de 10 dígitos sin `+52` se rechaza: ver la siguiente).
  - `"003/RF-01: Crear rechaza formatos inválidos"` (Theory) → `ArgumentException` con: slug `"Centro"`, `"ab"`, `"abc\n"`; teléfono `"5512345678"`; prefijo `"a"`, `"ABCD"`, `"A\n"`; zona `"Europe/Madrid"`; `phone_number_id` `"12a"`; nombre de 81 caracteres; dirección vacía.
  - `"003/RF-05: Editar cambia todo menos el slug"` → tras `Editar(...)`, `Slug` es el original y el resto toma los nuevos valores.
  - `"003/RF-06: desactivar y reactivar sólo cambia el estado"`.
  - `TelefonoMexicoTests` `"003/RF-01: normaliza teléfonos mexicanos"` (Theory): `"+52 55 1234 5678"` → `"+525512345678"`; `"+52-55-1234-5678"` → igual; `"5512345678"`, `"+15512345678"`, `"+52551234567"`, `null` → `null`.
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~003/"` → no compila.
- [ ] **Paso 3:** implementar en `Restaurante.Domain` (sin paquetes externos, Art. 6.2).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): entidad Sucursal y reglas de formato (T-01)"`.

### T-02 [RF-08, RF-09, RF-10, RF-11]: Horario por día y cierres temporales

**Archivos:**
- Crear: `apps/api/src/Restaurante.Domain/Sucursales/DiaSemana.cs`, `HorarioDia.cs`, `CierreTemporal.cs`
- Test: `apps/api/tests/Restaurante.Domain.Tests/Sucursales/HorarioDiaTests.cs`, `CierreTemporalTests.cs`

**Interfaces:**
- Produce: `enum DiaSemana { Lunes = 0, Martes, Miercoles, Jueves, Viernes, Sabado, Domingo }` y `static class DiasSemana { static string ComoTexto(this DiaSemana d); static bool TryDesdeTexto(string? texto, out DiaSemana d); static DiaSemana Desde(DayOfWeek d); }` (textos de las restricciones globales).
- Produce: `HorarioDia : IEntidadDeSucursal` con `RestauranteId`, `SucursalId`, `Dia`, `Cerrado`, `TimeOnly? Apertura`, `TimeOnly? Cierre`; `static HorarioDia Crear(Guid restauranteId, Guid sucursalId, DiaSemana dia, TimeOnly? apertura, TimeOnly? cierre)` (ambos nulos = cerrado; uno solo o iguales → `ArgumentException`); `void Cambiar(TimeOnly? apertura, TimeOnly? cierre)` con las mismas reglas; `bool CruzaMedianoche => Cierre < Apertura`; `static IReadOnlyList<HorarioDia> SemanaCerrada(Guid restauranteId, Guid sucursalId)` (7 días cerrados).
- Produce: `CierreTemporal : IEntidadDeSucursal` con `Id`, `RestauranteId`, `SucursalId`, `Inicio`, `Fin` (UTC), `Motivo?`, `CreadoEn`, `CanceladoEn?`; `static CierreTemporal Crear(Guid restauranteId, Guid sucursalId, DateTimeOffset inicio, DateTimeOffset fin, string? motivo, DateTimeOffset ahora)` (lanza `ArgumentException` si `fin <= inicio`, `fin <= ahora` o motivo > 120 tras `Trim()`; motivo vacío → `null`); `void Cancelar(DateTimeOffset ahora)` (si aún no empezó, `Inicio = Fin = ahora`; si no, `Fin = ahora`; siempre `CanceladoEn = ahora`); `bool AplicaEn(DateTimeOffset instante) => Inicio <= instante && instante < Fin`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-08: un día con apertura y cierre está abierto; sin horas está cerrado"`.
  - `"003/RF-09: apertura igual al cierre o una sola hora lanza ArgumentException"`.
  - `"003/RF-09: un cierre anterior a la apertura cruza la medianoche"` → `Crear(..., 18:00, 02:00).CruzaMedianoche` es `true`.
  - `"003/RF-08: la semana nueva tiene los siete días cerrados"` → 7 elementos, días `Lunes..Domingo`, todos `Cerrado`.
  - `"003/RF-08: los días se convierten a texto y desde texto"` (Theory, 7 casos, incluidos `"miércoles"` y `"sábado"`; `"Lunes"` y `"miercoles"` no son válidos).
  - `"003/RF-10: Crear rechaza fin no posterior al inicio, fin pasado y motivo largo"` (Theory).
  - `"003/RF-11: cancelar un cierre vigente adelanta su fin y deja de aplicar"` → tras `Cancelar(ahora)`, `AplicaEn(ahora)` es `false`, `CanceladoEn == ahora`.
  - `"003/RF-11: cancelar un cierre futuro lo deja sin duración"` → `Inicio == Fin == ahora`.
- [ ] **Paso 2:** `dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~003/"` → no compila.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): horario por día y cierres temporales (T-02)"`.

### T-03 [RF-12, RF-13]: Calculadora del estado de atención

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/EstadoAtencion/CalculadoraEstadoAtencion.cs`, `EstadoAtencionDto.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/CalculadoraEstadoAtencionTests.cs`

**Interfaces:**
- Consume: `HorarioDia`, `CierreTemporal`, `DiaSemana` (T-02).
- Produce: `sealed record EstadoAtencionDto(bool Abierta, string? Mensaje)`.
- Produce: `static class CalculadoraEstadoAtencion { const int DiasDeBusqueda = 14; static EstadoAtencionDto Calcular(DateTimeOffset ahora, TimeZoneInfo zona, IReadOnlyList<HorarioDia> horario, IReadOnlyList<CierreTemporal> cierres); }`.
- Algoritmo (fijado aquí porque las pruebas no lo determinan del todo): para cada fecha local desde (fecha local de `ahora` − 1 día) hasta (+14 días), si su día está abierto genera el intervalo `[fecha+Apertura, fecha(+1 si cruza medianoche)+Cierre)` en hora local y lo convierte a UTC con `TimeZoneInfo.ConvertTimeToUtc` (hora inválida por cambio de horario → se suma 1 h). A cada intervalo se le restan los cierres que se solapan (puede partirse en dos). Abierta si `ahora` cae en algún intervalo resultante. Si no, la próxima apertura es el menor inicio de intervalo `> ahora` dentro de 14 días; `{día}` se calcula comparando su fecha local con la de `ahora` (`hoy`, `mañana` o `DiasSemana.ComoTexto`) y `{hora}` es su hora local `HH:mm`.

- [ ] **Paso 1: Pruebas que fallan** (zona `America/Mexico_City`, sin horario de verano; `ahora` fijo)
  - `"003/RF-12: dentro del horario está abierta"` → lunes 09:00–18:00, lunes 12:00 local → `Abierta`, `Mensaje == null`.
  - `"003/RF-13: antes de abrir dice hoy"` → lunes 07:00 → `"Estamos cerrados, abrimos hoy a las 09:00"`.
  - `"003/RF-13: después de cerrar dice mañana"` → lunes y martes 09:00–18:00, lunes 19:00 → `"... abrimos mañana a las 09:00"`.
  - `"003/RF-13: con días cerrados dice el nombre del día"` → sólo viernes 10:00–22:00, lunes 12:00 → `"... abrimos viernes a las 10:00"`.
  - `"003/RF-12: un turno que cruza la medianoche sigue abierto de madrugada"` → domingo 18:00–02:00, lunes 01:00 → `Abierta` (Foco 1).
  - `"003/RF-12: un cierre temporal vigente cierra la sucursal y la próxima apertura lo salta"` → lunes y martes 09:00–18:00, cierre de lunes 00:00 a martes 23:59, lunes 12:00 → mensaje con el miércoles si está abierto, o `"por el momento"` si no.
  - `"003/RF-12: un cierre futuro no cierra antes de su inicio"` → cierre que empieza mañana, hoy en horario → `Abierta` (Foco 2).
  - `"003/RF-13: sin aperturas en 14 días dice por el momento"` → semana cerrada → `"Estamos cerrados por el momento"`.
  - `"003/RF-12: usa la zona de la sucursal"` → zona `America/Tijuana`, lunes 09:00–18:00, `ahora` = lunes 17:30 UTC (09:30 en Tijuana) → `Abierta`.
- [ ] **Paso 2:** `dotnet test apps/api/tests/Restaurante.Application.Tests --filter "DisplayName~003/"` → no compila.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): calculadora del estado de atención (T-03)"`.

### T-04 [RF-15, RF-16]: Persistencia de sucursales con filtro global por convención

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Abstracciones/IContextoSucursal.cs`, `ISucursalRepository.cs`, `ICierreTemporalRepository.cs`
- Crear: `apps/api/src/Restaurante.Infrastructure/Persistencia/SucursalConfiguration.cs`, `HorarioDiaConfiguration.cs`, `CierreTemporalConfiguration.cs`, `SucursalRepository.cs`, `CierreTemporalRepository.cs`, `ContextoSucursalFijo.cs`, `FiltrosGlobales.cs`
- Modificar: `apps/api/src/Restaurante.Infrastructure/Persistencia/RestauranteDbContext.cs` (constructor con `IContextoSucursal`, `DbSet`s, filtros por convención), `RestauranteDbContextFactory.cs`, `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs`
- Modificar: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/PostgresFixture.cs` (`CrearContexto(string cadena, Guid? restauranteId = null, Guid? sucursalId = null)`), y los usos de los tests de Infrastructure y WebApi que construyen el contexto (`FabricaApi` registra un `IContextoSucursal` provisional nulo hasta T-07)
- Crear: migración `Sucursales`
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/SucursalesIntegracionTests.cs`
- Docs: `docs/context/architecture-map.md` (tablas y filtro por convención)

**Interfaces:**
- Produce: `interface IContextoSucursal { Guid? SucursalId { get; } }`; `sealed class ContextoSucursalFijo(Guid? sucursalId) : IContextoSucursal` (fábrica de diseño y pruebas).
- Produce: `RestauranteDbContext(DbContextOptions<RestauranteDbContext> options, IContextoRestaurante contexto, IContextoSucursal contextoSucursal)` con `private Guid? SucursalActualId => contextoSucursal.SucursalId;` y `FiltrosGlobales.Aplicar(ModelBuilder, RestauranteDbContext)`: para cada tipo que implemente `IEntidadDeSucursal`, `e.RestauranteId == RestauranteActualId && e.SucursalId == SucursalActualId`; si sólo `IEntidadDeRestaurante`, `e.RestauranteId == RestauranteActualId` (expresiones construidas por reflexión sobre las propiedades de instancia del contexto). El filtro de `Usuario` de 002 no cambia (no implementa la interfaz: su `RestauranteId` es nulo para plataforma).
- Produce (`ISucursalRepository`):
  ```csharp
  Task<Sucursal?> ObtenerPorIdAsync(Guid id, CancellationToken ct);
  Task<Sucursal?> ObtenerPorSlugAsync(string slug, CancellationToken ct);
  Task<IReadOnlyList<Sucursal>> ListarAsync(CancellationToken ct);                 // por Nombre
  Task<bool> ExisteNombreAsync(string nombre, Guid? excluirId, CancellationToken ct); // en el restaurante
  Task<bool> ExisteSlugAsync(string slug, CancellationToken ct);                      // en el restaurante
  Task<bool> ExistePrefijoAsync(string prefijo, Guid? excluirId, CancellationToken ct);
  Task<bool> ExisteWhatsAppAsync(string whatsApp, Guid? excluirId, CancellationToken ct);       // en la plataforma (IgnoreQueryFilters)
  Task<bool> ExistePhoneNumberIdAsync(string phoneNumberId, Guid? excluirId, CancellationToken ct); // en la plataforma
  void Agregar(Sucursal sucursal, IReadOnlyList<HorarioDia> horario);
  Task<IReadOnlyList<HorarioDia>> ObtenerHorarioAsync(CancellationToken ct);           // de la sucursal del contexto, por Dia
  Task GuardarCambiosAsync(CancellationToken ct);
  ```
- Produce (`ICierreTemporalRepository`): `Task<IReadOnlyList<CierreTemporal>> ListarDesdeAsync(DateTimeOffset desde, CancellationToken ct)` (con `Fin > desde`, por `Inicio`), `Task<CierreTemporal?> ObtenerPorIdAsync(Guid id, CancellationToken ct)`, `void Agregar(CierreTemporal cierre)`, `Task GuardarCambiosAsync(CancellationToken ct)`.
- Produce el esquema del diseño (tablas `sucursales`, `horarios_sucursal`, `cierres_temporales`) con índices únicos `ix_sucursales_restaurante_nombre`, `ix_sucursales_restaurante_slug`, `ix_sucursales_restaurante_prefijo`, `ix_sucursales_whatsapp` y `ix_sucursales_phone_number_id` (filtrado `phone_number_id IS NOT NULL`). `GuardarCambiosAsync` traduce `UniqueViolation` por `ConstraintName` a `ConflictoException` con: `"Ya existe una sucursal con ese nombre"`, `"... con ese slug"`, `"... con ese prefijo"`, `"... con ese WhatsApp"`, `"... con ese phone_number_id"`.

- [ ] **Paso 1: Pruebas de integración que fallan**
  - `"003/RF-16: con contexto de la sucursal A sólo se ven su horario y sus cierres"` → sembrar A y B (mismo restaurante) con horario y un cierre cada una; con contexto (R, A), `ObtenerHorarioAsync` y `ListarDesdeAsync` devuelven sólo los de A; `ObtenerPorIdAsync(idCierreDeB)` → `null`.
  - `"003/RF-16: sin sucursal en el contexto no se ve ninguna entidad de sucursal"` → contexto (R, null): horario vacío y ningún cierre.
  - `"003/RF-15: las sucursales de otro restaurante no se ven"` → con contexto del restaurante R2, `ObtenerPorSlugAsync("centro")` de R1 → `null`.
  - `"003/RF-03: los índices únicos traducen a ConflictoException"` → prefijo repetido en el mismo restaurante → `"Ya existe una sucursal con ese prefijo"`; mismo prefijo en otro restaurante → se guarda; WhatsApp repetido entre restaurantes → `"... con ese WhatsApp"`.
  - `"003/RF-12: las zonas horarias admitidas se resuelven en el servidor"` (unitaria en Infrastructure.Tests, sin Docker) → `TimeZoneInfo.FindSystemTimeZoneById(z)` funciona para cada `ZonasHorarias.Admitidas`.
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~003/RF-1"` y `"Category=Integracion&DisplayName~003/"` → no compila.
- [ ] **Paso 3:** implementar; `dotnet tool run dotnet-ef -- migrations add Sucursales …`; registrar repositorios y `TryAddScoped<IContextoSucursal>(_ => new ContextoSucursalFijo(null))` provisional (T-07 lo sustituye en la API).
- [ ] **Paso 4:** unitarias e integración → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): persistencia de sucursales con filtro global por convención (T-04)"`.

### T-05 [RF-01, RF-02, RF-03, RF-07]: Alta de sucursal

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/SucursalDto.cs`, `DatosSucursalDto.cs`, `ValidadorDatosSucursal.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/CrearSucursal/CrearSucursalCommand.cs`, `CrearSucursalHandler.cs`, `CrearSucursalValidator.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/SucursalesController.cs`
- Crear: dobles en memoria `SucursalRepositoryEnMemoria` en `apps/api/tests/Restaurante.Application.Tests/Dobles/` y `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/` (el de WebApi filtra por el `IContextoRestaurante` de la petición; `FabricaApi.Sucursales` como almacén compartido)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/CrearSucursalTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/SucursalesEndpointsTests.cs`
- Docs: `docs/context/http/sucursales.http` (nuevo), glosarios (`SucursalDto`, `DatosSucursalDto`)

**Interfaces:**
- Consume: `Sucursal`, `DatosSucursal`, `TelefonoMexico`, `ZonasHorarias` (T-01), `HorarioDia.SemanaCerrada` (T-02), `ISucursalRepository` (T-04), `IContextoRestaurante`, `IReloj`.
- Produce: `sealed record DatosSucursalDto(string Nombre, string Direccion, string Telefono, string WhatsApp, string? PhoneNumberId, string Prefijo, string? ZonaHoraria)` (zona nula → `ZonasHorarias.PorDefecto`); `ValidadorDatosSucursal : AbstractValidator<DatosSucursalDto>` con los mensajes: `"El nombre es obligatorio"`, `"El nombre no puede superar 80 caracteres"`, `"La dirección es obligatoria"`, `"La dirección no puede superar 200 caracteres"`, `"El teléfono debe ser un número mexicano: +52 y 10 dígitos"`, `"El WhatsApp debe ser un número mexicano: +52 y 10 dígitos"`, `"El phone_number_id debe tener sólo dígitos (hasta 30)"`, `"El prefijo debe tener de 1 a 3 letras mayúsculas"`, `"La zona horaria no es válida"`.
- Produce: `sealed record SucursalDto(Guid Id, string Nombre, string Direccion, string Telefono, string Slug, string WhatsApp, string? PhoneNumberId, string Prefijo, string ZonaHoraria, bool Activa)` con `static Desde(Sucursal)`.
- Produce: `CrearSucursalCommand(string Slug, DatosSucursalDto Datos) : IRequest<SucursalDto>`; el validador añade el slug (`"El slug debe tener entre 3 y 40 caracteres: minúsculas, números y guiones"`) e incluye `ValidadorDatosSucursal` en `Datos` (las claves de error salen sin prefijo: `nombre`, `slug`, `telefono`, …; usar `.ChildRules`/`OverridePropertyName` si hace falta). El handler comprueba las cinco unicidades (409 con los mensajes de T-04), agrega la sucursal con `HorarioDia.SemanaCerrada` y guarda una vez.
- Produce: `POST /api/r/{slug}/sucursales` con cuerpo `{ nombre, direccion, telefono, slug, whatsApp, phoneNumberId?, prefijo, zonaHoraria? }` → 201, `Location: /api/r/{slug}/sucursales/{id}`; controller con `[RequiereRol(Rol.AdministradorGeneral)]` a nivel de clase.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-01: crea la sucursal activa con la semana cerrada en el restaurante del contexto"` (handler) → `RestauranteId` del contexto, 7 `HorarioDia` cerrados, un guardado.
  - `"003/RF-01: POST devuelve 201 con Location y la zona por defecto"` → sin `zonaHoraria`, la respuesta trae `"America/Mexico_City"`.
  - `"003/RF-02: datos inválidos dan 400 por campo"` (Theory) → `AssertValidacionAsync` con `slug` (`"Centro"`), `telefono` (`"5512345678"`), `prefijo` (`"a"`), `zonaHoraria` (`"Europe/Madrid"`), `phoneNumberId` (`"x1"`).
  - `"003/RF-03: nombre, slug o prefijo repetidos en el restaurante dan 409"` y `"003/RF-03: un WhatsApp de otra sucursal de cualquier restaurante da 409"`.
  - `"003/RF-03: el mismo prefijo en otro restaurante se acepta"`.
  - `"003/RF-07: un rol distinto de administrador_general recibe 403"` (Theory: `cajero`, `cocinero`, `administrador_sucursal`, y token de plataforma).
- [ ] **Paso 2:** `dotnet test apps/api --filter "DisplayName~003/RF-01|DisplayName~003/RF-02|DisplayName~003/RF-03|DisplayName~003/RF-07"` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): alta de sucursal (T-05)"`.

### T-06 [RF-04, RF-05, RF-06, RF-07]: Listar, editar y activar o desactivar sucursales

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/ListarSucursales/*`, `EditarSucursal/*`, `CambiarEstadoSucursal/*`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/SucursalesController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/EditarSucursalTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/SucursalesEndpointsTests.cs`
- Docs: `docs/context/http/sucursales.http`

**Interfaces:**
- Consume: `SucursalDto`, `DatosSucursalDto`, `ValidadorDatosSucursal` (T-05).
- Produce: `ListarSucursalesQuery() : IRequest<IReadOnlyList<SucursalDto>>` → `GET /api/r/{slug}/sucursales`.
- Produce: `EditarSucursalCommand(Guid Id, DatosSucursalDto Datos) : IRequest<SucursalDto>` → `PUT /api/r/{slug}/sucursales/{id}`; un `slug` en el cuerpo se ignora; id de otro restaurante o inexistente → `NoEncontradoException("Sucursal no encontrada")` (404); unicidades excluyendo su propio id → 409.
- Produce: `CambiarEstadoSucursalCommand(Guid Id, bool? Activa) : IRequest<SucursalDto>` → `PUT /api/r/{slug}/sucursales/{id}/estado` con `{ activa }`; sin `activa` → 400 `"El estado es obligatorio"`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-04: GET lista nombre, dirección, slug, prefijo y estado de las sucursales del restaurante"` → no incluye sucursales de otro restaurante.
  - `"003/RF-05: editar cambia los datos y no el slug"` → cuerpo con `slug: "otro"`; respuesta y repositorio conservan el original.
  - `"003/RF-05: editar con un prefijo de otra sucursal da 409 y con el propio no"`.
  - `"003/RF-05: editar una sucursal de otro restaurante da 404"`.
  - `"003/RF-06: desactivar y reactivar conserva horario y cierres"` (WebApi, sembrando horario y un cierre).
  - `"003/RF-07: listar, editar y cambiar estado exigen administrador_general"` → 403 con `cajero`.
- [ ] **Paso 2:** filtro `DisplayName~003/RF-04|DisplayName~003/RF-05|DisplayName~003/RF-06|DisplayName~003/RF-07` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): listar, editar y cambiar estado de sucursales (T-06)"`.

### T-07 [RF-14, RF-15]: Contexto de sucursal desde la ruta

**Archivos:**
- Crear: `apps/api/src/Restaurante.WebApi/Seguridad/ContextoSucursal.cs`, `MiddlewareContextoSucursal.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/DependencyInjection.cs`, `Program.cs` (tras `MiddlewareContextoRestaurante`), `apps/api/src/Restaurante.Infrastructure/DependencyInjection.cs` (quitar el `TryAddScoped` provisional de T-04)
- Modificar: `apps/api/tests/Restaurante.WebApi.Tests/Infraestructura/FabricaApi.cs` (quitar el contexto de sucursal provisional; el real lo fija el middleware)
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/ContextoSucursalTests.cs`
- Docs: `docs/context/architecture-map.md` (middleware de sucursal)

**Interfaces:**
- Produce: `sealed class ContextoSucursal : IContextoSucursal { void Establecer(Guid sucursalId); }`, registrado como `ContextoRestaurante` en 002 (scoped + reenvío a la interfaz).
- Produce: `MiddlewareContextoSucursal.InvokeAsync(HttpContext, ContextoSucursal, ISucursalRepository)`: si la ruta cumple `^/api/r/[^/]+/s/([^/]+)` (sin distinguir mayúsculas) y hay restaurante en el contexto, busca `ObtenerPorSlugAsync(slug.ToLowerInvariant())` (el filtro de restaurante ya aplica); nula → 404 con título `"Sucursal no disponible"` y detalle `"La dirección no corresponde a una sucursal de este restaurante."`; si existe (activa o no) → `Establecer(id)`. Constantes públicas `TituloNoDisponible` y `DetalleNoDisponible` para reutilizarlas en T-10.

- [ ] **Paso 1: Pruebas que fallan** (con un endpoint de prueba en `ControllersDePrueba.cs` bajo `api/r/{slug}/s/{sucursal}/eco-contexto` que devuelve el `SucursalId` del contexto, marcado `[EndpointAnonimo]`)
  - `"003/RF-15: la ruta de una sucursal fija su contexto"` → devuelve el id de `centro`; `/s/CENTRO/` igual.
  - `"003/RF-15: una sucursal de otro restaurante en la ruta da 404"` → `/api/r/resto-a/s/{slugDeB}/…` con `slugDeB` sólo en `resto-b` → 404 `"Sucursal no disponible"`.
  - `"003/RF-14: un slug de sucursal inexistente da 404 Sucursal no disponible"`.
  - `"003/RF-15: fuera de /s/ no hay contexto de sucursal"` → `GET /api/r/resto-a/sucursales` sigue funcionando y el contexto queda nulo.
- [ ] **Paso 2:** filtro `DisplayName~003/RF-14|DisplayName~003/RF-15` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS; integración → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): contexto de sucursal desde la ruta (T-07)"`.

### T-08 [RF-08, RF-09, RF-15]: Consultar y guardar el horario

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/Horario/HorarioDiaDto.cs`, `ObtenerHorarioQuery.cs`, `ObtenerHorarioHandler.cs`, `GuardarHorarioCommand.cs`, `GuardarHorarioHandler.cs`, `GuardarHorarioValidator.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/SucursalController.cs` (ruta `api/r/{slug}/s/{sucursal}`, `[RequiereRol(Rol.AdministradorGeneral)]`)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/GuardarHorarioTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/HorarioEndpointsTests.cs`
- Docs: `docs/context/http/sucursales.http`, glosarios (`HorarioDiaDto`)

**Interfaces:**
- Consume: `ISucursalRepository.ObtenerHorarioAsync` (T-04), `HorarioDia.Cambiar`, `DiasSemana` (T-02), contexto de sucursal (T-07).
- Produce: `sealed record HorarioDiaDto(string Dia, bool Cerrado, string? Apertura, string? Cierre)` (horas `HH:mm`).
- Produce: `ObtenerHorarioQuery() : IRequest<IReadOnlyList<HorarioDiaDto>>` → `GET /api/r/{slug}/s/{sucursal}/horario` (7 días en orden lunes→domingo).
- Produce: `GuardarHorarioCommand(IReadOnlyList<HorarioDiaDto> Dias) : IRequest<IReadOnlyList<HorarioDiaDto>>` → `PUT …/horario` con `{ dias: [...] }`. Validador: `"El horario debe incluir los siete días"` (clave `dias`; exactamente 7 días distintos y válidos); por día: `"La hora debe tener el formato HH:mm"`, `"Un día abierto necesita hora de apertura y de cierre"`, `"La hora de apertura y la de cierre no pueden ser iguales"` (claves `dias[i].apertura` etc.). `Cerrado == true` ignora las horas.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-08: guardar reemplaza los siete días y GET los devuelve en orden"`.
  - `"003/RF-09: un horario con días faltantes, horas iguales o mal escritas da 400"` (Theory).
  - `"003/RF-09: un cierre anterior a la apertura se acepta como turno nocturno"` → `18:00`–`02:00` guarda y se devuelve igual.
  - `"003/RF-15: el horario de la sucursal A no se ve desde la ruta de la B"` → guardar en A y consultar B devuelve la semana de B, intacta.
  - `"003/RF-07: guardar el horario exige administrador_general"` → 403 con `cajero`.
- [ ] **Paso 2:** filtro `DisplayName~003/RF-08|DisplayName~003/RF-09` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): consultar y guardar el horario (T-08)"`.

### T-09 [RF-10, RF-11, RF-15]: Cierres temporales

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/Cierres/CierreTemporalDto.cs`, `HoraLocal.cs`, `CrearCierreCommand.cs`, `CrearCierreHandler.cs`, `CrearCierreValidator.cs`, `ListarCierresQuery.cs`, `ListarCierresHandler.cs`, `CancelarCierreCommand.cs`, `CancelarCierreHandler.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/SucursalController.cs`
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/CierresTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/CierresEndpointsTests.cs`
- Docs: `docs/context/http/sucursales.http`, glosarios (`CierreTemporalDto`)

**Interfaces:**
- Consume: `CierreTemporal` (T-02), `ICierreTemporalRepository`, `ISucursalRepository.ObtenerPorIdAsync` (T-04), `IContextoSucursal`, `IReloj`.
- Produce: `static class HoraLocal { const string Formato = "yyyy-MM-ddTHH:mm"; static bool TryLeer(string? texto, out DateTime local); static DateTimeOffset AUtc(DateTime local, TimeZoneInfo zona); static string DesdeUtc(DateTimeOffset utc, TimeZoneInfo zona); }`.
- Produce: `sealed record CierreTemporalDto(Guid Id, string Inicio, string Fin, string? Motivo)` (horas locales de la sucursal, formato `HoraLocal.Formato`).
- Produce: `CrearCierreCommand(string? Inicio, string Fin, string? Motivo) : IRequest<CierreTemporalDto>` → `POST /api/r/{slug}/s/{sucursal}/cierres` → 201. `Inicio` nulo = ahora. Validador: `"La fecha debe tener el formato yyyy-MM-ddTHH:mm"`, `"El motivo no puede superar 120 caracteres"`; el handler convierte con la zona de la sucursal y traduce las reglas de `CierreTemporal.Crear` a 400: `"El fin debe ser posterior al inicio"` (clave `fin`), `"El fin no puede estar en el pasado"` (clave `fin`) lanzando `ValidationException`.
- Produce: `ListarCierresQuery() : IRequest<IReadOnlyList<CierreTemporalDto>>` → `GET …/cierres` (vigentes y futuros: `Fin > ahora`).
- Produce: `CancelarCierreCommand(Guid Id) : IRequest<CierreTemporalDto>` → `POST …/cierres/{id}/cancelar` → 200; id de otra sucursal o inexistente → `NoEncontradoException("Cierre no encontrado")`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-10: crear un cierre sin inicio empieza ahora y se lista"`.
  - `"003/RF-10: fin anterior al inicio, fin pasado o fecha mal escrita dan 400"` (Theory).
  - `"003/RF-10: las fechas se interpretan en la zona de la sucursal"` → sucursal en `America/Tijuana`, `inicio "2026-12-24T09:00"` → guardado como `2026-12-24T17:00Z` y devuelto como `"2026-12-24T09:00"` (Foco 4).
  - `"003/RF-11: cancelar un cierre lo saca de la lista de vigentes"`.
  - `"003/RF-15: cancelar desde la sucursal A un cierre de la B da 404"` → y el cierre de B sigue vigente.
- [ ] **Paso 2:** filtro `DisplayName~003/RF-10|DisplayName~003/RF-11` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): cierres temporales (T-09)"`.

### T-10 [RF-12, RF-13, RF-14]: Estado de atención público

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Sucursales/EstadoAtencion/ObtenerEstadoAtencionQuery.cs`, `ObtenerEstadoAtencionHandler.cs`
- Crear: `apps/api/src/Restaurante.WebApi/Controllers/EstadoSucursalController.cs` (ruta `api/r/{slug}/s/{sucursal}`, acción `[EndpointAnonimo]`)
- Modificar: `apps/api/Dockerfile` (etapa de ejecución: `apt-get install -y --no-install-recommends tzdata` antes de `USER`)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Sucursales/ObtenerEstadoAtencionTests.cs`, `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/EstadoAtencionEndpointTests.cs`
- Docs: `docs/context/http/sucursales.http`, glosarios (`EstadoAtencionDto`), `docs/ONBOARDING.md` (tzdata en la imagen)

**Interfaces:**
- Consume: `CalculadoraEstadoAtencion`, `EstadoAtencionDto` (T-03), repositorios (T-04), contexto (T-07), `IReloj`, constantes de `MiddlewareContextoSucursal`.
- Produce: `ObtenerEstadoAtencionQuery() : IRequest<EstadoAtencionDto>`; sucursal inactiva → `SucursalNoDisponibleException` nueva en `Application/Comun/Excepciones`, que `FiltroExcepciones` traduce a 404 con título `"Sucursal no disponible"`.
- Produce: `GET /api/r/{slug}/s/{sucursal}/estado` → 200 `{ abierta, mensaje }`, sin token.

- [ ] **Paso 1: Pruebas que fallan**
  - `"003/RF-12: el estado de una sucursal abierta responde abierta sin token"` → `RelojFijo` dentro del horario; `abierta == true`, `mensaje == null`.
  - `"003/RF-13: cerrada devuelve el mensaje con la próxima apertura"` → `"Estamos cerrados, abrimos mañana a las 09:00"`.
  - `"003/RF-14: una sucursal inactiva da 404 Sucursal no disponible"`.
  - `"003/RF-14: una sucursal inexistente da 404 Sucursal no disponible"`.
- [ ] **Paso 2:** filtro `DisplayName~003/RF-12|DisplayName~003/RF-13|DisplayName~003/RF-14` → FAIL.
- [ ] **Paso 3:** implementar (el handler resuelve la zona con `TimeZoneInfo.FindSystemTimeZoneById`).
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS; integración → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): estado de atención público (T-10)"`.

### T-11 [RF-17, RF-20]: Lista y formulario de sucursales
UI: sí

**Archivos:**
- Mover: `apps/web/src/plataforma/DialogoConfirmacion.tsx` (+ `.module.css`) → `apps/web/src/componentes/`; extraer `Campo` de `FormularioRestaurante.tsx` a `apps/web/src/componentes/CampoFormulario.tsx` (+ `.module.css`) y usarlo desde ambos formularios
- Crear: `apps/web/src/restaurante/MarcoRestaurante.tsx` (encabezado de marca + contenido; `InicioRestaurante` pasa a usarlo)
- Crear: `apps/web/src/sucursales/api.ts`, `SucursalesPagina.tsx`, `FormularioSucursal.tsx` y sus `*.module.css`
- Modificar: `apps/web/src/rutas/RutaProtegida.tsx` (prop `redirigirSinRol?: string`), `apps/web/src/restaurante/InicioRestaurante.tsx` (enlace "Sucursales"), `apps/web/src/App.tsx` (rutas hijas de `/r/:slug`: `sucursales`, `sucursales/:sucursal`)
- Crear: `apps/web/e2e/apiSimuladaSucursales.ts`
- Test: `apps/web/src/sucursales/FormularioSucursal.test.tsx`, `apps/web/src/rutas/RutaProtegida.test.tsx` (caso nuevo), `apps/web/e2e/sucursales.spec.ts`

**Interfaces:**
- Produce (`sucursales/api.ts`): `type Sucursal = { id; nombre; direccion; telefono; slug; whatsApp; phoneNumberId: string | null; prefijo; zonaHoraria; activa: boolean }`; `type DatosSucursal` (sin `id`/`activa`; `slug` sólo en el alta); `ZONAS_HORARIAS` (las 12 de RNF-01); `listarSucursales(token, slugRestaurante)`, `crearSucursal(token, slugRestaurante, datos)`, `editarSucursal(token, slugRestaurante, id, datos)`, `cambiarEstadoSucursal(token, slugRestaurante, id, activa)`.
- Produce: `RutaProtegida({ roles, slug?, redirigirSinRol?, children })`: sin sesión o de otro restaurante → login (como en 002); sesión válida con rol no permitido → `redirigirSinRol` si se indica.
- Produce: `SucursalesPagina` en `/r/{slug}/sucursales` (dentro de `RutaProtegida roles={['administrador_general']} redirigirSinRol={`/r/${slug}/inicio`}`): tabla `Nombre`, `Dirección`, `Estado` (`Activa`/`Inactiva`), `Acciones` (`Editar`, `Ver horario` → detalle, `Desactivar`/`Activar`); botón `Nueva sucursal`; desactivar con `DialogoConfirmacion` (título `¿Desactivar {nombre}?`, mensaje `Dejará de aparecer en los canales hasta que la reactives.`).
- Produce: `FormularioSucursal({ sucursal?, onGuardado, onCancelar })` con etiquetas `Nombre`, `Dirección`, `Teléfono`, `Slug` (deshabilitado al editar), `WhatsApp`, `phone_number_id (opcional)`, `Prefijo` (se convierte a mayúsculas al escribir), `Zona horaria` (`select`, `America/Mexico_City` por defecto); `aria-label` `Nueva sucursal` o `Editar {nombre}`; errores 400 junto a su campo y 409 como alerta.
- Produce: en `/r/{slug}/inicio`, enlace `Sucursales` (a `/r/{slug}/sucursales`) sólo para `administrador_general`.

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("003/RF-17: el prefijo se escribe en mayúsculas y el slug se bloquea al editar")`.
  - Vitest `it("003/RF-20: un rol no permitido con sesión válida va a la ruta indicada")` (RutaProtegida).
  - Playwright `test("003/RF-17: el administrador general ve la lista de sucursales con su estado")`.
  - Playwright `test("003/RF-17: crear una sucursal la añade a la lista")` → `POST` con `zonaHoraria: "America/Mexico_City"` por defecto y `prefijo: "B"` aunque se escriba `b`.
  - Playwright `test("003/RF-17: los errores 400 se muestran junto a su campo")`.
  - Playwright `test("003/RF-17: desactivar pide confirmación y cambia el estado")`.
  - Playwright `test("003/RF-20: el inicio muestra Sucursales sólo al administrador general")` → con `cajero` no aparece y `/r/la-esquina/sucursales` lleva a `/r/la-esquina/inicio`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "003/" && npx playwright test e2e/sucursales.spec.ts)` → FAIL; commit de las pruebas Playwright solas (referencia para Ralph Loop).
- [ ] **Paso 3:** fase verde con Ralph Loop (`/implementar`; promesa `T-11 VERDE`; sin tocar las pruebas). Las pruebas de 002 deben seguir pasando tras mover `DialogoConfirmacion` y extraer `CampoFormulario`.
- [ ] **Paso 4:** el mismo comando y toda la suite → PASS en `escritorio` y `movil`; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): lista y formulario de sucursales (T-11)"`.

### T-12 [RF-18]: Detalle de sucursal con horario y cierres
UI: sí

**Archivos:**
- Crear: `apps/web/src/sucursales/DetalleSucursalPagina.tsx`, `EditorHorario.tsx`, `CierresTemporales.tsx`, `EstadoSucursal.tsx` y sus `*.module.css`
- Modificar: `apps/web/src/sucursales/api.ts`, `apps/web/e2e/apiSimuladaSucursales.ts`
- Test: `apps/web/src/sucursales/EditorHorario.test.tsx`, `apps/web/e2e/detalleSucursal.spec.ts`

**Interfaces:**
- Produce (`api.ts`): `type HorarioDia = { dia: string; cerrado: boolean; apertura: string | null; cierre: string | null }`, `type Cierre = { id; inicio; fin; motivo: string | null }`, `type EstadoAtencion = { abierta: boolean; mensaje: string | null }`; `obtenerHorario`, `guardarHorario`, `listarCierres`, `crearCierre`, `cancelarCierre` (con token) y `obtenerEstado(slugRestaurante, slugSucursal)` (sin token).
- Produce: `EstadoSucursal` muestra `Abierta ahora` o el `mensaje` del estado (`role="status"`).
- Produce: `EditorHorario({ horario, onGuardar })`: una fila por día con la etiqueta del día, casilla `Cerrado el {día}`, y campos `type="time"` `Apertura del {día}` y `Cierre del {día}` (deshabilitados si está cerrado); botón `Guardar horario`; aviso `El turno termina al día siguiente` cuando cierre < apertura; errores 400 por día.
- Produce: `CierresTemporales`: lista (inicio, fin, motivo; botón `Cancelar cierre` con confirmación) y formulario `Inicio` (`datetime-local`, vacío = ahora), `Fin`, `Motivo` y botón `Cerrar temporalmente`. Las fechas se envían tal cual (hora local de la sucursal, sin zona). Tras crear o cancelar, se vuelve a pedir el estado.
- Produce: `DetalleSucursalPagina` en `/r/{slug}/sucursales/{sucursal}`, con título = nombre de la sucursal y enlace `Volver a sucursales`.

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("003/RF-18: marcar Cerrado deshabilita las horas y un cierre menor avisa del turno nocturno")`.
  - Playwright `test("003/RF-18: el detalle muestra el estado y el horario de la sucursal")` → estado simulado cerrado con su mensaje.
  - Playwright `test("003/RF-18: guardar el horario envía los siete días")` → cuerpo `PUT` con 7 días y `apertura "09:00"` en lunes.
  - Playwright `test("003/RF-18: crear y cancelar un cierre temporal actualiza la lista y el estado")`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "003/RF-18" && npx playwright test e2e/detalleSucursal.spec.ts)` → FAIL; commit de las pruebas Playwright solas.
- [ ] **Paso 3:** fase verde con Ralph Loop (promesa `T-12 VERDE`).
- [ ] **Paso 4:** el mismo comando y toda la suite → PASS en ambos proyectos; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): detalle de sucursal con horario y cierres (T-12)"`.

### T-13 [RF-19]: Selector de sucursal en el encabezado
UI: sí

**Archivos:**
- Crear: `apps/web/src/sucursales/SelectorSucursal.tsx` (+ `.module.css`)
- Modificar: `apps/web/src/sesion/almacenSesion.ts` (`Sesion` gana `sucursalActiva?: string | null`), `apps/web/src/sesion/ProveedorSesion.tsx` (`elegirSucursal(slug: string | null): void`), `apps/web/src/restaurante/EncabezadoMarca.tsx` (muestra el selector para `administrador_general`)
- Test: `apps/web/src/sucursales/SelectorSucursal.test.tsx`, `apps/web/e2e/selectorSucursal.spec.ts`

**Interfaces:**
- Consume: `listarSucursales` (T-11), `useSesion`.
- Produce: `useSesion()` gana `elegirSucursal(slug)`, que guarda la sesión con `sucursalActiva` (memoria + `sessionStorage`, como en 002).
- Produce: `SelectorSucursal({ slugRestaurante })`: `select` con etiqueta `Sucursal`, opción `Elegir sucursal` y sólo las sucursales activas; al elegir, `elegirSucursal(slug)` y navega a `/r/{slugRestaurante}/sucursales/{slug}`. Si `sucursalActiva` ya no está entre las activas, llama a `elegirSucursal(null)` (Foco 5). La lista se invalida cuando `SucursalesPagina` cambia una sucursal (clave de consulta compartida `['sucursales', slugRestaurante]`).

- [ ] **Paso 1: Pruebas que fallan**
  - Vitest `it("003/RF-19: el selector sólo lista sucursales activas y recuerda la elegida")`.
  - Vitest `it("003/RF-19: si la sucursal activa se desactiva, deja de ser la activa")` (Foco 5).
  - Playwright `test("003/RF-19: elegir una sucursal la muestra en el encabezado y lleva a su detalle")`.
  - Playwright `test("003/RF-19: el selector no aparece para un cajero")`.
- [ ] **Paso 2:** `(cd apps/web && npx vitest run -t "003/RF-19" && npx playwright test e2e/selectorSucursal.spec.ts)` → FAIL; commit de las pruebas Playwright solas.
- [ ] **Paso 3:** fase verde con Ralph Loop (promesa `T-13 VERDE`).
- [ ] **Paso 4:** el mismo comando y toda la suite → PASS en ambos proyectos; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(003): selector de sucursal en el encabezado (T-13)"`.

### T-14 [RF-01, RF-12]: Prueba de punta a punta de sucursales

**Archivos:**
- Crear: `apps/web/e2e-stack/sucursales.spec.ts`
- Docs: `docs/ONBOARDING.md` (qué cubre la suite completa)

**Interfaces:**
- Consume: todo lo anterior, `ADMIN_USERNAME`/`ADMIN_PASSWORD` del entorno (falla con `"Exporta ADMIN_USERNAME y ADMIN_PASSWORD (.env)"` si faltan) y `E2E_BASE_URL`.

- [ ] **Paso 1: Prueba que falla**
  - `test("003/RF-01: el administrador general da de alta una sucursal y su estado público responde")`. Flujo: la plataforma crea un restaurante `e2e-suc-<marca de tiempo>` con su administrador; el administrador entra en `/r/<slug>/login`, abre `Sucursales`, crea `Centro` (slug `centro`, prefijo `A`, WhatsApp único derivado de la marca de tiempo), abre su detalle y guarda un horario de lunes a domingo 00:00–23:59. Después, `GET /api/r/<slug>/s/centro/estado` (con `request`, sin token) responde 200 con `abierta` booleano (RF-12) y la imagen resuelve la zona (prueba indirecta de `tzdata`).
- [ ] **Paso 2:** `docker compose up -d --build`, `set -a; . ./.env; set +a` y `(cd apps/web && npx playwright test --config playwright.stack.config.ts e2e-stack/sucursales.spec.ts)` → FAIL antes de tener las pantallas desplegadas.
- [ ] **Paso 3:** ajustar lo necesario (sólo si la prueba revela un fallo real del sistema).
- [ ] **Paso 4:** el mismo comando → PASS; `comando_e2e_stack` completo → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "test(003): prueba de punta a punta de sucursales (T-14)"`.

## Cobertura RF → tareas

| RF | Tareas | RF | Tareas | RF | Tareas |
|---|---|---|---|---|---|
| RF-01 | T-01, T-05, T-14 | RF-08 | T-02, T-08 | RF-15 | T-04, T-07, T-08, T-09 |
| RF-02 | T-05 | RF-09 | T-02, T-08 | RF-16 | T-04 |
| RF-03 | T-05 | RF-10 | T-02, T-09 | RF-17 | T-11 |
| RF-04 | T-06 | RF-11 | T-02, T-09 | RF-18 | T-12 |
| RF-05 | T-01, T-06 | RF-12 | T-03, T-10, T-14 | RF-19 | T-13 |
| RF-06 | T-01, T-06 | RF-13 | T-03, T-10 | RF-20 | T-11 |
| RF-07 | T-05, T-06 | RF-14 | T-07, T-10 | | |

RNF-01 → T-01, T-03, T-09 · RNF-02 → T-01 · RNF-03 → proyectos `escritorio`/`movil` (T-11 a T-13) · RNF-04 → T-03 y T-10 (`IReloj`).
