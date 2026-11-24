---
feature: 004-usuarios-roles-y-asignacion-a-sucursales
estado: borrador
---
# Tareas: Usuarios, roles y asignación a sucursales (AUTH-01)

> **Para agentes:** se ejecuta con `/implementar`, tarea por tarea, con TDD. Cada tarea se cierra
> con `python3 .claude/scripts/completar-tarea.py T-NN` (sólo con `comando_test` en verde). Los
> pasos usan casillas (`- [ ]`).

**Objetivo:** que cada usuario tenga rol y sucursales, que el administrador de sucursal gestione a
su personal exclusivo, que las sesiones se corten al cambiar el acceso y que cada persona entre
sólo a sus sucursales, con pantallas de usuarios y de elección de sucursal.

**Arquitectura:** el usuario gana sucursales asignadas (`usuarios_sucursales`) y un
`SelloSesion` que viaja en el token como claim `sello`. Un middleware nuevo, justo después de
`UseAuthentication`, carga al usuario por clave primaria y rechaza el token si el usuario no
existe, está inactivo o el sello cambió; deja rol y sucursales vigentes en `IUsuarioActual`. Una
clase pura, `AlcanceGestionUsuarios`, decide qué usuarios ve y gestiona cada administrador. El
middleware de sucursal de 003 rechaza las sucursales no asignadas o inactivas.

**Stack:** .NET 10, MediatR 12.x, FluentValidation, EF Core + Npgsql, xUnit, Testcontainers;
React + TypeScript + Vite, Vitest + Testing Library, Playwright.

**Spec:** `specs/004-usuarios-roles-y-asignacion-a-sucursales/requerimiento.md` y `diseno.md`;
constitución en `specs/000-constitucion/`. El código de 002 (usuarios, login, `FabricaApi`) y de
003 (sucursales, middleware de sucursal, `SelectorSucursal`) es la base: seguir sus patrones.

## Restricciones globales

- Tests con el RF calificado: `[Fact(DisplayName = "004/RF-NN: …")]`, `it("004/RF-NN: …")`, `test("004/RF-NN: …")`.
- Integración con PostgreSQL: `[Trait("Category", "Integracion")]` + `[Collection(PostgresCollection.Nombre)]`; se ejecuta a mano en la tarea que la crea (Docker encendido).
- `dotnet`: anteponer `PATH=/root/.dotnet:$PATH`. EF: `dotnet tool run dotnet-ef -- migrations add <Nombre> --project src/Restaurante.Infrastructure --startup-project src/Restaurante.Infrastructure --output-dir Persistencia/Migraciones` desde `apps/api`.
- Roles en API y token: `administrador_plataforma`, `administrador_general`, `administrador_sucursal`, `cajero`, `cocinero`.
- Cardinalidad rol ↔ sucursales (RF-02): `administrador_general` y `administrador_plataforma` 0; `administrador_sucursal` exactamente 1; `cajero` y `cocinero` ≥ 1; sin duplicados. Error 400 en el campo `sucursales`. La API recibe y devuelve sucursales por `slug`.
- Textos fijos: 401 título `Sesión no válida`, detalle `Vuelve a iniciar sesión.`, con `WWW-Authenticate: Bearer`; 403 del login título `Sin sucursales activas`; 404 de sucursal `Sucursal no disponible` (constante de 003); 403 de alcance título `Acceso denegado` (el de `RequiereRol`). Frontend: `Tu sesión terminó. Vuelve a iniciar sesión.`, `No tienes sucursales activas. Habla con tu administrador.`, `No tienes permiso para esta acción.`.
- Claim del token: `sello` = `SelloSesion` (Guid en formato `D`).
- Orden de errores en usuarios: 400 del validador (pipeline) → 404 usuario de otro restaurante → 403 alcance → 409 reglas de administrador.
- El sello se renueva al desactivar, al cambiar contraseña y al cambiar rol o conjunto de sucursales; una edición sin cambios no lo renueva; reactivar no lo renueva.
- Art. 5: todo endpoint nuevo o cambiado actualiza `docs/context/http/usuarios.http` (o `auth.http`) y los glosarios (`docs/GLOSSARY.md`, `docs/context/glossary.md`) en la misma tarea; los cambios del pipeline, `docs/context/architecture-map.md`.
- Frontend: sólo variables de tema; escritorio 1440 px y móvil 390 px (proyectos de Playwright ya configurados); el token se pasa siempre explícito a `pedirApi`.

