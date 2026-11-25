import '@fontsource/nunito/400.css';
import '@fontsource/nunito/700.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { ErrorApi } from './api/cliente';
import { App } from './App';
import { ProveedorSesion } from './sesion/ProveedorSesion';
import './tema/tema.css';

// Un 4xx no mejora reintentando (p. ej. el 403 de RF-26 tardaría varios segundos en mostrarse):
// sólo se reintentan, hasta 3 veces, los errores de red y los 5xx.
const clienteConsultas = new QueryClient({
  defaultOptions: {
    queries: {
      retry: (fallos, error) => !(error instanceof ErrorApi && error.estado < 500) && fallos < 3,
    },
  },
});

// Sin ProveedorTema global: tema.css ya fija la paleta por defecto en :root, y un proveedor
// externo pisaría la paleta del restaurante (los efectos del padre corren después que los del hijo).
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <ProveedorSesion>
        <App />
      </ProveedorSesion>
    </QueryClientProvider>
  </StrictMode>,
);
