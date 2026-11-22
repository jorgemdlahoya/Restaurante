import { createBrowserRouter, Navigate, RouterProvider } from 'react-router';
import { DisenoRestaurante } from './restaurante/DisenoRestaurante';
import { InicioRestaurante } from './restaurante/InicioRestaurante';
import { LoginRestaurante } from './restaurante/LoginRestaurante';

const enrutador = createBrowserRouter([
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