## Foco de revisión

1. **Administrador general que se asigna contraseña a sí mismo:** su sesión se corta y el frontend lo lleva al login con el aviso, sin pantalla en blanco ni bucle de peticiones (T-07, T-12).
2. **Cajero compartido Centro + Norte visto por el administrador del Centro:** aparece en la lista sin acciones y cualquier PUT sobre él da 403 (T-05, T-06).
3. **Token emitido antes de la migración (sin claim `sello`):** responde 401 "Sesión no válida", nunca 500 (T-04).
4. **Edición que reenvía el mismo rol y las mismas sucursales en otro orden:** no corta la sesión (T-01).
5. **Sucursal guardada en `sessionStorage` que fue desactivada o desasignada:** al recargar, el selector la descarta y el usuario elige otra o ve el aviso sin sucursales (T-11).

---

### T-01 [RF-02, RF-13]: Usuario con sucursales y sello de sesión

**Archivos:**
- Modificar: `apps/api/src/Restaurante.Domain/Usuarios/Usuario.cs`
- Crear: `apps/api/src/Restaurante.Domain/Usuarios/UsuarioSucursal.cs`
- Modificar (llamadas a `Crear`): `Restaurante.Infrastructure/Arranque/InicializadorAdministrador.cs`, `Restaurante.Application/Features/Restaurantes/CrearRestaurante/CrearRestauranteHandler.cs`, `Restaurante.Application/Features/Usuarios/CrearUsuario/CrearUsuarioHandler.cs` (pasan `[]` por ahora)
- Test: `apps/api/tests/Restaurante.Domain.Tests/Usuarios/UsuarioSucursalesTests.cs`

**Interfaces:**
- Produce: `sealed class UsuarioSucursal : IEntidadDeRestaurante { Guid UsuarioId; Guid SucursalId; Guid RestauranteId; }` (constructor privado para EF).
- Produce en `Usuario`: `IReadOnlyCollection<Guid> Sucursales`; `Guid SelloSesion`; `static Usuario Crear(Guid? restauranteId, string nombreUsuario, string hashContrasena, Rol rol, IReadOnlyCollection<Guid> sucursales, DateTimeOffset creadoEn)`; `static void ValidarSucursales(Rol rol, int cantidad)` (lanza `ArgumentException` con la cardinalidad de las restricciones globales); `bool AsignarRolYSucursales(Rol rol, IReadOnlyCollection<Guid> sucursales)` (devuelve si cambió; compara como conjunto; renueva el sello sólo si cambió); `void CambiarContrasena(string hash)` (renueva); `Desactivar()` renueva; `Reactivar()` no. Se elimina `CambiarRol`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-02: la cardinalidad de sucursales depende del rol"` (Theory): válidos `(AdministradorGeneral,0)`, `(AdministradorSucursal,1)`, `(Cajero,1)`, `(Cocinero,3)`; inválidos `(AdministradorGeneral,1)`, `(AdministradorSucursal,0)`, `(AdministradorSucursal,2)`, `(Cajero,0)` → `ArgumentException`.
  - `"004/RF-02: Crear rechaza sucursales duplicadas"` → `[a, a]` con `Cajero` lanza.
  - `"004/RF-13: un usuario nuevo tiene sello y sus sucursales"`.
  - `"004/RF-13: cambiar rol o sucursales renueva el sello"`; `"004/RF-13: reasignar lo mismo en otro orden no renueva el sello"` (devuelve `false`, Foco 4).
  - `"004/RF-13: desactivar y cambiar contraseña renuevan el sello; reactivar no"`.
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Domain.Tests --filter "DisplayName~004/"` → no compila.
- [ ] **Paso 3:** implementar (Domain sin paquetes externos, Art. 6.2); ajustar los tres llamadores.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): usuario con sucursales y sello de sesión (T-01)"`.

### T-02 [RF-01, RF-03, RF-13]: Persistencia de sucursales del usuario y sello

**Archivos:**
- Modificar: configuración EF de `Usuario` (en `apps/api/src/Restaurante.Infrastructure/Persistencia/`), `UsuarioRepository.cs`, `RestauranteDbContext.cs`
- Crear: configuración de `UsuarioSucursal`; migración `UsuariosSucursales`
- Modificar: `apps/api/src/Restaurante.Application/Abstracciones/IUsuarioRepository.cs`; dobles `UsuarioRepositoryEnMemoria.cs` de `tests/Restaurante.Application.Tests/Dobles/` y `tests/Restaurante.WebApi.Tests/Infraestructura/`
- Test: `apps/api/tests/Restaurante.Infrastructure.Tests/Persistencia/UsuariosSucursalesTests.cs` (integración)

