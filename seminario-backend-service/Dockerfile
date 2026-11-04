# syntax=docker/dockerfile:1

# ---------- build ----------
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Restaurar solo con el csproj: mientras no cambien los paquetes, esta capa sale de cache.
COPY seminario-backend-service.csproj ./
RUN dotnet restore seminario-backend-service.csproj

COPY . .
RUN dotnet publish seminario-backend-service.csproj -c Release -o /app/publish --no-restore /p:UseAppHost=false

# ---------- runtime ----------
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

COPY --from=build /app/publish .

# FileStorageService escribe en {ContentRoot}/uploads. Se monta como volumen y debe
# pertenecer al usuario sin privilegios de la imagen base (APP_UID).
RUN mkdir -p /app/uploads && chown -R $APP_UID /app/uploads

USER $APP_UID

ENV ASPNETCORE_URLS=http://+:8080 \
    ASPNETCORE_ENVIRONMENT=Production
EXPOSE 8080

# GET /api/health/live con el /dev/tcp de bash: la imagen base no trae curl/wget y asi el
# build no depende de apt (ni de que el host tenga salida a internet).
# Es el liveness (el proceso responde); /api/health, que tambien revisa SQL Server y
# MongoDB, lo vigila Uptime Kuma. Asi una base caida no marca el contenedor como roto.
HEALTHCHECK --interval=15s --timeout=5s --start-period=40s --retries=5 \
    CMD ["bash", "-c", "exec 3<>/dev/tcp/127.0.0.1/8080 && printf 'GET /api/health/live HTTP/1.0\\r\\nHost: localhost\\r\\n\\r\\n' >&3 && head -n 1 <&3 | grep -q ' 200 '"]

ENTRYPOINT ["dotnet", "seminario-backend-service.dll"]
