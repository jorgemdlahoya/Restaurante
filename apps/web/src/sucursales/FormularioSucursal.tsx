import { useState, type FormEvent } from 'react';
import { ErrorApi } from '../api/cliente';
import { CampoFormulario } from '../componentes/CampoFormulario';
import { useSesion } from '../sesion/ProveedorSesion';
import { crearSucursal, editarSucursal, ZONA_POR_DEFECTO, ZONAS_HORARIAS, type Sucursal } from './api';
import estilos from './FormularioSucursal.module.css';

type Props = { slugRestaurante: string; sucursal?: Sucursal; onGuardado(): void; onCancelar(): void };

const OPCIONES_ZONA = ZONAS_HORARIAS.map((z) => ({ valor: z, etiqueta: z }));

export function FormularioSucursal({ slugRestaurante, sucursal, onGuardado, onCancelar }: Props) {
  const { sesion } = useSesion();
  const token = sesion?.token ?? '';
  const esAlta = !sucursal;

  const [nombre, setNombre] = useState(sucursal?.nombre ?? '');
  const [direccion, setDireccion] = useState(sucursal?.direccion ?? '');
  const [telefono, setTelefono] = useState(sucursal?.telefono ?? '');
  const [slug, setSlug] = useState(sucursal?.slug ?? '');
  const [whatsApp, setWhatsApp] = useState(sucursal?.whatsApp ?? '');
  const [phoneNumberId, setPhoneNumberId] = useState(sucursal?.phoneNumberId ?? '');
  const [prefijo, setPrefijo] = useState(sucursal?.prefijo ?? '');
  const [zonaHoraria, setZonaHoraria] = useState(sucursal?.zonaHoraria ?? ZONA_POR_DEFECTO);
  const [errores, setErrores] = useState<Record<string, string[]>>({});
  const [errorGeneral, setErrorGeneral] = useState<string | null>(null);
  const [guardando, setGuardando] = useState(false);

  const titulo = esAlta ? 'Nueva sucursal' : `Editar ${sucursal.nombre}`;

  async function guardar(evento: FormEvent) {
    evento.preventDefault();
    setErrores({});
    setErrorGeneral(null);
    setGuardando(true);
    const datos = {
      nombre,
      direccion,
      telefono,
      whatsApp,
      phoneNumberId: phoneNumberId.trim() === '' ? null : phoneNumberId.trim(),
      prefijo,
      zonaHoraria,
    };
    try {
      if (esAlta) {
        await crearSucursal(token, slugRestaurante, { ...datos, slug });
      } else {
        await editarSucursal(token, slugRestaurante, sucursal.id, datos);
      }
      onGuardado();
    } catch (e) {
      if (e instanceof ErrorApi && e.estado === 400 && Object.keys(e.errores).length > 0) {
        setErrores(e.errores);
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
        <CampoFormulario etiqueta="Nombre" errores={errores.nombre} value={nombre} onChange={(e) => setNombre(e.target.value)} />
        <CampoFormulario
          etiqueta="Dirección"
          errores={errores.direccion}
          value={direccion}
          onChange={(e) => setDireccion(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Teléfono"
          errores={errores.telefono}
          type="tel"
          value={telefono}
          onChange={(e) => setTelefono(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Slug"
          errores={errores.slug}
          value={slug}
          disabled={!esAlta}
          onChange={(e) => setSlug(e.target.value)}
        />
        <CampoFormulario
          etiqueta="WhatsApp"
          errores={errores.whatsApp}
          type="tel"
          value={whatsApp}
          onChange={(e) => setWhatsApp(e.target.value)}
        />
        <CampoFormulario
          etiqueta="phone_number_id (opcional)"
          errores={errores.phoneNumberId}
          inputMode="numeric"
          value={phoneNumberId}
          onChange={(e) => setPhoneNumberId(e.target.value)}
        />
        <CampoFormulario
          etiqueta="Prefijo"
          errores={errores.prefijo}
          maxLength={3}
          value={prefijo}
          onChange={(e) => setPrefijo(e.target.value.toUpperCase())}
        />
        <CampoFormulario
          etiqueta="Zona horaria"
          errores={errores.zonaHoraria}
          opciones={OPCIONES_ZONA}
          value={zonaHoraria}
          onChange={(e) => setZonaHoraria(e.target.value)}
        />
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