**Interfaces:**
- Consume: `Usuario`, `UsuarioSucursal` (T-01).
- Produce en `IUsuarioRepository`: `Task<Usuario?> ObtenerParaSesionAsync(Guid id, CancellationToken ct)` (por PK, `IgnoreQueryFilters`, con sucursales, sin seguimiento); `ObtenerPorIdAsync` y `ObtenerPorNombreNormalizadoAsync` incluyen sucursales; `ListarAsync` ordena por `NombreUsuarioNormalizado`.
- Datos: `usuarios.sello_sesion uuid not null` (la migración rellena cada fila con `gen_random_uuid()`); `usuarios_sucursales(usuario_id FK usuarios on delete cascade, sucursal_id FK sucursales on delete restrict, restaurante_id not null)`, PK `(usuario_id, sucursal_id)`, índice por `sucursal_id`; filtro global por restaurante (convención de 003).

- [ ] **Paso 1: Pruebas que fallan (integración)**
  - `"004/RF-01: las sucursales del usuario se guardan y se leen"`.
  - `"004/RF-13: la migración da un sello distinto a cada usuario existente"` (insertar dos usuarios con la migración anterior y aplicar `UsuariosSucursales`; RNF-04).
  - `"004/RF-13: ObtenerParaSesionAsync ignora el filtro de restaurante"` (contexto sin restaurante).
  - `"004/RF-01: no se puede asignar una sucursal inexistente"` (FK → excepción).
  - `"004/RF-13: la consulta de sesión tarda menos de 10 ms de mediana"` (100 lecturas tras calentar; RNF-01).
- [ ] **Paso 2:** `PATH=/root/.dotnet:$PATH dotnet test apps/api/tests/Restaurante.Infrastructure.Tests --filter "DisplayName~004/"` (Docker encendido) → FAIL.
- [ ] **Paso 3:** configuración, repositorio, dobles en memoria y migración.
- [ ] **Paso 4:** el mismo filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): persistencia de sucursales del usuario y sello (T-02)"`.

### T-03 [RF-07, RF-08, RF-09, RF-10, RF-11, RF-26]: Alcance de gestión de usuarios

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Usuarios/AlcanceGestionUsuarios.cs`, `ActorUsuarios.cs`
- Crear: `apps/api/src/Restaurante.Application/Comun/Excepciones/ProhibidoException.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Filtros/FiltroExcepciones.cs` (`ProhibidoException` → 403 `Acceso denegado` con su mensaje como detalle)
- Test: `apps/api/tests/Restaurante.Application.Tests/Features/Usuarios/AlcanceGestionUsuariosTests.cs`, `tests/Restaurante.WebApi.Tests/Errores/` (mapeo 403)

**Interfaces:**
- Produce: `sealed record ActorUsuarios(Guid Id, Rol Rol, Guid? SucursalPropia, bool SucursalPropiaActiva)`.
- Produce: `static class AlcanceGestionUsuarios { bool PuedeVer(ActorUsuarios a, Usuario u); bool PuedeGestionar(ActorUsuarios a, Usuario u); bool PuedeAsignar(ActorUsuarios a, Rol rol, IReadOnlyCollection<Guid> sucursales); bool PuedeOperar(ActorUsuarios a); }` con las reglas de `diseno.md`: general todo; de sucursal con su sucursal activa ve a quien la tenga asignada salvo administradores generales, gestiona sólo cajero/cocinero cuya única sucursal es la suya y nunca a sí mismo, asigna sólo cajero/cocinero con `[SucursalPropia]`; con su sucursal inactiva `PuedeOperar == false`; otros roles `PuedeOperar == false`.
- Produce: `sealed class ProhibidoException(string mensaje) : Exception`.

- [ ] **Paso 1: Pruebas que fallan** (tabla de casos, cada uno con su RF):
  - `"004/RF-07: el administrador de sucursal ve a quien tiene su sucursal, sin administradores generales"`.
  - `"004/RF-08: el administrador de sucursal sólo asigna cajero o cocinero con su sucursal"` (rol `administrador_sucursal`, sucursal ajena o dos sucursales → `false`).
  - `"004/RF-09: el administrador de sucursal gestiona al cajero exclusivo de su sucursal"`.
  - `"004/RF-10: no gestiona al personal compartido, a otro administrador ni a sí mismo"` (Foco 2).
  - `"004/RF-11: cajero y cocinero no operan sobre usuarios"`.
  - `"004/RF-26: con su sucursal inactiva el administrador de sucursal no opera"`.
  - `"004/RF-10: ProhibidoException se traduce en 403 Acceso denegado"` (WebApi).
- [ ] **Paso 2:** filtro `DisplayName~004/` en Application y WebApi → no compila.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): alcance de gestión de usuarios (T-03)"`.

