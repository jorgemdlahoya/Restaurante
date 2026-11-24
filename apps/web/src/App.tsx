import { createBrowserRouter, Navigate, type RouteObject, RouterProvider } from 'react-router';
import { PaginaNoEncontrada } from './componentes/PaginaNoEncontrada';
import { LoginPlataforma } from './plataforma/LoginPlataforma';
import { RestaurantesPagina } from './plataforma/RestaurantesPagina';
import { DisenoRestaurante } from './restaurante/DisenoRestaurante';
import { InicioRestaurante } from './restaurante/InicioRestaurante';
import { LoginRestaurante } from './restaurante/LoginRestaurante';
import { RutaProtegida } from './rutas/RutaProtegida';
import { SucursalesPagina } from './sucursales/SucursalesPagina';

export const rutas: RouteObject[] = [
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
      { path: 'sucursales', element: <SucursalesPagina /> },
      { path: '*', element: <PaginaNoEncontrada /> },
    ],
  },
  { path: '*', element: <PaginaNoEncontrada /> },
];

const enrutador = createBrowserRouter(rutas);

export function App() {
  return <RouterProvider router={enrutador} />;
}
