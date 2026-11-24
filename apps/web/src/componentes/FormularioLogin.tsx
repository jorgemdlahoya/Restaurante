import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import estilos from './FormularioLogin.module.css';

type Props = { titulo: string; onEnviar(nombreUsuario: string, contrasena: string): Promise<void> };

export function FormularioLogin({ titulo, onEnviar }: Props) {
  const [nombreUsuario, setNombreUsuario] = useState('');
  const [contrasena, setContrasena] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [enviando, setEnviando] = useState(false);

  async function enviar(evento: FormEvent) {
    evento.preventDefault();
    setError(null);
    setEnviando(true);
    try {
      await onEnviar(nombreUsuario, contrasena);
    } catch (e) {
      setError(e instanceof ErrorApi ? (e.detalle ?? e.titulo) : 'No se pudo iniciar sesión. Inténtalo de nuevo.');
    } finally {
      setEnviando(false);
    }
  }

  return (
    <form className={estilos.formulario} onSubmit={enviar} aria-label={titulo}>
      <label className={estilos.campo}>
        Usuario
        <input
          autoComplete="username"
          value={nombreUsuario}
          onChange={(e) => setNombreUsuario(e.target.value)}
          required
        />
      </label>
      <label className={estilos.campo}>
        Contraseña
        <input
          type="password"
          autoComplete="current-password"
          value={contrasena}
          onChange={(e) => setContrasena(e.target.value)}
          required
        />
      </label>
      {error && (
        <p role="alert" className={estilos.error}>
          {error}
        </p>
      )}
      <button type="submit" className={estilos.boton} disabled={enviando}>
        Iniciar sesión
      </button>
    </form>
  );
}