### T-04 [RF-12, RF-13, RF-14]: Sello en el token y middleware de sesión

**Archivos:**
- Modificar: `apps/api/src/Restaurante.Infrastructure/Seguridad/EmisorTokensJwt.cs` (claim `sello`)
- Crear: `apps/api/src/Restaurante.WebApi/Seguridad/MiddlewareSesionUsuario.cs`
- Modificar: `apps/api/src/Restaurante.Application/Abstracciones/IUsuarioActual.cs`, `Restaurante.WebApi/Seguridad/UsuarioActual.cs`, `Restaurante.WebApi/Program.cs` (tras `UseAuthentication`, antes del contexto de restaurante), `DependencyInjection.cs`; doble `UsuarioActualFalso`
- Docs: `docs/context/architecture-map.md` (orden del pipeline), glosarios ("Sello de sesión")
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Seguridad/SesionUsuarioTests.cs`, `tests/Restaurante.Infrastructure.Tests/Seguridad/` (claim)

**Interfaces:**
- Consume: `IUsuarioRepository.ObtenerParaSesionAsync` (T-02).
- Produce: `interface IUsuarioActual { Guid Id { get; } Rol Rol { get; } IReadOnlyCollection<Guid> Sucursales { get; } }` con los datos **de base de datos** que fija el middleware (no los del token). `UsuarioActual` pasa a ser un contenedor por petición con `void Establecer(Usuario usuario)`.
- Produce: `MiddlewareSesionUsuario` con `const string TituloSesionInvalida = "Sesión no válida"` y `DetalleSesionInvalida = "Vuelve a iniciar sesión."`.

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-13: el token incluye el sello del usuario"` (Infrastructure).
  - `"004/RF-12: un usuario desactivado no inicia sesión"` (401 con el mensaje genérico; ya existe: se renombra o se añade con el prefijo 004).
  - `"004/RF-13: tras desactivar, el token anterior responde 401 Sesión no válida"`; `"004/RF-13: tras cambiar el sello, un token nuevo funciona"` (modificar el usuario en el doble en memoria).
  - `"004/RF-14: un token sin sello responde 401 Sesión no válida"` (Foco 3) — firmar a mano un token sin el claim con la clave de `FabricaApi`.
  - `"004/RF-14: un token de un usuario inexistente responde 401"`; `"004/RF-14: también en /api/plataforma"`.
  - `"004/RF-14: el 401 anuncia WWW-Authenticate Bearer"`.
- [ ] **Paso 2:** filtro `DisplayName~004/` → FAIL.
- [ ] **Paso 3:** implementar; el middleware sólo actúa con `User.Identity.IsAuthenticated`.
- [ ] **Paso 4:** filtro → PASS; `comando_test` → PASS (las pruebas existentes que emiten tokens deben seguir pasando: `FabricaApi` emite con el emisor real).
- [ ] **Paso 5:** `git commit -m "feat(004): sello en el token y middleware de sesión (T-04)"`.

### T-05 [RF-01, RF-02, RF-03, RF-07, RF-08, RF-11]: Alta y lista de usuarios con sucursales

**Archivos:**
- Modificar: `apps/api/src/Restaurante.Application/Features/Usuarios/CrearUsuario/*`, `ListarUsuarios/*`, `UsuarioDto.cs`
- Crear: `apps/api/src/Restaurante.Application/Features/Usuarios/ResolutorActor.cs`, `SucursalAsignadaDto.cs`
- Modificar: `apps/api/src/Restaurante.WebApi/Controllers/UsuariosController.cs` (`[RequiereRol(AdministradorGeneral, AdministradorSucursal)]`, `CrearUsuarioPeticion` con `string[]? Sucursales`)
- Docs: `docs/context/http/usuarios.http`, glosarios
- Test: `tests/Restaurante.Application.Tests/Features/Usuarios/`, `tests/Restaurante.WebApi.Tests/Usuarios/UsuariosSucursalesTests.cs`

