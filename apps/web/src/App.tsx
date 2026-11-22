import { createBrowserRouter, RouterProvider } from 'react-router';

// Enrutador vacío: las pantallas se registran en las tareas siguientes.
const enrutador = createBrowserRouter([]);

export function App() {
  return <RouterProvider router={enrutador} />;
}
