import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import { LoginPlataforma } from './plataforma/LoginPlataforma';
import { RestaurantesPagina } from './plataforma/RestaurantesPagina';
import { DisenoRestaurante } from './restaurante/DisenoRestaurante';
import { InicioRestaurante } from './restaurante/InicioRestaurante';
import { LoginRestaurante } from './restaurante/LoginRestaurante';
import { RutaProtegida } from './rutas/RutaProtegida';

const enrutador = createBrowserRouter([
  { path: '/', element: <Navigate replace to="/plataforma/login" /> },
  { path: '/plataforma/login', element: <LoginPlataforma /> },
  {
    path: '/plataforma/restaurantes',
    element: (
      <RutaProtegida roles={['administrador_plataforma']}>
        <RestaurantesPagina />
      </RutaProtegida>
    ),
  },
  {
    path: '/r/:slug',
    element: <DisenoRestaurante />,
    children: [
      { index: true, element: <Navigate replace to="login" /> },
      { path: 'login', element: <LoginRestaurante /> },
      { path: 'inicio', element: <InicioRestaurante /> },
    ],
  },
]);

export function App() {
  return <RouterProvider router={enrutador} />;
}