**Interfaces:**
- Consume: `AlcanceGestionUsuarios`, `ActorUsuarios`, `ProhibidoException` (T-03); `IUsuarioActual` (T-04).
- Produce: `sealed record SucursalAsignadaDto(string Slug, string Nombre, bool Activa)`; `UsuarioDto(Guid Id, string NombreUsuario, string Rol, bool Activo, IReadOnlyList<SucursalAsignadaDto> Sucursales, bool Gestionable)` con sucursales ordenadas por nombre.
- Produce: `sealed class ResolutorActor(IUsuarioActual, ISucursalRepository) { Task<ActorUsuarios> ObtenerAsync(CancellationToken ct); }` (registrado en DI; lanza `ProhibidoException` si `!PuedeOperar`) y `static Task<IReadOnlyList<Guid>> ResolverSlugsAsync(...)` (o método del resolutor) que convierte slugs → ids y lanza `ValidationException` en `sucursales` si alguno no existe.
- `CrearUsuarioCommand(string NombreUsuario, string Contrasena, string Rol, IReadOnlyList<string> Sucursales)`; el validador añade la cardinalidad (RF-02) y duplicados.

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-01: el administrador general crea un cajero con dos sucursales"` → 201 con `sucursales` `[{slug,nombre,activa}]` y `gestionable: true`.
  - `"004/RF-02: alta con cardinalidad inválida responde 400 en sucursales"` (Theory de los cuatro casos inválidos) y `"004/RF-02: alta con una sucursal inexistente responde 400 en sucursales"`.
  - `"004/RF-03: la lista trae todos los usuarios con sus sucursales, ordenada por nombre"`.
  - `"004/RF-07: el administrador de sucursal sólo ve a su personal, compartido sin gestionable"` (Foco 2).
  - `"004/RF-08: el administrador de sucursal crea un cocinero en su sucursal"` (201) y `"… con otra sucursal o con rol administrador responde 403"`, incluida una sucursal inexistente (403, no 400).
  - `"004/RF-11: cajero y cocinero reciben 403 en /usuarios"`.
  - Aislamiento: `"004/RF-03: un administrador del restaurante A no ve usuarios del B"` (RNF-02).
- [ ] **Paso 2:** filtro `DisplayName~004/` → FAIL.
- [ ] **Paso 3:** implementar.
- [ ] **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): alta y lista de usuarios con sucursales y alcance (T-05)"`.

### T-06 [RF-04, RF-05, RF-09, RF-10, RF-13]: Editar rol y sucursales, y activar o desactivar

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Usuarios/EditarUsuario/{EditarUsuarioCommand,EditarUsuarioValidator,EditarUsuarioHandler}.cs`
- Eliminar: `apps/api/src/Restaurante.Application/Features/Usuarios/CambiarRolUsuario/` y la acción `PUT {id}/rol` (y sus pruebas de 002, sustituidas por las de esta tarea)
- Modificar: `CambiarEstadoUsuario/CambiarEstadoUsuarioHandler.cs` (alcance), `UsuariosController.cs` (`PUT {id}` con `EditarUsuarioPeticion(string? Rol, string[]? Sucursales)`)
- Docs: `usuarios.http`, glosarios
- Test: `tests/Restaurante.Application.Tests/Features/Usuarios/`, `tests/Restaurante.WebApi.Tests/Usuarios/`

**Interfaces:**
- Consume: `ResolutorActor`, `UsuarioDto` (T-05); `Usuario.AsignarRolYSucursales` (T-01).
- Produce: `EditarUsuarioCommand(Guid Id, string Rol, IReadOnlyList<string> Sucursales)` → `UsuarioDto`.
- Reglas 409 (RF-05): quitarse a sí mismo el rol de administrador general; dejar sin administradores generales activos; desactivarse a sí mismo (mensajes de 002).

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-04: el administrador general cambia rol y sucursales"` y `"004/RF-04: el nombre de usuario no cambia"` (un `nombreUsuario` en el cuerpo se ignora).
  - `"004/RF-05: quitar el rol al último administrador general responde 409"`, `"… quitarse a sí mismo el rol responde 409"`, `"… desactivarse a sí mismo responde 409"`.
  - `"004/RF-09: el administrador de sucursal edita y desactiva a su cajero exclusivo"`.
  - `"004/RF-10: el administrador de sucursal recibe 403 con personal compartido, otro administrador o él mismo, y nada cambia"`.
  - `"004/RF-13: editar sin cambios no invalida el token del usuario editado"` y `"… cambiar sus sucursales sí"` (WebApi, con dos tokens).
  - `"004/RF-04: editar un usuario de otro restaurante responde 404"` (RNF-02).
