import '@fontsource/nunito/400.css';
import '@fontsource/nunito/700.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { App } from './App';
import { ProveedorTema } from './tema/ProveedorTema';
import './tema/tema.css';

const clienteConsultas = new QueryClient();

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <QueryClientProvider client={clienteConsultas}>
      <ProveedorTema>
        <App />
      </ProveedorTema>
    </QueryClientProvider>
  </StrictMode>,
);
