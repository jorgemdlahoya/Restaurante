import '@fontsource/nunito/400.css';
import '@fontsource/nunito/700.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { ProveedorSesion } from './sesion/ProveedorSesion';
import './tema/tema.css';

const clienteConsultas = new QueryClient();

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