- [ ] **Paso 2:** filtro → FAIL.
- [ ] **Paso 3:** implementar y eliminar `CambiarRolUsuario`.
- [ ] **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): editar rol y sucursales con alcance (T-06)"`.

### T-07 [RF-06, RF-13]: Asignar contraseña nueva

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Usuarios/CambiarContrasenaUsuario/{Command,Validator,Handler}.cs`
- Modificar: `UsuariosController.cs` (`PUT {id}/contrasena` con `CambiarContrasenaPeticion(string? Contrasena)`, 204)
- Docs: `usuarios.http`
- Test: Application y WebApi `Usuarios/`

**Interfaces:**
- Consume: `ResolutorActor` (T-05), `Usuario.CambiarContrasena` (T-01), `IHasherContrasenas`.
- Produce: `CambiarContrasenaUsuarioCommand(Guid Id, string Contrasena)` → `Unit`. Longitud mínima `CrearUsuarioValidator.LongitudMinimaContrasena` (8).

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-06: el administrador general asigna una contraseña nueva y el usuario entra con ella"` (204; login con la nueva 200 y con la vieja 401).
  - `"004/RF-06: una contraseña de 7 caracteres responde 400"`.
  - `"004/RF-13: asignar contraseña invalida el token anterior del usuario"`.
  - `"004/RF-06: el administrador general puede asignarse contraseña y su token anterior deja de valer"` (Foco 1).
  - `"004/RF-10: el administrador de sucursal recibe 403 al cambiar la contraseña de personal compartido"`.
- [ ] **Paso 2:** filtro → FAIL. **Paso 3:** implementar. **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): asignar contraseña nueva (T-07)"`.

### T-08 [RF-15, RF-25]: Acceso sólo a sucursales asignadas y activas

**Archivos:**
- Modificar: `apps/api/src/Restaurante.WebApi/Seguridad/MiddlewareContextoSucursal.cs`
- Test: `apps/api/tests/Restaurante.WebApi.Tests/Sucursales/AccesoSucursalTests.cs`

**Interfaces:**
- Consume: `IUsuarioActual` (T-04). Si hay usuario autenticado con rol distinto de `AdministradorGeneral` y la sucursal resuelta no está en `Sucursales` o no está `Activa` → 404 `Sucursal no disponible`. Sin autenticar: sin cambios.
- Nota: los endpoints actuales bajo `/s/` son sólo de `administrador_general`; las pruebas usan un controlador de prueba en `ControllersDePrueba.cs` permitido a cajero para observar el 200/404.

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-15: un cajero accede a su sucursal"` (200) y `"004/RF-15: un cajero recibe 404 en una sucursal no asignada"`.
  - `"004/RF-25: un cajero recibe 404 en su sucursal desactivada"`.
  - `"004/RF-15: el administrador general accede a cualquier sucursal"`; `"004/RF-15: el estado público no cambia"`.
- [ ] **Paso 2:** filtro → FAIL. **Paso 3:** implementar. **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): acceso sólo a sucursales asignadas y activas (T-08)"`.

### T-09 [RF-16, RF-17]: Mi sesión y login sin sucursales activas

**Archivos:**
- Crear: `apps/api/src/Restaurante.Application/Features/Auth/MiSesion/{ObtenerMiSesionQuery,ObtenerMiSesionHandler,MiSesionDto}.cs`, `apps/api/src/Restaurante.WebApi/Controllers/MiSesionController.cs` (`GET /api/r/{slug}/mi-sesion`, roles de restaurante)
- Crear: `apps/api/src/Restaurante.Application/Comun/Excepciones/SinSucursalesActivasException.cs` → 403 `Sin sucursales activas` en `FiltroExcepciones`
- Modificar: `Features/Auth/IniciarSesion/IniciarSesionHandler.cs` (tras validar credenciales y estado)
- Docs: `docs/context/http/auth.http`, glosarios
- Test: Application `Auth/`, WebApi `Auth/MiSesionTests.cs`

