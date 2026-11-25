import { seRecorta, tituloWhatsApp } from './tituloWhatsApp';
import estilos from './VistaPreviaWhatsApp.module.css';

type Props = { nombre: string; nombreCorto: string | null };

export function VistaPreviaWhatsApp({ nombre, nombreCorto }: Props) {
  return (
    <div className={estilos.vista}>
      <p className={estilos.titulo}>Así se verá en WhatsApp: {tituloWhatsApp(nombre, nombreCorto)}</p>
      {seRecorta(nombre, nombreCorto) && (
        <p className={estilos.aviso}>Este nombre se verá recortado en WhatsApp</p>
      )}
    </div>
  );
}
