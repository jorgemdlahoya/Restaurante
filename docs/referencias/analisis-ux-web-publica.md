# Análisis de toks.com.mx: plano para la web de tu restaurante

Analizado el 15/11/2026 con un navegador real, en escritorio (1440px) y en móvil (390px).
Las capturas están en la carpeta de trabajo de la sesión (`desk.png`, `mob.png`, `p_*.png`).

> Este plano copia la **estructura y las funciones** de la página. Logo, fotos, nombre, textos y
> la combinación negro+naranja de Toks son marca registrada suya: tu sitio debe usar tu propia identidad.

---

## 1. Mapa del sitio

| Ruta | Qué es |
|---|---|
| `/` | Portada: menú del restaurante, menú para pedir, banner de la app, 2 tarjetas grandes y footer |
| `/home` | Pedir en línea: elegir entre domicilio o recoger en tienda, escribir la dirección, carrusel de promos y "Nuestro menú" |
| `/encuentra-tu-toks` | Localizador de sucursales: mapa de Google a pantalla completa con un panel lateral de búsqueda |
| `/conocenos` | Historia, "nuestros detalles", favoritos del menú, distintivos, alianzas y blog/aprendizaje |
| `/responsabilidad-social` | Proyectos de productores con video y un botón "Conocer más" en cada uno |
| `/aviso-de-privacidad`, `/terminos-y-condiciones`, `/pet-friendly`, `/legales-promociones` | Páginas legales |
| Externos | Facturación (`efactura.toks.com.mx`), app en Google Play y App Store, web del grupo corporativo |

## 2. Header (fijo, 70px de alto, fondo blanco)

```
[LOGO]  Menú | Conócenos | Responsabilidad Social     (📍 Encuentra tu sucursal) (ORDENA AQUÍ)  ¡Hola, Invitado! 👤  🛍  🇲🇽▾
```
- Logo cuadrado a la izquierda, que sobresale por debajo del header.
- Enlaces de texto separados por barras verticales.
- Botón píldora negro con icono de ubicación que lleva al localizador.
- Botón píldora naranja, el CTA principal, que lleva a pedir en línea.
- Saludo con la cuenta del usuario (iniciar sesión con Google), carrito y selector de idioma con bandera.
- **En móvil:** menú hamburguesa, logo centrado, y a la derecha saludo, cuenta, carrito e idioma.

## 3. Portada, de arriba abajo

1. **Menú de restaurante:** título centrado con la segunda parte en el color de acento, seguido de un carrusel de categorías que se carga desde la API.
2. **Menú para pedir en línea:** igual, pero con las categorías de pedido.
3. **Banner de la app** a todo lo ancho, sobre foto oscura: titular grande en mayúsculas, insignias de las tiendas, foto de producto, maqueta de un teléfono y un código de descuento para el primer pedido.
4. **Dos tarjetas grandes al 50/50** (450px de alto), con foto de fondo, título y subtítulo abajo a la izquierda y una flecha en un cuadro naranja abajo a la derecha. Una va a Responsabilidad social y otra al Club de lealtad.
5. **Footer negro** con 3 columnas: logo · redes sociales y descarga de la app · enlaces "Sobre nosotros" en dos columnas (Conócenos, Contacto, Facturación, Bolsa de trabajo, Club, Responsabilidad social, Grupo). Debajo, el copyright con los enlaces legales en línea.

## 4. Funciones que hay que construir

| Función | Cómo la resuelve Toks | Equivalente para ti |
|---|---|---|
| Pedidos en línea | Plataforma propia de **Wallia** (`api-toks.wallia.cloud`): domicilio o recoger, dirección, carrito y cuenta | Algo sencillo: enlaces a Rappi, DiDi Food o Uber Eats, o un pedido por WhatsApp. Más completo: una plataforma de pedidos (Wallia, Toteat, Square, Shopify) |
| Localizador | Google Maps JS API, buscador de dirección y botón "Usar mi ubicación" | Con una sola sucursal basta un mapa incrustado y la dirección. Con varias, Google Maps o Leaflet+OpenStreetMap más una lista |
| Menú | Categorías y platillos servidos por la API en carruseles | Un JSON o CMS con categorías y platillos (foto, nombre, descripción, precio) |
| Cuenta / login | Google Identity Services | Solo hace falta si tienes pedidos o programa de lealtad |
| Club de lealtad | App con niveles, puntos y tarjeta digital con QR | Opcional (por ejemplo una tarjeta de sellos digital) |
| Facturación | Portal externo | Enlace a tu proveedor de CFDI |
| Bolsa de trabajo / contacto | Formularios o modales | Un formulario (Formspree, Netlify Forms) o WhatsApp |
| Analítica | Google Tag Manager / GA4 | Lo mismo |
| Idiomas | Selector ES/EN | Opcional |

## 5. Estilo visual medido

- **Tipografía:** *Nunito* (Google Fonts) en todo el sitio. Los titulares van en mayúsculas y peso extra bold (800–900).
- **Colores medidos:** negro `#000000` y `#111111` (header de pedidos, footer, botones), blanco `#FFFFFF`, gris de fondo `#F8F9FA`, naranja `#F97316` / `#FF6600` (CTA y acentos), gris azulado `#1F2937` (títulos).
- **Botones:** píldora (radio completo), en mayúsculas y negrita.
- **Imágenes:** fotos de producto y de personas con mucho contraste, banners con texto incrustado en la imagen y maquetas de teléfono.
- **Estructura:** bloques a todo lo ancho que se apilan sin márgenes entre ellos, con títulos de sección centrados.

## 6. Tecnología detectada

- SPA hecha con **Vite** (bundle `assets/index.[hash].js`), probablemente **React o Vue**, con **Tailwind CSS** (clases como `sticky top-[62px] md:top-[62px] z-40`).
- Google Tag Manager, Google Sign-In, Google Maps y crypto-js.
- Los datos del menú, las sucursales y los pedidos vienen de la API de Wallia.

## 7. Qué no se pudo ver

- **El contenido de los carruseles de menú**, porque viene de `api-toks.wallia.cloud` y este entorno no tiene acceso a ese dominio. Aparecen vacíos en las capturas.
- Flujos que requieren sesión o dirección: carrito, pago y club.

## 8. Qué necesito de ti para construir la tuya

1. Nombre, logo y colores (si no tienes, los proponemos).
2. Tipo de cocina y número de sucursales.
3. El menú: categorías, platillos, precios y, si es posible, fotos.
4. Cómo quieres recibir pedidos: apps de delivery, WhatsApp o pedidos propios.
5. Horarios, dirección, teléfono y redes sociales.
6. Qué secciones extra quieres: historia, club, facturación, bolsa de trabajo.