**Interfaces:**
- Produce: `MiSesionDto(string NombreUsuario, string Rol, IReadOnlyList<SucursalDeSesionDto> Sucursales)`; `SucursalDeSesionDto(string Slug, string Nombre)` ordenadas por nombre; general: todas las activas; resto: asignadas activas.

- [ ] **Paso 1: Pruebas que fallan**
  - `"004/RF-16: mi-sesion del cajero trae sólo sus sucursales activas ordenadas por nombre"`.
  - `"004/RF-16: mi-sesion del administrador general trae todas las sucursales activas"`.
  - `"004/RF-17: un cajero con todas sus sucursales inactivas recibe 403 Sin sucursales activas y ningún token"`.
  - `"004/RF-17: con contraseña incorrecta sigue respondiendo 401"` (el 403 no adelanta información).
- [ ] **Paso 2:** filtro → FAIL. **Paso 3:** implementar. **Paso 4:** filtro → PASS; `comando_test` → PASS.
- [ ] **Paso 5:** `git commit -m "feat(004): mi sesión y login sin sucursales activas (T-09)"`.

### T-10 [RF-18, RF-19, RF-22]: Pantalla de usuarios
UI: sí

**Archivos:**
- Crear: `apps/web/src/usuarios/{api.ts,UsuariosPagina.tsx,FormularioUsuario.tsx,DialogoContrasena.tsx}` (+ `.module.css` y `.test.tsx`)
- Modificar: `apps/web/src/App.tsx` (ruta `usuarios` bajo `/r/:slug`), `apps/web/src/restaurante/InicioRestaurante.tsx` (enlace "Usuarios" para ambos administradores)
- Crear: `apps/web/e2e/apiSimuladaUsuarios.ts`, `apps/web/e2e/usuarios.spec.ts`

**Interfaces:**
- Consume: API de T-05–T-07; `CampoFormulario`, `DialogoConfirmacion`, `MarcoRestaurante`, `RutaProtegida` (`roles={['administrador_general','administrador_sucursal']}`, `redirigirSinRol` al inicio).
- Produce en `usuarios/api.ts`: tipos `Usuario`, `SucursalAsignada`; `listarUsuarios`, `crearUsuario`, `editarUsuario`, `cambiarContrasena`, `cambiarEstadoUsuario`; `claveUsuarios(slug)`.
- Las sucursales del formulario salen de `mi-sesion` (T-09); para el administrador de sucursal, roles `cajero`/`cocinero` y su sucursal marcada y deshabilitada.

- [ ] **Paso 1: Prueba Playwright en rojo** (`apps/web/e2e/usuarios.spec.ts`, API simulada):
  - `"004/RF-18: el administrador general crea un cajero con dos sucursales y lo ve en la lista"`.
  - `"004/RF-18: desactivar pide confirmación; asignar contraseña envía la nueva"`.
  - `"004/RF-18: el personal no gestionable se muestra sin acciones"`.
  - `"004/RF-19: el administrador de sucursal sólo ofrece cajero y cocinero con su sucursal fija"`.
  - `"004/RF-22: el inicio muestra Usuarios a los administradores y un cajero que abre /usuarios vuelve al inicio"`.
  - Comprobar que fallan y hacer commit de las pruebas solas.
- [ ] **Paso 2:** Ralph Loop (`/implementar`) con Vitest de `FormularioUsuario` (roles y sucursales por rol, errores por campo) y de `UsuariosPagina` (403 → "No tienes permiso para esta acción.").
- [ ] **Paso 3:** `completar-tarea.py T-10`.

### T-11 [RF-20, RF-21, RF-24, RF-27]: Elegir sucursal al entrar y selector para todos
UI: sí

**Archivos:**
- Crear: `apps/web/src/sucursales/ElegirSucursalPagina.tsx` (+ css y test), `apps/web/src/sesion/miSesion.ts` (`obtenerMiSesion`, `claveMiSesion`)
- Modificar: `apps/web/src/restaurante/LoginRestaurante.tsx`, `apps/web/src/sucursales/SelectorSucursal.tsx`, `apps/web/src/restaurante/MarcoRestaurante.tsx`, `apps/web/src/restaurante/InicioRestaurante.tsx`, `apps/web/src/App.tsx` (ruta `elegir-sucursal`)
- Crear: `apps/web/e2e/eleccionSucursal.spec.ts` (ampliar `apiSimulada.ts` con `mi-sesion`)

