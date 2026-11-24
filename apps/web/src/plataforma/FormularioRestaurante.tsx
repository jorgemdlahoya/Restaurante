import { useId, useMemo, useState, type FormEvent, type InputHTMLAttributes } from 'react';
import { ErrorApi } from '../api/cliente';
import { useSesion } from '../sesion/ProveedorSesion';
import { resolverPaleta, type Paleta } from '../tema/paleta';
import { crearRestaurante, editarRestaurante, subirLogo, type Restaurante } from './api';
import estilos from './FormularioRestaurante.module.css';
import { VistaPreviaPaleta } from './VistaPreviaPaleta';

type Props = { restaurante?: Restaurante; onGuardado(): void; onCancelar(): void };

type Errores = Record<string, string[]>;

const COLORES: { clave: keyof Paleta; etiqueta: string }[] = [
  { clave: 'principal', etiqueta: 'Color principal' },
  { clave: 'acento', etiqueta: 'Color de acento' },
  { clave: 'oscuro', etiqueta: 'Color oscuro' },
  { clave: 'fondo', etiqueta: 'Color de fondo' },
  { clave: 'texto', etiqueta: 'Color de texto' },
];

// <input type="color"> sólo admite #rrggbb en minúsculas.
function paletaInicial(restaurante?: Restaurante): Paleta {
  const p = resolverPaleta(restaurante?.paleta);
  return Object.fromEntries(Object.entries(p).map(([k, v]) => [k, v.toLowerCase()])) as Paleta;
}

export function FormularioRestaurante({ restaurante, onGuardado, onCancelar }: Props) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const esAlta = !restaurante;

  const [nombre, setNombre] = useState(restaurante?.nombre ?? '');
  const [slug, setSlug] = useState(restaurante?.slug ?? '');
  const [paleta, setPaleta] = useState<Paleta>(() => paletaInicial(restaurante));
  const [logo, setLogo] = useState<File | null>(null);
  const [adminUsuario, setAdminUsuario] = useState('');
  const [adminContrasena, setAdminContrasena] = useState('');
  const [errores, setErrores] = useState<Errores>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);
  // Si el alta se guardó pero el logo falló, los reintentos editan el restaurante ya creado.
  const [idCreado, setIdCreado] = useState<string | null>(null);

  const vistaPrevia = useMemo(() => ({ ...paleta }), [paleta]);
  const titulo = esAlta ? 'Nuevo restaurante' : `Editar ${restaurante.nombre}`;

  function registrarError(e: unknown, campoPorDefecto?: string) {
    if (e instanceof ErrorApi && e.estado === 400 && Object.keys(e.errores).length > 0) {
      setErrores(e.errores);
    } else if (e instanceof ErrorApi && campoPorDefecto && e.estado === 400) {
      setErrores({ [campoPorDefecto]: [e.detalle ?? e.titulo] });
    } else {
      setErrorGeneral(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo guardar. Inténtalo de nuevo.');
    }
  }

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrores({});
    setErrorGeneral(null);
    setGuardando(true);

    let id = restaurante?.id ?? idCreado;
    try {
      if (id) {
        await editarRestaurante(token, id, { nombre, paleta });
      } else {
        const creado = await crearRestaurante(token, {
          nombre,
          slug,
          paleta,
          administrador: { nombreUsuario: adminUsuario, contrasena: adminContrasena },
        });
        id = creado.id;
        setIdCreado(creado.id);
      }
    } catch (e) {
      registrarError(e);
      setGuardando(false);
      return;
    }

    if (logo) {
      try {
        await subirLogo(token, id, logo);
      } catch (e) {
        registrarError(e, 'logo');
        setGuardando(false);
        return;
      }
    }

    setGuardando(false);
    onGuardado();
  }

  return (
    <form className={estilos.formulario} aria-label={titulo} onSubmit={guardar} noValidate>
      <h2 className={estilos.titulo}>{titulo}</h2>

      {errorGeneral && (
        <p role="alert" className={estilos.alerta}>
          {errorGeneral}
        </p>
      )}

      <div className={estilos.columnas}>
        <fieldset className={estilos.grupo}>
          <legend>Datos</legend>
          <Campo etiqueta="Nombre" errores={errores.nombre} value={nombre} onChange={(e) => setNombre(e.target.value)} />
          <Campo
            etiqueta="Slug"
            errores={errores.slug}
            value={slug}
            disabled={!esAlta || idCreado !== null}
            onChange={(e) => setSlug(e.target.value)}
          />
          <Campo
            etiqueta="Logo"
            errores={errores.logo}
            type="file"
            accept="image/png,image/jpeg,image/webp"
            onChange={(e) => setLogo(e.target.files?.[0] ?? null)}
          />
          {esAlta && (
            <>
              <Campo
                etiqueta="Usuario administrador"
                errores={errores['administrador.nombreUsuario']}
                autoComplete="off"
                value={adminUsuario}
                onChange={(e) => setAdminUsuario(e.target.value)}
              />
              <Campo
                etiqueta="Contraseña del administrador"
                errores={errores['administrador.contrasena']}
                type="password"
                autoComplete="new-password"
                value={adminContrasena}
                onChange={(e) => setAdminContrasena(e.target.value)}
              />
            </>
          )}
        </fieldset>

        <fieldset className={estilos.grupo}>
          <legend>Colores</legend>
          <div className={estilos.colores}>
            {COLORES.map(({ clave, etiqueta }) => (
              <Campo
                key={clave}
                etiqueta={etiqueta}
                errores={errores[`paleta.${clave}`]}
                type="color"
                value={paleta[clave]}
                onChange={(e) => setPaleta((p) => ({ ...p, [clave]: e.target.value }))}
              />
            ))}
          </div>
          <VistaPreviaPaleta paleta={vistaPrevia} />
        </fieldset>
      </div>

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

type PropsCampo = InputHTMLAttributes<HTMLInputElement> & { etiqueta: string; errores?: string[] };

/** Campo con etiqueta y, si hay errores del servidor, mensaje asociado por aria-describedby. */
function Campo({ etiqueta, errores, ...input }: PropsCampo) {
  const idCampo = useId();
  const idError = useId();
  const conError = !!errores?.length;
  return (
    <div className={estilos.campo}>
      <label htmlFor={idCampo}>{etiqueta}</label>
      <input
        {...input}
        id={idCampo}
        aria-invalid={conError || undefined}
        aria-describedby={conError ? idError : undefined}
      />
      {conError && (
        <span id={idError} className={estilos.error}>
          {errores.join(' ')}
        </span>
      )}
    </div>
  );
}
