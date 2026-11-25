import { useEffect, useId, useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import estilos from '../componentes/DialogoConfirmacion.module.css';
import { useSesion } from '../sesion/ProveedorSesion';
import { cambiarContrasena, type Usuario } from './api';
import { MENSAJE_SIN_PERMISO } from './FormularioUsuario';

type Props = { slugRestaurante: string; usuario: Usuario; onAsignada(): void; onCancelar(): void };

/** Asigna una contraseña nueva a un usuario (RF-06); hacerlo corta su sesión abierta. */
export function DialogoContrasena({ slugRestaurante, usuario, onAsignada, onCancelar }: Props) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const idTitulo = useId();
  const [contrasena, setContrasena] = useState('');
  const [errores, setErrores] = useState<string[] | undefined>();
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  useEffect(() => {
    const alPulsar = (e: KeyboardEvent) => {
      if (e.key === 'Escape') onCancelar();
    };
    document.addEventListener('keydown', alPulsar);
    return () => document.removeEventListener('keydown', alPulsar);
  }, [onCancelar]);

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrores(undefined);
    setErrorGeneral(null);
    setGuardando(true);
    try {
      await cambiarContrasena(token, slugRestaurante, usuario.id, contrasena);
      onAsignada();
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 400) {
        setErrores(e.errores.contrasena ?? e.errores.Contrasena ?? [e.detalle ?? e.titulo]);
      } else if (e instanceof ErrorApi && e.estado === 403) {
        setErrorGeneral(MENSAJE_SIN_PERMISO);
      } else {
        setErrorGeneral('No se pudo asignar la contraseña. Inténtalo de nuevo.');
      }
    } finally {
      setGuardando(false);
    }
  }

  return (
    <div className={estilos.fondo}>
      <div role="dialog" aria-modal="true" aria-labelledby={idTitulo} className={estilos.dialogo}>
        <h2 id={idTitulo} className={estilos.titulo}>
          Asignar contraseña a {usuario.nombreUsuario}
        </h2>
        <form onSubmit={guardar} noValidate>
          <p>Su sesión abierta se cerrará y tendrá que entrar con la contraseña nueva.</p>
          {errorGeneral && <p role="alert">{errorGeneral}</p>}
          <CampoFormulario
            etiqueta="Contraseña nueva"
            type="password"
            autoComplete="new-password"
            autoFocus
            errores={errores}
            value={contrasena}
            onChange={(e) => setContrasena(e.target.value)}
          />
          <div className={estilos.acciones}>
            <button type="button" className={estilos.secundario} onClick={onCancelar}>
              Cancelar
            </button>
            <button type="submit" className={estilos.peligro} disabled={guardando}>
              Guardar
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