**Interfaces:**
- Consume: `GET /api/r/{slug}/mi-sesion` (T-09).
- Login: tras 200 consulta `mi-sesion`; rol ≠ general con ≥ 2 sucursales → `/r/{slug}/elegir-sucursal`; con 1 → `elegirSucursal(slug)` e inicio; 403 `Sin sucursales activas` → mensaje de RF-24.
- `SelectorSucursal` usa `mi-sesion` para todos los roles: ≥ 2 → `<select>`; 1 → nombre; 0 → nada. Navega a `/r/{slug}/sucursales/{s}` sólo para el general; para el resto, a `/r/{slug}/inicio`. Descarta una `sucursalActiva` que ya no esté en la lista (Foco 5).
- El inicio, con 0 sucursales y rol ≠ general, muestra el aviso de RF-27.

- [ ] **Paso 1: Prueba Playwright en rojo**:
  - `"004/RF-20: un cajero con dos sucursales elige una al iniciar sesión"`; `"004/RF-20: con una sola entra directo al inicio"`.
  - `"004/RF-21: el selector muestra las sucursales del cajero y con una sola sólo su nombre"`.
  - `"004/RF-24: el login muestra el aviso sin sucursales activas"`.
  - `"004/RF-27: con la sesión abierta y sin sucursales activas el inicio muestra el aviso"`.
  - Las pruebas de 003 del selector del administrador general deben seguir pasando sin modificarse.
  - Commit de las pruebas solas.
- [ ] **Paso 2:** Ralph Loop con Vitest de `ElegirSucursalPagina` y del selector.
- [ ] **Paso 3:** `completar-tarea.py T-11`.

### T-12 [RF-23]: Sesión terminada vuelve al login con aviso
UI: sí

**Archivos:**
- Modificar: `apps/web/src/api/cliente.ts` (aviso global de 401 `Sesión no válida`), `apps/web/src/sesion/ProveedorSesion.tsx` (suscripción: cerrar sesión y navegar), `apps/web/src/restaurante/LoginRestaurante.tsx` (muestra el aviso)
- Crear: `apps/web/e2e/sesionTerminada.spec.ts`

**Interfaces:**
- Produce en `cliente.ts`: `alSesionInvalida(manejador: () => void): () => void` (suscripción); `pedirApi` lo dispara cuando una petición **con token** recibe 401 con título `Sesión no válida`, antes de lanzar el `ErrorApi`.
- El aviso viaja en el estado de navegación (`{ sesionTerminada: true }`) o en un parámetro de consulta; el login muestra `Tu sesión terminó. Vuelve a iniciar sesión.`.

- [ ] **Paso 1: Prueba Playwright en rojo**:
  - `"004/RF-23: una petición con 401 Sesión no válida lleva al login con el aviso"`.
  - `"004/RF-23: tras asignarse contraseña, el administrador general vuelve al login con el aviso"` (Foco 1).
  - Commit de las pruebas solas.
- [ ] **Paso 2:** Ralph Loop con Vitest de `cliente.ts` (401 con otro título no dispara el aviso).
- [ ] **Paso 3:** `completar-tarea.py T-12`.

### T-13 [RF-01, RF-13, RF-20]: Prueba de punta a punta de usuarios

**Archivos:**
- Crear: `apps/web/e2e-stack/usuarios.spec.ts`
- Modificar si hace falta: `docs/ONBOARDING.md` (asignar sucursales tras la migración, RNF-04)

**Interfaces:**
- Consume: sistema levantado con `docker compose up` (API, web, PostgreSQL) y las credenciales `ADMIN_*` de `.env`, como `e2e-stack/sucursales.spec.ts`.

- [ ] **Paso 1:** escribir `"004/RF-01: el administrador general crea un cajero con dos sucursales"`, `"004/RF-20: el cajero inicia sesión y elige sucursal"` y `"004/RF-13: al desactivar al cajero, su sesión vuelve al login"` (dos contextos de navegador); datos con sufijo aleatorio para poder repetir.
- [ ] **Paso 2:** con el sistema levantado, `cd apps/web && npx playwright test e2e-stack/usuarios.spec.ts` → PASS en ambos proyectos.
- [ ] **Paso 3:** `comando_test` → PASS; commit `test(004): prueba de punta a punta de usuarios (T-13)`.
