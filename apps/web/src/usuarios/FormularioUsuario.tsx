import { useState, type ChangeEvent, type FormEvent } from 'react';
import type { Rol } from '../api/auth';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import { useSesion } from '../sesion/ProveedorSesion';
import type { SucursalDeSesion } from '../sesion/miSesion';
import { crearUsuario, editarUsuario, NOMBRES_ROL, type RolRestaurante, type Usuario } from './api';
import estilos from './FormularioUsuario.module.css';

type Props = {
  slugRestaurante: string;
  /** Rol de quien usa el formulario: el administrador de sucursal sólo ofrece cajero/cocinero y su sucursal. */
  rolActual: Rol;
  /** Sucursales con las que trabaja quien usa el formulario (`mi-sesion`). */
  sucursalesDisponibles: SucursalDeSesion[];
  usuario?: Usuario;
  onGuardado(): void;
  onCancelar(): void;
};

const ROLES_GENERAL: RolRestaurante[] = ['administrador_general', 'administrador_sucursal', 'cajero', 'cocinero'];
const ROLES_SUCURSAL: RolRestaurante[] = ['cajero', 'cocinero'];

export const MENSAJE_SIN_PERMISO = 'No tienes permiso para esta acción.';

export function FormularioUsuario({
  slugRestaurante,
  rolActual,
  sucursalesDisponibles,
  usuario,
  onGuardado,
  onCancelar,
}: Props) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const esAlta = !usuario;
  const esAdminSucursal = rolActual === 'administrador_sucursal';
  const roles = esAdminSucursal ? ROLES_SUCURSAL : ROLES_GENERAL;
  const sucursalesFijas = esAdminSucursal ? sucursalesDisponibles.map((s) => s.slug) : null;

  // Además de las sucursales de la sesión, se muestran las ya asignadas (p. ej. una sucursal inactiva).
  const opcionesSucursal = [
    ...sucursalesDisponibles,
    ...(usuario?.sucursales ?? []).filter((a) => !sucursalesDisponibles.some((s) => s.slug === a.slug)),
  ];

  const [nombreUsuario, setNombreUsuario] = useState(usuario?.nombreUsuario ?? '');
  const [contrasena, setContrasena] = useState('');
  const [rol, setRol] = useState<RolRestaurante>(
    usuario && roles.includes(usuario.rol as RolRestaurante) ? (usuario.rol as RolRestaurante) : 'cajero',
  );
  const [sucursalesMarcadas, setSucursales] = useState<string[]>(usuario?.sucursales.map((s) => s.slug) ?? []);
  // Las sucursales fijas salen de las props en cada render (no del estado inicial): si `mi-sesion`
  // llega después de montar el formulario, se muestran y se envían igualmente.
  const sucursales = sucursalesFijas ?? sucursalesMarcadas;
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const titulo = esAlta ? 'Nuevo usuario' : `Editar ${usuario.nombreUsuario}`;
  const conSucursales = rol !== 'administrador_general';

  function alternar(slug: string, marcada: boolean) {
    setSucursales((actuales) => (marcada ? [...actuales, slug] : actuales.filter((s) => s !== slug)));
  }

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrores({});
    setErrorGeneral(null);
    setGuardando(true);
    // Se envían en el orden de la lista para que la petición no dependa del orden de los clics.
    const elegidas = conSucursales ? opcionesSucursal.map((s) => s.slug).filter((s) => sucursales.includes(s)) : [];
    try {
      if (esAlta) {
        await crearUsuario(token, slugRestaurante, { nombreUsuario, contrasena, rol, sucursales: elegidas });
      } else {
        await editarUsuario(token, slugRestaurante, usuario.id, { rol, sucursales: elegidas });
      }
      onGuardado();
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 400 && Object.keys(e.errores).length > 0) {
        setErrores(normalizar(e.errores));
      } else if (e instanceof ErrorApi && e.estado === 403) {
        setErrorGeneral(MENSAJE_SIN_PERMISO);
      } else {
        setErrorGeneral(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo guardar. Inténtalo de nuevo.');
      }
    } finally {
      setGuardando(false);
    }
  }

  return (
    <form className={estilos.formulario} aria-label={titulo} onSubmit={guardar} noValidate>
      <h2 className={estilos.titulo}>{titulo}</h2>
      {errorGeneral && (
        <p role="alert" className={estilos.alerta}>
          {errorGeneral}
        </p>
      )}
      <div className={estilos.campos}>
        <CampoFormulario
          etiqueta="Usuario"
          errores={errores.nombreUsuario}
          autoComplete="off"
          value={nombreUsuario}
          disabled={!esAlta}
          onChange={(e) => setNombreUsuario(e.target.value)}
        />
        {esAlta && (
          <CampoFormulario
            etiqueta="Contraseña"
            errores={errores.contrasena}
            type="password"
            autoComplete="new-password"
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
          />
        )}
        <CampoFormulario
          etiqueta="Rol"
          errores={errores.rol}
          opciones={roles.map((r) => ({ valor: r, etiqueta: NOMBRES_ROL[r] }))}
          value={rol}
          onChange={(e: ChangeEvent<HTMLSelectElement>) => setRol(e.target.value as RolRestaurante)}
        />
      </div>
      {conSucursales && (
        <fieldset className={estilos.sucursales} aria-describedby={errores.sucursales ? 'errores-sucursales' : undefined}>
          <legend>Sucursales</legend>
          {opcionesSucursal.map((s) => (
            <label key={s.slug} className={estilos.casilla}>
              <input
                type="checkbox"
                checked={sucursales.includes(s.slug)}
                disabled={sucursalesFijas !== null}
                onChange={(e) => alternar(s.slug, e.target.checked)}
              />
              {s.nombre}
            </label>
          ))}
          {errores.sucursales && (
            <span id="errores-sucursales" className={estilos.error}>
              {errores.sucursales.join(' ')}
            </span>
          )}
        </fieldset>
      )}
      <div className={estilos.acciones}>
        <button type="button" className={estilos.secundario} onClick={onCancelar}>
          Cancelar
        </button>
        <button type="submit" className={estilos.principal} disabled={guardando}>
          Guardar
        </button>
      </div>
    </form>
  );
}

/** Las claves de error del servidor pueden venir en PascalCase (`Sucursales`); se pasan a camelCase. */
function normalizar(errores: Record<string, string[]>): Record<string, string[]> {
  return Object.fromEntries(Object.entries(errores).map(([k, v]) => [k.charAt(0).toLowerCase() + k.slice(1), v]));
}
