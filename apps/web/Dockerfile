# Contexto de build: apps/web (docker compose lo construye con `build: ./apps/web`).

# --- Etapa de compilación ---
FROM node:22-alpine AS build
WORKDIR /app

# Dependencias primero (capa cacheable).
COPY package.json package-lock.json ./
RUN npm ci

COPY . .
RUN npm run build

# --- Etapa de ejecución ---
FROM nginx:alpine AS runtime
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
