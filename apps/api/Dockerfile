# Contexto de build: apps/api (docker compose lo construye con `build: ./apps/api`).

# --- Etapa de compilación ---
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Restauración primero (capa cacheable): sólo los archivos de proyecto y de versiones centralizadas.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Restaurante.Domain/Restaurante.Domain.csproj src/Restaurante.Domain/
COPY src/Restaurante.Application/Restaurante.Application.csproj src/Restaurante.Application/
COPY src/Restaurante.Infrastructure/Restaurante.Infrastructure.csproj src/Restaurante.Infrastructure/
COPY src/Restaurante.WebApi/Restaurante.WebApi.csproj src/Restaurante.WebApi/
RUN dotnet restore src/Restaurante.WebApi/Restaurante.WebApi.csproj

COPY src/ src/
RUN dotnet publish src/Restaurante.WebApi/Restaurante.WebApi.csproj \
    -c Release --no-restore -o /app/publish

# --- Etapa de ejecución ---
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

# Usuario sin privilegios (incluido en las imágenes oficiales de ASP.NET).
USER $APP_UID

ENTRYPOINT ["dotnet", "Restaurante.WebApi.dll"]
