# ---- Compilación ----
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY PlataformaIncidencias.csproj .
RUN dotnet restore
COPY . .
RUN dotnet publish PlataformaIncidencias.csproj -c Release -o /app/publish --no-restore

# ---- Ejecución ----
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Carpeta con permisos de escritura para la base SQLite (el contenedor no corre como root).
RUN mkdir -p /app/data && chown "$APP_UID" /app/data
USER $APP_UID

# Render termina HTTPS en su proxy y reenvía por HTTP; los secretos se configuran en Render.
ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_FORWARDEDHEADERS_ENABLED=true \
    ConnectionStrings__DefaultConnection="Data Source=/app/data/incidencias.db"

EXPOSE 8080
# Render indica el puerto en la variable PORT (por defecto 10000).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_HTTP_PORTS=${PORT:-8080} exec dotnet PlataformaIncidencias.dll"]
